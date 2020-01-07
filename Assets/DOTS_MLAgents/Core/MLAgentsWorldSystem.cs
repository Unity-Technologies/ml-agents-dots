using Unity.Entities;
using Unity.Jobs;
using System.Collections.Generic;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace DOTS_MLAgents.Core
{

    public enum Mode
    {
        COMMUNICATION,
        BARRACUDA,
        HEURISTIC
    }
    // [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class MLAgentsWorldSystem : JobComponentSystem // Should this be a ISimulation from Unity.Physics ?
    {

        private JobHandle dependencies;
        public JobHandle FinalJobHandle;

        private SharedMemoryCom com;

        private List<IWorldProcessor> WorldProcessors;
        private HashSet<MLAgentsWorld> RegisteredWorlds;
        private HashSet<string> RegisteredWorldNames;

        public void SubscribeWorld(string policyId, MLAgentsWorld world, /*Optional barracuda model*/)
        {
            CheckWorldNotPresent(policyId, world);
            if (com != null)
            {
                WorldProcessors.Add(new ExternalWorldProcessor(policyId, world, com));
            }
            else
            {
                // TODO   
            }
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

        public void SubscribeWorldWithHeuristic<T>(string policyId, MLAgentsWorld world, Func<T> lambda /*Optional barracuda model*/) where T : struct
        {
            CheckWorldNotPresent(policyId, world);
            if (com != null)
            {
                WorldProcessors.Add(new ExternalWorldProcessor(policyId, world, com));
                return;
            }
            WorldProcessors.Add(new HeuristicWorldProcessor<T>(world, lambda));
        }

        protected override void OnCreate()
        {
            WorldProcessors = new List<IWorldProcessor>();
            RegisteredWorlds = new HashSet<MLAgentsWorld>();
            RegisteredWorldNames = new HashSet<string>();

            dependencies = new JobHandle();
            FinalJobHandle = new JobHandle();

            var path = ArgParser.ReadSharedMemoryPathFromArgs();
            if (path == null)
            {
                // throw new MLAgentsException("Could not connect.");
                UnityEngine.Debug.Log("Could not connect");
            }
            else
            {
                com = new SharedMemoryCom(path);
                com.Advance();
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
                p.WriteWorldData();
            }
            foreach (var p in WorldProcessors)
            {
                p.ProcessWorldData();
            }

            if (com != null)
            {
                // com.WriteSideChannelData(new byte[4]);
                com.SetUnityReady();
                var command = com.Advance();


                switch (command)
                {
                    case SharedMemoryCom.PythonCommand.RESET:
                        ProcessReceivedSideChannelData(com.ReadAndClearSideChannelData());
                        // TODO : RESET
                        break;
                    case SharedMemoryCom.PythonCommand.CLOSE:
#if UNITY_EDITOR
                        EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                        break;
                    case SharedMemoryCom.PythonCommand.DEFAULT:
                        ProcessReceivedSideChannelData(com.ReadAndClearSideChannelData());
                        foreach (var p in WorldProcessors)
                        {
                            p.RetrieveWorldData();
                        }
                        break;
                    default:
                        break;

                }
            }
            else
            {
                foreach (var p in WorldProcessors)
                {
                    p.RetrieveWorldData();
                }
            }



            inputDeps = JobHandle.CombineDependencies(inputDeps, FinalJobHandle);
            inputDeps.Complete();
            return inputDeps;
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
