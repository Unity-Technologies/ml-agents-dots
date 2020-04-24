using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using System;

namespace Unity.AI.MLAgents
{
    /// <summary>
    /// A EpisodeTermination is a struct used to provide data about an Agent to a MLAgentsWorld.
    /// This data will be used to notify of the end of the episode of an Agent.
    /// Adding data is done through a builder pattern.
    /// </summary>
    public struct EpisodeTermination
    {
        private int m_Index;
        private MLAgentsWorld m_World;

        internal EpisodeTermination(int index, MLAgentsWorld world)
        {
            this.m_Index = index;
            this.m_World = world;
        }

        /// <summary>
        /// Sets the reward that the Agent has accumulated since the last decision request.
        /// Add any "end of episode" reward.
        /// </summary>
        /// <param name="r"> The reward value </param>
        /// <returns> The EpisodeTermination struct </returns>
        public EpisodeTermination SetReward(float r)
        {
            m_World.TerminationRewards[m_Index] = r;
            return this;
        }

        /// <summary>
        /// Sets the observation for of the end of the Episode.
        /// </summary>
        /// <param name="sensorNumber"> The index of the observation as provided when creating the associated MLAgentsWorld </param>
        /// <param name="sensor"> A struct strictly containing floats used as observation data </param>
        /// <returns> The EpisodeTermination struct </returns>
        public EpisodeTermination SetObservation<T>(int sensorNumber, T sensor) where T : struct
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
            var tmp = m_World.TerminationObs.Slice(start, inputSize).SliceConvert<T>();
            tmp[0] = sensor;
            return this;
        }

        /// <summary>
        /// Sets the last observation the Agent perceives before ending the episode.
        /// </summary>
        /// <param name="sensorNumber"> The index of the observation as provided when creating the associated MLAgentsWorld </param>
        /// <param name="obs"> A NativeSlice of floats containing the observation data </param>
        /// <returns> The EpisodeTermination struct </returns>
        public EpisodeTermination SetObservationFromSlice(int sensorNumber, [ReadOnly] NativeSlice<float> obs)
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
            m_World.TerminationObs.Slice(start, inputSize).CopyFrom(obs);
            return this;
        }
    }
}
