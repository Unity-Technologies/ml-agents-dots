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
        private int m_Index;
        private MLAgentsWorld m_World;

        internal DecisionRequest(int index, MLAgentsWorld world)
        {
            this.m_Index = index;
            this.m_World = world;
        }

        /// <summary>
        /// Sets the reward that the Agent has accumulated since the last decision request.
        /// </summary>
        /// <param name="r"> The reward value </param>
        /// <returns> The DecisionRequest struct </returns>
        public DecisionRequest SetReward(float r)
        {
            m_World.DecisionRewards[m_Index] = r;
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
            if (m_World.ActionType == ActionType.CONTINUOUS)
            {
                throw new MLAgentsException("SetDiscreteActionMask can only be used with discrete acton space.");
            }
            if (branch > m_World.DiscreteActionBranches.Length)
            {
                throw new MLAgentsException("Unknown action branch used when setting mask.");
            }
            if (actionIndex > m_World.DiscreteActionBranches[branch])
            {
                throw new MLAgentsException("Index is out of bounds for requested action mask.");
            }
#endif
            var trueMaskIndex = m_World.DiscreteActionBranches.CumSumAt(branch) + actionIndex;
            m_World.DecisionActionMasks[trueMaskIndex + m_World.DiscreteActionBranches.Sum() * m_Index] = true;
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
            int3 s = m_World.SensorShapes[sensorNumber];
            int expectedInputSize = s.x * math.max(1, s.y) * math.max(1, s.z);
            if (inputSize != expectedInputSize)
            {
                throw new MLAgentsException(
                    $"Cannot set observation due to incompatible size of the input. Expected size : { expectedInputSize }, received size : { inputSize}");
            }
#endif
            int start = m_World.ObservationOffsets[sensorNumber];
            start += inputSize * m_Index;
            var tmp = m_World.DecisionObs.Slice(start, inputSize).SliceConvert<T>();
            tmp[0] = sensor;
            return this;
        }

        /// <summary>
        /// Sets the observation for a decision request using a categorical value.
        /// </summary>
        /// <param name="sensorNumber"> The index of the observation as provided when creating the associated MLAgentsWorld </param>
        /// <param name="sensor"> An integer containing the index of the categorical observation </param>
        /// <returns> The DecisionRequest struct </returns>
        public DecisionRequest SetObservation(int sensorNumber, int sensor)
        {
            int3 s = m_World.SensorShapes[sensorNumber];
            int maxValue = s.x;
#if ENABLE_UNITY_COLLECTIONS_CHECKS

            if (s.y != 0 || s.z != 0)
            {
                throw new MLAgentsException(
                    $"Categorical observation must have a shape (max_category, 0, 0)");
            }
            if (sensor > maxValue)
            {
                throw new MLAgentsException(
                    $"Categorical observation is out of bound for observation {sensorNumber} with maximum {maxValue} (received {sensor}.");
            }
#endif
            int start = m_World.ObservationOffsets[sensorNumber];
            start += maxValue * m_Index;
            m_World.DecisionObs[start + sensor] = 1.0f;
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
            int3 s = m_World.SensorShapes[sensorNumber];
            int expectedInputSize = s.x * math.max(1, s.y) * math.max(1, s.z);
            if (inputSize != expectedInputSize)
            {
                throw new MLAgentsException(
                    $"Cannot set observation due to incompatible size of the input. Expected size : {expectedInputSize}, received size : { inputSize}");
            }
#endif
            int start = m_World.ObservationOffsets[sensorNumber];
            start += inputSize * m_Index;
            m_World.DecisionObs.Slice(start, inputSize).CopyFrom(obs);
            return this;
        }
    }
}
