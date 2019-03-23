using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;

namespace ECS_MLAgents_v0.Core
{
    
    /*
     * AgentSystem<Sensor, Actuator> is a JobComponentSystem that updates the Actuator based of
     * the data present in Sensor for all of the compatible Entities. The user can create a new
     * AgentSystem by defining a class this way :
     *
     *     public class MyAgentSystem : AgentSystem<MySensor, MyActuator> { }
     *
     * The user can modify properties of MyAgentSystem to modify which Entities will be
     * affected by MyAgentSystem.
     *
     * To access the instance of MyAgentSystem, use :
     * 
     *     World.Active.GetExistingManager<MyAgentSystem>(); 
     * 
     * It is the responsibility of the user to create and populate
     * the MySensor of each Entity as well as create and use the data in the MyActuator of each
     * Entity. MySensor and MyActuator must be IComponentData struct that only contains blittable
     * float fields
     * Note that an Agent IComponentData must be attached to a Entity to be affected by
     * MyAgentSystem.
     *
     * At each call to OnUpdate, the Data from the sensors of compatible entities will be
     * aggregated into a single NativeArray<float>. The AgentSystem will then process this
     * data in batch and generate a new NativeArray<float> that will be used to populate the
     * Actuator data of all compatible Entities.
     */
    public abstract class AgentSystem<TS, TA> : JobComponentSystem, IAgentSystem<TS, TA>
        where TS : struct, IComponentData
        where TA : struct, IComponentData
    {   
        private const int INITIAL_MEMORY_SIZE = 1024;
        
        private int _currentNAgents = INITIAL_MEMORY_SIZE;
        
        public IDecisionRequester DecisionRequester { get; set; }
        private int _phase;
        
        public IAgentDecision<TS, TA> Decision { get; set; }
        
        private ComponentGroup _componentGroup;
        private int _sensorSize;
        private int _actuatorSize;
        // TODO : Make sure there is not extra cost for memory allocation here and when copying
        private NativeArray<TS> _sensorTensor =
            new NativeArray<TS>(INITIAL_MEMORY_SIZE, Allocator.Persistent);
        private NativeArray<TA> _actuatorTensor =
            new NativeArray<TA>(INITIAL_MEMORY_SIZE, Allocator.Persistent);

        // TODO : Decide if we want to keep at all
        private Logger _logger;

        protected override void OnCreateManager()
        {
            if (DecisionRequester == null)
            {
                DecisionRequester = new FixedCountRequester();
            }

            _logger = new Logger(GetType().Name);
            _logger.Log("OnCreateManager");
            SetNewComponentGroup();
            _sensorSize = UnsafeUtility.SizeOf<TS>();
            _actuatorSize = UnsafeUtility.SizeOf<TA>();
        }

        protected override void OnDestroyManager()
        {
            _logger.Log("OnDestroyManager");
            _sensorTensor.Dispose();
            _actuatorTensor.Dispose();
        }

        public void SetNewComponentGroup(params ComponentType[] t)
        {
            _logger.Log("UpdateComponentGroup");
            var componentTypes = t.ToList();
            componentTypes.Add(ComponentType.ReadOnly(typeof(TS)));
            componentTypes.Add(typeof(TA));
            componentTypes.Add(typeof(Agent));
            _componentGroup = GetComponentGroup(componentTypes.ToArray());
        }

        public void SetFilter<T>(T filter) where T : struct, ISharedComponentData
        {
            _componentGroup.SetFilter<T>(filter);
        }

        public void SetFilter<T0, T1>(T0 filterA, T1 filterB) 
            where T0 : struct, ISharedComponentData
            where T1 : struct, ISharedComponentData
        {
            _componentGroup.SetFilter<T0, T1>(filterA, filterB);
        }
        
        public void ResetFilter() 
        {
            _componentGroup.ResetFilter();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            _logger.Log("OnUpdate");

            DecisionRequester.Update();
            if (!DecisionRequester.Ready)
            {
                return inputDeps;
            }
            DecisionRequester.Reset();
            

            var nAgents = _componentGroup.CalculateLength();
            
            /*
             * If the AgentSystem is not active or if there is no Decision component on the
             * AgentSystem or if no Entities match the ComponentGroups' requirement, the Update
             * of the Actuators returns immediately.
             */
            if (Decision == null || nAgents == 0)
            {
                return inputDeps;
            }

            /*
             * If there was more agents than allowed by the memory allocation of the sensor or
             * actuator, then the size is updated to the required size.
             */
            if (nAgents > _currentNAgents)
            {
                _currentNAgents = nAgents;
                _sensorTensor.Dispose();
                _actuatorTensor.Dispose();
                _sensorTensor = new NativeArray<TS>(_currentNAgents, Allocator.Persistent);
                _actuatorTensor = new NativeArray<TA>(_currentNAgents, Allocator.Persistent);
            }
            
            /*
             * Collecting the DataArray necessary for the computation
             */
            _logger.Log("On update with "+_componentGroup.CalculateLength()+" entities");
            var sensors = _componentGroup.GetComponentDataArray<TS>();
            var actuators = _componentGroup.GetComponentDataArray<TA>();
            var agents = _componentGroup.GetComponentDataArray<Agent>();
            var handle = inputDeps;
            
            /*
             * Copy the data from the sensors to the sensor NativeArray<float> for batch processing.
             */
            var copySensorsJob = new CopySensorsJob
            {
                Sensors = sensors,
                SensorTensor = _sensorTensor,
            };
            handle = copySensorsJob.Schedule(nAgents, 64, handle);

            handle.Complete();

            Decision.BatchProcess(ref _sensorTensor, ref _actuatorTensor);

                        /*
             * Copy the data from the actuator NativeArray<float> to the actuators of each entity.
             */
            var copyActuatorsJob = new CopyActuatorsJob
            {
                ActuatorTensor = _actuatorTensor,
                Actuators = actuators,

            }; 

            return copyActuatorsJob.Schedule(nAgents, 64, handle);

        }

        /*
         * This IJobParallelFor copied the Sensor data into a NativeArray<float>
         */
       [BurstCompile]
        private struct CopySensorsJob : IJobParallelFor
        {
            [ReadOnly] public ComponentDataArray<TS> Sensors;
            public NativeArray<TS> SensorTensor;
            
            public void Execute(int i)
            {
                SensorTensor[i] = Sensors[i];
            }
        }
        
        /*
         * This IJobParallelFor copies the Actuator data to the appropriate IComponentData
         */
//        [BurstCompile]
        private struct CopyActuatorsJob : IJobParallelFor
        {
            public ComponentDataArray<TA> Actuators;
            public NativeArray<TA> ActuatorTensor;
            
            public void Execute(int i)
            {
                Actuators[i] = ActuatorTensor[i];
            }
        }
    }
}
