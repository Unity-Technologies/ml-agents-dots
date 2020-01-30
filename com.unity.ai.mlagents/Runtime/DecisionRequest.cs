using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using System;

namespace Unity.AI.MLAgents
{
    public struct DecisionRequest
    {
        private int index;
        private MLAgentsWorld world;

        internal DecisionRequest(int index, MLAgentsWorld world)
        {
            this.index = index;
            this.world = world;
        }

        public DecisionRequest SetReward(float r)
        {
            world.Rewards[index] = r;
            return this;
        }

        public DecisionRequest HasTerminated(bool done, bool maxStepReached)
        {
            world.DoneFlags[index] = done;
            world.MaxStepFlags[index] = maxStepReached;
            return this;
        }

        public DecisionRequest SetDiscreteActionMask(int branch, int actionIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (branch > world.DiscreteActionBranches.Length)
                {
                    throw new MLAgentsException("Unknown action branch used when setting mask.");
                }
                if (actionIndex > world.DiscreteActionBranches[branch])
                {
                    throw new MLAgentsException("Index is out of bounds for requested action mask.");
                }
#endif
            var trueMaskIndex = world.DiscreteActionBranches.CumSumAt(branch) + actionIndex;
            world.ActionMasks[trueMaskIndex + world.DiscreteActionBranches.Sum() * index] = true;
            return this;
        }

        public DecisionRequest SetObservation<T>(int sensorNumber, T sensor) where T : struct
        {
            int inputSize = UnsafeUtility.SizeOf<T>() / sizeof(float);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                int3 s = world.SensorShapes[sensorNumber];
                int expectedInputSize = s.x * math.max(1, s.y) * math.max(1, s.z);
                if (inputSize != expectedInputSize)
                {
                    throw new MLAgentsException(
                        "Cannot set observation due to incompatible size of the input. Expected size : " + expectedInputSize + ", received size : " + inputSize);
                }
#endif
            int start = world.ObservationOffsets[sensorNumber];
            start += inputSize * index;
            var tmp = world.Sensors.Slice(start, inputSize).SliceConvert<T>();
            tmp[0] = sensor;
            return this;
        }

        public DecisionRequest SetObservationFromSlice(int sensorNumber, [ReadOnly] NativeSlice<float> obs)
        {
            int inputSize = obs.Length;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                int3 s = world.SensorShapes[sensorNumber];
                int expectedInputSize = s.x * math.max(1, s.y) * math.max(1, s.z);
                if (inputSize != expectedInputSize)
                {
                    throw new MLAgentsException(
                        "Cannot set observation due to incompatible size of the input. Expected size : " + expectedInputSize + ", received size : " + inputSize);
                }
#endif
            int start = world.ObservationOffsets[sensorNumber];
            start += inputSize * index;
            world.Sensors.Slice(start, inputSize).CopyFrom(obs);
            return this;
        }
    }
}
