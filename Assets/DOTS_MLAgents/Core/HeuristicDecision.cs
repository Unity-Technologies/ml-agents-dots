using DOTS_MLAgents.Core;
using Unity.Entities;
using Unity.Collections;
using System;
using Unity.Jobs;
using Unity.Burst;


namespace DOTS_MLAgents.Core {
    public class HeuristicDecision<TS, TA> : IAgentDecision<TS, TA> 
        where TS : struct, IComponentData
        where TA : struct, IComponentData 
    {

        Func<TS, TA> _lambda;

        public HeuristicDecision(Func<TS, TA> lambda) => _lambda = lambda;

        public void BatchProcess(ref NativeArray<TS> sensors, ref NativeArray<TA> actuators , int offset = 0, int size = -1)
        {
            if (size ==-1){
                size = sensors.Length - offset;
            }
            
            for (var i =offset ; i < offset+size; i++ ){
                actuators[i] = _lambda(sensors[i]);
            }
        }
    }
}
