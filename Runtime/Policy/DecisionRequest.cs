using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using System;

namespace Unity.AI.MLAgents
{
    /// <summary>
    /// A DecisionRequest is a struct used to provide data about an Agent to a Policy.
    /// This data will be used to generate a decision after the Policy is processed.
    /// Adding data is done through a builder pattern.
    /// </summary>
    public struct DecisionRequest
    {
        private int m_Index;
        private Policy m_Policy;

        internal DecisionRequest(int index, Policy policy)
        {
            this.m_Index = index;
            this.m_Policy = policy;
        }

        /// <summary>
        /// Sets the reward that the Agent has accumulated since the last decision request.
        /// </summary>
        /// <param name="r"> The reward value </param>
        /// <returns> The DecisionRequest struct </returns>
        public DecisionRequest SetReward(float r)
        {
            m_Policy.DecisionRewards[m_Index] = r;
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
            if (branch > m_Policy.DiscreteActionBranches.Length)
            {
                throw new MLAgentsException("Unknown action branch used when setting mask.");
            }
            if (actionIndex > m_Policy.DiscreteActionBranches[branch])
            {
                throw new MLAgentsException("Index is out of bounds for requested action mask.");
            }
#endif
            var trueMaskIndex = m_Policy.DiscreteActionBranches.CumSumAt(branch) + actionIndex;
            m_Policy.DecisionActionMasks[trueMaskIndex + m_Policy.DiscreteActionBranches.Sum() * m_Index] = true;
            return this;
        }

        /// <summary>
        /// Sets the observation for a decision request.
        /// </summary>
        /// <param name="sensorNumber"> The index of the observation as provided when creating the associated Policy </param>
        /// <param name="sensor"> A struct strictly containing floats used as observation data </param>
        /// <returns> The DecisionRequest struct </returns>
        public DecisionRequest SetObservation<T>(int sensorNumber, T sensor) where T : struct
        {
            int inputSize = UnsafeUtility.SizeOf<T>() / sizeof(float);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            int3 s = m_Policy.SensorShapes[sensorNumber];
            int expectedInputSize = s.x * math.max(1, s.y) * math.max(1, s.z);
            if (inputSize != expectedInputSize)
            {
                throw new MLAgentsException(
                    $"Cannot set observation {sensorNumber} due to incompatible size of the input. Expected size : { expectedInputSize }, received size : { inputSize}");
            }
#endif
            int start = m_Policy.ObservationOffsets[sensorNumber];
            start += inputSize * m_Index;
            var tmp = m_Policy.DecisionObs.Slice(start, inputSize).SliceConvert<T>();
            tmp[0] = sensor;
            return this;
        }

        /// <summary>
        /// Sets the observation for a decision request using a categorical value.
        /// </summary>
        /// <param name="sensorNumber"> The index of the observation as provided when creating the associated Policy </param>
        /// <param name="sensor"> An integer containing the index of the categorical observation </param>
        /// <returns> The DecisionRequest struct </returns>
        public DecisionRequest SetObservation(int sensorNumber, int sensor)
        {
            int3 s = m_Policy.SensorShapes[sensorNumber];
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

            int start = m_Policy.ObservationOffsets[sensorNumber];
            start += maxValue * m_Index;
            for (int i = 0; i < maxValue; i++)
            {
                m_Policy.DecisionObs[start + i] = 0.0f;
            }
            m_Policy.DecisionObs[start + sensor] = 1.0f;
            return this;
        }

        /// <summary>
        /// Sets the observation for a decision request.
        /// </summary>
        /// <param name="sensorNumber"> The index of the observation as provided when creating the associated Policy </param>
        /// <param name="obs"> A NativeSlice of floats containing the observation data </param>
        /// <returns> The DecisionRequest struct </returns>
        public DecisionRequest SetObservationFromSlice(int sensorNumber, [ReadOnly] NativeSlice<float> obs)
        {
            int inputSize = obs.Length;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            int3 s = m_Policy.SensorShapes[sensorNumber];
            int expectedInputSize = s.x * math.max(1, s.y) * math.max(1, s.z);
            if (inputSize != expectedInputSize)
            {
                throw new MLAgentsException(
                    $"Cannot set observation {sensorNumber} due to incompatible size of the input. Expected size : {expectedInputSize}, received size : { inputSize}");
            }
#endif
            int start = m_Policy.ObservationOffsets[sensorNumber];
            start += inputSize * m_Index;
            m_Policy.DecisionObs.Slice(start, inputSize).CopyFrom(obs);
            return this;
        }
    }
}
