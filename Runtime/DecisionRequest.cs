using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using System;

namespace Unity.AI.MLAgents
{
    /// <summary>
    /// A DecisionRequest is a struct used to provide data about an Agent to a MLAgentsWorld.
    /// This data will be used to generate a decision after the world is processed.
    /// Adding data is done through a builder pattern.
    /// </summary>
    public struct DecisionRequest
    {
        private int index;
        private MLAgentsWorld world;

        internal DecisionRequest(int index, MLAgentsWorld world)
        {
            this.index = index;
            this.world = world;
        }

        /// <summary>
        /// Sets the reward that the Agent has accumulated since the last decision request.
        /// </summary>
        /// <param name="r"> The reward value </param>
        /// <returns> The DecisionRequest struct </returns>
        public DecisionRequest SetReward(float r)
        {
            world.Rewards[index] = r;
            return this;
        }

        /// <summary>
        /// Specifies if the Agent terminated after the last decision request.
        /// Note : If the Agent timed out, the doneStatus will be set to true.
        /// </summary>
        /// <param name="doneStatus"> Wether the Agent has terminated (either succeeded or failed at its task) </param>
        /// <param name="maxStepReached"> Wether the Agent took too long to complete to terminate </param>
        /// <returns> The DecisionRequest struct </returns>
        public DecisionRequest HasTerminated(bool doneStatus, bool timedOut)
        {
            doneStatus = doneStatus || timedOut;
            world.DoneFlags[index] = doneStatus;
            world.MaxStepFlags[index] = timedOut;
            return this;
        }

        /// <summary>
        /// Specifies that a discrete action is not available for the next decision.
        /// Note : This is only available is discrete action spaces.
        /// </summary>
        /// <param name="branch"> The branch of the action to be masked </param>
        /// <param name="actionIndex"> The index of the action to be masked </param>
        /// <returns> The DecisionRequest struct </returns>
        public DecisionRequest SetDiscreteActionMask(int branch, int actionIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (world.ActionType == ActionType.CONTINUOUS)
            {
                throw new MLAgentsException("SetDiscreteActionMask can only be used with discrete acton space.");
            }
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

        /// <summary>
        /// Sets the observation for a decision request.
        /// </summary>
        /// <param name="sensorNumber"> The index of the observation as provided when creating the associated MLAgentsWorld </param>
        /// <param name="sensor"> A struct strictly containing floats used as observation data </param>
        /// <returns> The DecisionRequest struct </returns>
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

        /// <summary>
        /// Sets the observation for a decision request.
        /// </summary>
        /// <param name="sensorNumber"> The index of the observation as provided when creating the associated MLAgentsWorld </param>
        /// <param name="obs"> A NativeSlice of floats containing the observation data </param>
        /// <returns> The DecisionRequest struct </returns>
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
