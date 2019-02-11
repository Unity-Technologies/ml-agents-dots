using Unity.Collections;
using Unity.Jobs;

namespace ECS_MLAgents_v0.Core
{
    /*
     * The Interface to define a Decision process by which a bach of agent updates its actuator
     * based on the information present in the sensor.
     */
    public interface IAgentDecision
    {
        /// <summary>
        /// DecideBatch updates the aggregated actuators of the agents present in the batch from
        /// the aggregated actuators. 
        /// </summary>
        /// <param name="sensor">The aggregated data for the sensor information present in the
        /// batch. The sensor data is linearly arranged.</param>
        /// <param name="actuator">The aggregated data for the actuator information present in the
        /// batch. The sensor data is linearly arranged.</param>
        /// <param name="sensorSize">The number of float values present in a sensor for one agent
        /// </param>
        /// <param name="actuatorSize">The number of float values present in an actuator
        /// for one agent</param>
        /// <param name="nAgents">The number of agents present in the batch</param>
        /// <param name="handle">The JobHandle for the input dependencies.</param>
        /// <returns>The Job Handle for the output dependencies.</returns>
        JobHandle DecideBatch(ref NativeArray<float> sensor, 
            ref NativeArray<float> actuator, 
            int sensorSize, 
            int actuatorSize,
            int nAgents,
            JobHandle handle);
    }
}
