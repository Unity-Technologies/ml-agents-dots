using Unity.Entities;
using Unity.Jobs;
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

        public static bool IsInitialized
        {
            get { return s_Lazy.IsValueCreated; }
        }

        public static Academy Instance { get { return s_Lazy.Value; } }

        /// <summary>
        /// Private constructor called the first time the Academy is used.
        /// Academy uses this time to initialize internal data
        /// structures, initialize the environment and check for the existence
        /// of a communicator.
        /// </summary>
        Academy()
        {
            // Application.quitting += Dispose;
            LazyInitialize();
        }

        bool m_Initialized = false;
        #endregion

        private bool FirstMessageReceived;

        internal Dictionary<MLAgentsWorld, IWorldProcessor> WorldToProcessor; // Maybe we can put the processor in the world with an unsafe managed memory pointer

        private SharedMemoryCom com;

        public IFloatProperties FloatProperties;
        private SideChannel[] m_SideChannels;

        // Signals that the Academy has been reset by the training process
        // If you have jobs Scheduled but not completed when this event is called,
        // If is recommended to Complete them.
        public event Action OnEnvironmentReset;

        // Need to find a way to deregister ?
        public void SubscribeWorld(string policyId, MLAgentsWorld world, IWorldProcessor fallbackWorldProcessor = null, bool communicate = true)
        {
            var nativePolicyId = new NativeString64(policyId);
            CheckWorldNotPresent(nativePolicyId, world.GetHashCode());

            IWorldProcessor processor = null;
            if (com != null && communicate)
            {
                processor = new RemoteWorldProcessor(world, policyId, com);
            }
            else if (fallbackWorldProcessor != null)
            {
                processor = fallbackWorldProcessor;
            }
            else
            {
                processor = new NullWorldProcessor(world);
            }
            // Array.Resize<IWorldProcessor>(ref WorldProcessors, WorldProcessors.Length + 1);
            // WorldProcessors[WorldProcessors.Length - 1] = processor;
            WorldToProcessor[world] = processor;
        }

        public void SubscribeSideChannel(SideChannel channel)
        {
            foreach (var registeredChannel in m_SideChannels)
            {
                if (registeredChannel.ChannelId == channel.ChannelId)
                {
                    // TODO Error
                    return;
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

                RegisteredWorldHashes = new NativeList<int>(Allocator.Persistent);
                RegisteredWorldNames = new NativeList<NativeString64>(Allocator.Persistent);

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
                throw new MLAgentsException("TODO : ActionCount is 0");
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
                    OnEnvironmentReset?.Invoke();;
                    ResetAllWorlds();
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

        private NativeList<int> RegisteredWorldHashes;
        private NativeList<NativeString64> RegisteredWorldNames;

        private void CheckWorldNotPresent(NativeString64 policyId, int worldHash)
        {
            if (RegisteredWorldHashes.Contains(worldHash))
            {
                throw new MLAgentsException("The MLAgentsWorld has already been subscribed ");
            }
            if (RegisteredWorldNames.Contains(policyId))
            {
                throw new MLAgentsException(
                    string.Format(
                        "An MLAgentsWorld has already been subscribed using the key {0}",
                        policyId)
                );
            }
            RegisteredWorldHashes.Add(worldHash);
            RegisteredWorldNames.Add(policyId);
        }

        public void Dispose()
        {
            com?.Dispose();
            com = null;

            FloatProperties = null;
            m_Initialized = false;

            RegisteredWorldHashes.Dispose();
            RegisteredWorldNames.Dispose();

            // Reset the Lazy instance
            s_Lazy = new Lazy<Academy>(() => new Academy());
        }
    }
}
