using Unity.Entities;
using System;
using Unity.AI.MLAgents.SideChannels;

using System.Collections.Generic;
using UnityEngine;


namespace Unity.AI.MLAgents
{
    /// <summary>
    /// The Academy is a singleton that orchestrates the decision making of the
    /// decision making of the Agents.
    /// It is used to register PolicyProcessors to Policy and to keep track of the
    /// reset logic of the simulation.
    /// </summary>
    public class Academy : IDisposable
    {
        #region Singleton
        // Lazy initializer pattern, see https://csharpindepth.com/articles/singleton#lazy
        static Lazy<Academy> s_Lazy = new Lazy<Academy>(() => new Academy());

        /// <summary>
        /// True if the Academy is initialized, false otherwise.
        /// </summary>
        public static bool IsInitialized
        {
            get { return s_Lazy.IsValueCreated; }
        }

        /// <summary>
        /// The singleton Academy object.
        /// </summary>
        public static Academy Instance { get { return s_Lazy.Value; } }

        /// <summary>
        /// Private constructor called the first time the Academy is used.
        /// Academy uses this time to initialize internal data
        /// structures, initialize the environment and check for the existence
        /// of a communicator.
        /// </summary>
        Academy()
        {
            LazyInitialize();
        }

        bool m_Initialized = false;
        #endregion

        private bool m_FirstMessageReceived;
        private SharedMemoryCommunicator m_Communicator;

        internal Dictionary<Policy, IPolicyProcessor> m_PolicyToProcessor;

        private EnvironmentParameters m_EnvironmentParameters;
        private StatsRecorder m_StatsRecorder;

        /// <summary>
        /// Signals that the Academy has been reset by the training process
        /// If you have jobs Scheduled but not completed when this event is called,
        /// If is recommended to Complete them.
        /// </summary>
        public Action OnEnvironmentReset;

        /// <summary>
        /// Registers a Policy to a decision making mechanism.
        /// By default, the Policy will use a remote process for decision making when available.
        /// </summary>
        /// <param name="policyId"> The string identifier of the Policy. There can only be one Policy per unique id.</param>
        /// <param name="policy"> The Policy that is being subscribed.</param>
        /// <param name="policyProcessor"> If the remote process is not available, the Policy will use this IPolicyProcessor for decision making.</param>
        /// <param name="defaultRemote"> If true, the Policy will default to using the remote process for communication making and use the fallback IPolicyProcessor otherwise.</param>
        public void RegisterPolicy(string policyId, Policy policy, IPolicyProcessor policyProcessor = null, bool defaultRemote = true)
        {
            IPolicyProcessor processor = null;
            if (m_Communicator != null && defaultRemote)
            {
                processor = new RemotePolicyProcessor(policy, policyId, m_Communicator);
            }
            else if (policyProcessor != null)
            {
                processor = policyProcessor;
            }
            else
            {
                processor = new NullPolicyProcessor(policy);
            }
            m_PolicyToProcessor[policy] = processor;
        }

        /// <summary>
        /// Returns the <see cref="EnvironmentParameters"/> instance. If training
        /// features such as Curriculum Learning or Environment Parameter Randomization are used,
        /// then the values of the parameters generated from the training process can be
        /// retrieved here.
        /// </summary>
        /// <returns></returns>
        public EnvironmentParameters EnvironmentParameters
        {
            get { return m_EnvironmentParameters; }
        }

        /// <summary>
        /// Returns the <see cref="StatsRecorder"/> instance. This instance can be used
        /// to record any statistics from the Unity environment.
        /// </summary>
        /// <returns></returns>
        public StatsRecorder StatsRecorder
        {
            get { return m_StatsRecorder; }
        }

        /// <summary>
        /// Reports whether or not the communicator is on.
        /// </summary>
        /// <seealso cref="ICommunicator"/>
        /// <value>
        /// <c>True</c>, if communicator is on, <c>false</c> otherwise.
        /// </value>
        public bool IsCommunicatorOn
        {
            get { return m_Communicator != null; }
        }

        private void LazyInitialize()
        {
            if (!m_Initialized)
            {
                Application.quitting += Dispose;
                OnEnvironmentReset = () => {};

                m_PolicyToProcessor = new Dictionary<Policy, IPolicyProcessor>();

                TryInitializeCommunicator();
                SideChannelManager.RegisterSideChannel(new EngineConfigurationChannel());
                m_EnvironmentParameters = new EnvironmentParameters();
                m_StatsRecorder = new StatsRecorder();
                m_Initialized = true;
            }
        }

        private void TryInitializeCommunicator()
        {
            var path = ArgumentParser.ReadSharedMemoryPathFromArgs();

            if (path == null)
            {
                Debug.Log("ML-Agents could not connect with the Python training process.");
            }
            else
            {
                m_Communicator = new SharedMemoryCommunicator(path);
                if (!m_Communicator.Active)
                {
                    m_Communicator = null;
                    return;
                }
            }
        }

        // We will make the assumption that a policy can only be updated one at a time
        internal void UpdatePolicy(Policy policy)
        {
            if (!m_Initialized)
            {
                return;
            }

            // If no agents requested a decision return
            if (policy.DecisionCounter.Count == 0 && policy.TerminationCounter.Count == 0)
            {
                return;
            }

            // Ensure the policy does not have lingering actions:
            if (policy.ActionCounter.Count != 0)
            {
                // This means something in the execution went wrong, this error should never appear
                throw new MLAgentsException("An error ocurred, ActionCount is not 0 at start of policy update");
            }

            var processor = m_PolicyToProcessor[policy];
            if (processor == null)
            {
                // Raise error
                throw new MLAgentsException($"A Policy has not been correctly registered.");
            }


            if (m_Communicator != null && m_Communicator.Active && processor.IsConnected)
            {
                bool reset = false;
                #region BLOCKING_ALL_THREADS
                if (!m_FirstMessageReceived)
                {
                    // Unity must call advance to read the first message of Python.
                    // We do this only if there is already something to send
                    // We could ignore the first command
                    m_Communicator.WaitForPython();
                    AnswerQuery();
                    SideChannelManager.ProcessSideChannelData(m_Communicator.ReadAndClearSideChannelData());
                    m_FirstMessageReceived = true;
                    reset = m_Communicator.ReadAndClearResetCommand();
                }
                if (!reset)
                {
                    m_Communicator.WriteSideChannelData(SideChannelManager.GetSideChannelMessage());
                    processor.Process();
                    reset = m_Communicator.ReadAndClearResetCommand();
                    policy.SetActionReady();
                    policy.ResetDecisionsAndTerminationCounters();
                    SideChannelManager.ProcessSideChannelData(m_Communicator.ReadAndClearSideChannelData());
                }
                if (reset)
                {
                    Reset();
                }
                #endregion
            }
            else if (!processor.IsConnected)
            {
                processor.Process();
                policy.SetActionReady();
                policy.ResetDecisionsAndTerminationCounters();
            }
            else
            {
                // The processor wants to communicate but the communicator is either null or inactive
                policy.ResetActionsCounter();
                policy.ResetDecisionsAndTerminationCounters();
            }
            if (m_Communicator == null)
            {
                SideChannelManager.GetSideChannelMessage();
            }
        }

        private void AnswerQuery()
        {
            while (m_Communicator.ReadAndClearQueryCommand())
            {
                var data = m_Communicator.ReadAndClearSideChannelData();
                SideChannelManager.ProcessSideChannelData(data);
                m_Communicator.WriteSideChannelData(SideChannelManager.GetSideChannelMessage());
                m_Communicator.SetUnityReady();
                m_Communicator.WaitForPython();
            }
        }

        private void Reset()
        {
            foreach (World ECSWorld in World.All)
            {
                // Need to complete all of the jobs at this point.
                ECSWorld.EntityManager.CompleteAllJobs();
            }
            ResetAllPolicies();
            OnEnvironmentReset?.Invoke();
        }

        private void ResetAllPolicies() // This is problematic because it affects all policies and is not thread safe...
        {
            foreach (var pol in m_PolicyToProcessor.Keys)
            {
                pol.ResetActionsCounter();
                pol.ResetDecisionsAndTerminationCounters();
            }
        }

        /// <summary>
        /// Shuts down the Academy.
        /// </summary>
        public void Dispose()
        {
            m_Communicator?.Dispose();
            m_Communicator = null;
            SideChannelManager.UnregisterAllSideChannels();
            m_Initialized = false;

            // Reset the Lazy instance // No reset because Academy.Instance is called after dispose...
            s_Lazy = new Lazy<Academy>(() => new Academy());
        }
    }
}
