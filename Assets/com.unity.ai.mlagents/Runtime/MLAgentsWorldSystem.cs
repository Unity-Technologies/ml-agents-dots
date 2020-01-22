using Unity.Entities;
using Unity.Jobs;
using System.Collections.Generic;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Unity.AI.MLAgents
{

    // [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class MLAgentsWorldSystem : JobComponentSystem // Should this be a ISimulation from Unity.Physics ?
    {

        private JobHandle dependencies;
        public JobHandle FinalJobHandle;

        private SharedMemoryCom com;

        private struct IdWorldPair
        {
            public string name;
            public MLAgentsWorld world;
        }
        private bool FirstMessageReceived;
        private List<IdWorldPair> ExternalWorlds;
        private List<IWorldProcessor> WorldProcessors;
        private HashSet<MLAgentsWorld> RegisteredWorlds;
        private HashSet<string> RegisteredWorldNames;

        public void SubscribeWorld(string policyId, MLAgentsWorld world, IWorldProcessor fallbackWorldProcessor = null, bool communicate = true)
        {
            CheckWorldNotPresent(policyId, world);
            if (com != null && communicate)
            {
                ExternalWorlds.Add(new IdWorldPair { name = policyId, world = world });
            }
            else if (fallbackWorldProcessor != null)
            {
                WorldProcessors.Add(fallbackWorldProcessor);
            }
        }

        protected override void OnCreate()
        {
            ExternalWorlds = new List<IdWorldPair>();
            WorldProcessors = new List<IWorldProcessor>();
            RegisteredWorlds = new HashSet<MLAgentsWorld>();
            RegisteredWorldNames = new HashSet<string>();

            dependencies = new JobHandle();
            FinalJobHandle = new JobHandle();
            TryInitializeCommunicator();
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
            }
        }

        /// <summary>
        /// TODO : Need help on handling dependencies
        /// </summary>
        public void RegisterDependency(JobHandle handle)
        {
            dependencies = JobHandle.CombineDependencies(handle, dependencies);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        // { return inputDeps; }
        // public JobHandle ManualUpdate(JobHandle inputDeps)
        {
            // Need to complete here to ensure we have the right Agent Count
            dependencies.Complete();

            foreach (var p in WorldProcessors)
            {
                p.ProcessWorld();
            }

            if (com != null)
            {

                bool anyWorldChanged = false;
                foreach (var idWorldPair in ExternalWorlds)
                {
                    anyWorldChanged = anyWorldChanged || idWorldPair.world.AgentCounter.Count > 0;
                }
                if (anyWorldChanged)
                {
                    if (!FirstMessageReceived)
                    {
                        // Unity must call advance to read the first message of Python.
                        // We do this only if there is already something to send 
                        com.Advance();
                        FirstMessageReceived = true;
                    }

                    foreach (var idWorldPair in ExternalWorlds)
                    {
                        com.WriteWorld(idWorldPair.name, idWorldPair.world);
                    }

                    // com.WriteSideChannelData(new byte[4]);
                    // TODO : Write side channel data
                    com.SetUnityReady();
                    var command = com.Advance();
                    ProcessReceivedSideChannelData(com.ReadAndClearSideChannelData());

                    switch (command)
                    {
                        case SharedMemoryCom.PythonCommand.RESET:
                            ResetAllWorlds();
                            // TODO : RESET logic
                            break;
                        case SharedMemoryCom.PythonCommand.CLOSE:
                            ResetAllWorlds();
#if UNITY_EDITOR
                            EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                            com = null;
                            break;
                        case SharedMemoryCom.PythonCommand.DEFAULT:
                            foreach (var idWorldPair in ExternalWorlds)
                            {
                                com.LoadWorld(idWorldPair.name, idWorldPair.world);
                                idWorldPair.world.SetActionReady();
                                idWorldPair.world.ResetDecisionsCounter();
                            }
                            break;
                        default:
                            break;

                    }
                }
            }

            inputDeps = JobHandle.CombineDependencies(inputDeps, FinalJobHandle);
            inputDeps.Complete();
            return inputDeps;
        }

        private void CheckWorldNotPresent(string policyId, MLAgentsWorld world)
        {
            if (RegisteredWorlds.Contains(world))
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
            RegisteredWorlds.Add(world);
            RegisteredWorldNames.Add(policyId);
        }

        private void ResetAllWorlds()
        {
            foreach (var p in WorldProcessors)
            {
                p.ResetWorld();
            }
            foreach (var IdWorldPair in ExternalWorlds)
            {
                IdWorldPair.world.ResetActionsCounter();
                IdWorldPair.world.ResetDecisionsCounter();
            }
        }

        private void ProcessReceivedSideChannelData(byte[] data)
        {
            if (data != null)
            {
                UnityEngine.Debug.Log("Received side channel data : " + data.Length);
            }
        }

        protected override void OnDestroy()
        {
            if (com != null)
            {
                com.Dispose();
            }
            // We do not dispose the world since this is not where they are created
        }
    }

}
