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
        /// Collection of float properties (indexed by a string).
        /// </summary>
        public IFloatProperties FloatProperties;
        private SideChannel[] m_SideChannels;

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
        /// <param name="defaultRemote"> If true, the MLAgentsWorld will default to using the remote process for communication making and use the fallbackWorldProcessor otherwise.</param>
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

        /// <summary>
        /// Registers SideChannel to the Academy to send and receive data with Python.
        /// If IsCommunicatorOn is false, the SideChannel will not be registered.
        /// </summary>
        /// <param name="channel"> The side channel to be registered.</param>
        public void RegisterSideChannel(SideChannel channel)
        {
            foreach (var registeredChannel in m_SideChannels)
            {
                if (registeredChannel.ChannelId == channel.ChannelId)
                {
                    throw new MLAgentsException("TODO : 2 side channels with same id");
                }
            }
            Array.Resize<SideChannel>(ref m_SideChannels, m_SideChannels.Length + 1);
            m_SideChannels[m_SideChannels.Length - 1] = channel;
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
                m_SideChannels = new SideChannel[0];
            }
            else
            {
                com = new SharedMemoryCom(path);
                m_SideChannels = new SideChannel[2];
                m_SideChannels[0] = new EngineConfigurationChannel();;
                var FloatPropertiesChannel = new FloatPropertiesChannel();
                m_SideChannels[1] = FloatPropertiesChannel;
                FloatProperties = FloatPropertiesChannel;
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
            if (world.AgentCounter.Count == 0)
            {
                return;
            }

            // Ensure the world does not have lingering actions:
            if (world.ActionCounter.Count != 0)
            {
                throw new MLAgentsException("TODO : ActionCount is not 0");
            }

            var processor = WorldToProcessor[world];
            if (processor == null)
            {
                // Raise error
                throw new MLAgentsException("TODO : Null processor");
            }

            var command = RemoteCommand.DEFAULT;

            if (com != null && processor.IsConnected)
            {
                #region BLOCKING_ALL_THREADS
                if (!FirstMessageReceived)
                {
                    // Unity must call advance to read the first message of Python.
                    // We do this only if there is already something to send
                    command = com.Advance();
                    SideChannelUtils.ProcessSideChannelData(m_SideChannels, com.ReadAndClearSideChannelData());
                    FirstMessageReceived = true;
                }

                if (command == RemoteCommand.DEFAULT)
                {
                    com.WriteSideChannelData(SideChannelUtils.GetSideChannelMessage(m_SideChannels));
                    command = processor.ProcessWorld();
                    world.SetActionReady();
                    world.ResetDecisionsCounter();
                    SideChannelUtils.ProcessSideChannelData(m_SideChannels, com.ReadAndClearSideChannelData());
                }
                #endregion
            }
            else
            {
                command = processor.ProcessWorld();
                world.SetActionReady();
                world.ResetDecisionsCounter();
            }
            switch (command)
            {
                case RemoteCommand.RESET:
                    World.DefaultGameObjectInjectionWorld.EntityManager.CompleteAllJobs(); // This is problematic because it completes only for the active world
                    ResetAllWorlds();
                    OnEnvironmentReset?.Invoke();
                    // TODO : RESET logic
                    break;

                case RemoteCommand.CLOSE:
#if UNITY_EDITOR
                    EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    com = null;
                    break;

                case RemoteCommand.DEFAULT:
                    break;

                default:
                    break;
            }
        }

        private void ResetAllWorlds() // This is problematic because it affects all worlds and is not thread safe...
        {
            foreach (var w in WorldToProcessor.Keys)
            {
                w.ResetActionsCounter();
                w.ResetDecisionsCounter();
            }
        }

        /// <summary>
        /// Shuts down the Academy.
        /// </summary>
        public void Dispose()
        {
            com?.Dispose();
            com = null;

            FloatProperties = null;
            m_Initialized = false;

            // Reset the Lazy instance // No reset because Academy.Instance is called after dispose...
            // s_Lazy = new Lazy<Academy>(() => new Academy());
        }
    }
}
