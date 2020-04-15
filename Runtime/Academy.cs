using Unity.Entities;
using Unity.Collections;
using System;
using Unity.AI.MLAgents.SideChannels;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic; // TODO : REMOVE
using UnityEngine;


namespace Unity.AI.MLAgents
{
    /// <summary>
    /// The Academy is a singleton that orchestrates the decision making of the
    /// decision making of the Agents.
    /// It is used to register WorldProcessors to Worlds and to keep track of the
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

        private bool FirstMessageReceived;
        private SharedMemoryCom com;

        internal Dictionary<MLAgentsWorld, IWorldProcessor> WorldToProcessor; // Maybe we can put the processor in the world with an unsafe unmanaged memory pointer ?

        /// <summary>
        /// Signals that the Academy has been reset by the training process
        /// If you have jobs Scheduled but not completed when this event is called,
        /// If is recommended to Complete them.
        /// </summary>
        public event Action OnEnvironmentReset;

        /// <summary>
        /// Registers a MLAgentsWorld to a decision making mechanism.
        /// By default, the MLAgentsWorld will use a remote process for decision making when available.
        /// </summary>
        /// <param name="policyId"> The string identifier of the MLAgentsWorld. There can only be one world per unique id.</param>
        /// <param name="world"> The MLAgentsWorld that is being subscribed.</param>
        /// <param name="worldProcessor"> If the remote process is not available, the MLAgentsWorld will use this World processor for decision making.</param>
        /// <param name="defaultRemote"> If true, the MLAgentsWorld will default to using the remote process for communication making and use the fallback worldProcessor otherwise.</param>
        public void RegisterWorld(string policyId, MLAgentsWorld world, IWorldProcessor worldProcessor = null, bool defaultRemote = true)
        {
            // Need to find a way to deregister ?
            // Need a way to modify the World processor on the fly
            // Automagically register world on creation ?

            IWorldProcessor processor = null;
            if (com != null && defaultRemote)
            {
                processor = new RemoteWorldProcessor(world, policyId, com);
            }
            else if (worldProcessor != null)
            {
                processor = worldProcessor;
            }
            else
            {
                processor = new NullWorldProcessor(world);
            }
            WorldToProcessor[world] = processor;
        }

        private void LazyInitialize()
        {
            if (!m_Initialized)
            {
                Application.quitting += Dispose;
                OnEnvironmentReset = () => {};

                WorldToProcessor = new Dictionary<MLAgentsWorld, IWorldProcessor>();

                TryInitializeCommunicator();
                m_Initialized = true;
            }
        }

        private void TryInitializeCommunicator()
        {
            var path = ArgParser.ReadSharedMemoryPathFromArgs();

            if (path == null)
            {
                UnityEngine.Debug.Log("Could not connect");
            }
            else
            {
                com = new SharedMemoryCom(path);
                if (!com.Active)
                {
                    com = null;
                    return;
                }
                SideChannelUtils.RegisterSideChannel(new EngineConfigurationChannel());
                SideChannelUtils.RegisterSideChannel(new FloatPropertiesChannel());
            }
        }

        // We will make the assumption that a world can only be updated one at a time
        internal void UpdateWorld(MLAgentsWorld world)
        {
            if (!m_Initialized)
            {
                return;
            }

            // If no agents requested a decision return
            if (world.DecisionCounter.Count == 0 && world.TerminationCounter.Count == 0)
            {
                return;
            }

            // Ensure the world does not have lingering actions:
            if (world.ActionCounter.Count != 0)
            {
                // This means something in the execution went wrong, this error should never appear
                throw new MLAgentsException("TODO : ActionCount is not 0");
            }

            var processor = WorldToProcessor[world];
            if (processor == null)
            {
                // Raise error
                throw new MLAgentsException($"A world has not been correctly registered.");
            }


            if (com != null && com.Active && processor.IsConnected)
            {
                bool reset = false;
                #region BLOCKING_ALL_THREADS
                if (!FirstMessageReceived)
                {
                    // Unity must call advance to read the first message of Python.
                    // We do this only if there is already something to send
                    // We could ignore the first command
                    com.WaitForPython();
                    SideChannelUtils.ProcessSideChannelData(com.ReadAndClearSideChannelData());
                    FirstMessageReceived = true;
                    reset = com.ReadAndClearResetCommand();
                }
                if (!reset) // TODO : Comment out if we do not want to reset on first env.reset()
                {
                    com.WriteSideChannelData(SideChannelUtils.GetSideChannelMessage());
                    processor.ProcessWorld();
                    reset = com.ReadAndClearResetCommand();
                    world.SetActionReady();
                    world.ResetDecisionsAndTerminationCounters();
                    SideChannelUtils.ProcessSideChannelData(com.ReadAndClearSideChannelData());
                }
                if (reset)
                {
                    Reset();
                }
                #endregion
            }
            else if (!processor.IsConnected)
            {
                processor.ProcessWorld();
                world.SetActionReady();
                world.ResetDecisionsAndTerminationCounters();
                // TODO com.ReadAndClearSideChannelData(); // Remove side channel data
            }
            else
            {
                // The processor wants to communicate but the communicator is either null or inactive
                world.ResetActionsCounter();
                world.ResetDecisionsAndTerminationCounters();
            }
            if (com == null)
            {
                SideChannelUtils.GetSideChannelMessage();
            }
        }

        private void Reset()
        {
            World.DefaultGameObjectInjectionWorld.EntityManager.CompleteAllJobs(); // This is problematic because it completes only for the active world
            ResetAllWorlds();
            OnEnvironmentReset?.Invoke();
        }

        private void ResetAllWorlds() // This is problematic because it affects all worlds and is not thread safe...
        {
            foreach (var w in WorldToProcessor.Keys)
            {
                w.ResetActionsCounter();
                w.ResetDecisionsAndTerminationCounters();
            }
        }

        /// <summary>
        /// Shuts down the Academy.
        /// </summary>
        public void Dispose()
        {
            com?.Dispose();
            com = null;
            SideChannelUtils.UnregisterAllSideChannels();
            m_Initialized = false;

            // Reset the Lazy instance // No reset because Academy.Instance is called after dispose...
            s_Lazy = new Lazy<Academy>(() => new Academy());
        }
    }
}
