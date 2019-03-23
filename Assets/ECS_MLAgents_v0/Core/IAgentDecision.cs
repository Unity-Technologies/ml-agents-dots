using Unity.Collections;
using Unity.Entities;

namespace ECS_MLAgents_v0.Core
{
    /*
     * The Interface to define a Decision process by which a bach of agent updates its actuator
     * based on the information present in the sensor.
     */
    public interface IAgentDecision<TS, TA> 
        where TS : struct, IComponentData
        where TA : struct, IComponentData 
    {
        /// <summary>
        /// DecideBatch updates the actuators of the agents present in the batch from
        /// the data present in the sensors. 
        /// </summary>
        /// <param name="sensors">The aggregated data for the sensor information present in the
        /// batch. T.</param>
        /// <param name="actuators">The aggregated data for the actuator information present in the
        /// batch. </param>
        void BatchProcess(ref NativeArray<TS> sensors, ref NativeArray<TA> actuators);

    }

}
