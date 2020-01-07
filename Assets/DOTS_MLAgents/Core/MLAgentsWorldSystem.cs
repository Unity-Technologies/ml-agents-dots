using Unity.Entities;
using Unity.Jobs;
using System.Collections.Generic;
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

        public Mode MODE = Mode.COMMUNICATION;

        private JobHandle dependencies;
        public JobHandle FinalJobHandle;

        private SharedMemoryCom com;

        private Dictionary<string, MLAgentsWorld> WorldDict;

        public void SubscribeWorld(string policyId, MLAgentsWorld world)
        {
            if (!WorldDict.ContainsKey(policyId))
            {
                WorldDict[policyId] = world;
            }
            else
            {
                throw new MLAgentsException(
                    string.Format(
                        "An MLAgentsWorld has already been subscribed using the key {0}",
                        policyId)
                        );
            }
        }

        protected override void OnCreate()
        {
            WorldDict = new Dictionary<string, MLAgentsWorld>();
            dependencies = new JobHandle();
            FinalJobHandle = new JobHandle();
            if (MODE == Mode.COMMUNICATION)
            {
                var path = ArgParser.ReadSharedMemoryPathFromArgs();
                if (path == null)
                {
                    // throw new MLAgentsException("Could not connect.");
                    UnityEngine.Debug.Log("Could not connect");
                    MODE = Mode.BARRACUDA;
                }
                else
                {
                    com = new SharedMemoryCom(path);
                    com.Advance();
                }
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

            if (MODE == Mode.COMMUNICATION)
            {
                foreach (var val in WorldDict)
                {
                    var world = val.Value;
                    com.WriteWorld(val.Key, world);
                }
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
                        foreach (var val in WorldDict)
                        {
                            var world = val.Value;
                            com.LoadWorld(val.Key, world);
                        }
                        break;
                    default:
                        break;

                }
            }
            else if (MODE == Mode.HEURISTIC)
            {

            }
            else if (MODE == Mode.BARRACUDA)
            {
                // ModelStore[val.Key].ProcessWorld(world);
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
            if (MODE == Mode.COMMUNICATION)
            {
                com.Dispose();
            }
            // We do not dispose the world since this is not where they are created
        }
    }

}
