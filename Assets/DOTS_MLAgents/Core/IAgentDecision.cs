using Unity.Collections;
using Unity.Entities;

namespace DOTS_MLAgents.Core
{
    /*
     * The Interface to define a Decision process by which a bach of agent updates its actuator
     * based on the information present in the sensor.
     */
    public interface IAgentDecision<TS, TA> 
        where TS : struct
        where TA : struct
    {
        /// <summary>
        /// DecideBatch updates the actuators of the agents present in the batch from
        /// the data present in the sensors. 
        /// </summary>
        /// <param name="sensors">The aggregated data for the sensor information present in the
        /// batch. T.</param>
        /// <param name="actuators">The aggregated data for the actuator information present in the
        /// batch. </param>
        void BatchProcess(ref NativeArray<TS> sensors, ref NativeArray<TA> actuators, int offset = 0, int size = -1);

        // TODO : It is debatable wether or not we want to enforce the type here
    }

}
