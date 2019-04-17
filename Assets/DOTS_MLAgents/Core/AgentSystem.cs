using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;

namespace DOTS_MLAgents.Core
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
    public abstract class AgentSystem<TS, TA> : ComponentSystem, IAgentSystem<TS, TA>
        where TS : struct, IComponentData
        where TA : struct, IComponentData
    {   
        
        public IDecisionRequester DecisionRequester { get; set; }
        
        public IAgentDecision<TS, TA> Decision { get; set; }
        
        private EntityQuery _componentGroup;
        private int _sensorSize;
        private int _actuatorSize;

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
        }

        public void SetNewComponentGroup(params ComponentType[] t)
        {
            _logger.Log("UpdateComponentGroup");
            var componentTypes = t.ToList();
            componentTypes.Add(ComponentType.ReadOnly(typeof(TS)));
            componentTypes.Add(typeof(TA));
            _componentGroup = GetEntityQuery(componentTypes.ToArray());
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

        protected override void OnUpdate()
        {
            _logger.Log("OnUpdate");

            DecisionRequester.Update();
            if (!DecisionRequester.Ready)
            {
                return;
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
                return;
            }
            
            /*
             * Collecting the DataArray necessary for the computation
             */
            _logger.Log("On update with "+_componentGroup.CalculateLength()+" entities");
            var sensors = _componentGroup.ToComponentDataArray<TS>(Allocator.TempJob);
            var actuators = new NativeArray<TA>(sensors.Length, Allocator.TempJob);

            Decision.BatchProcess(ref sensors, ref actuators, 0, nAgents);

            _componentGroup.CopyFromComponentDataArray<TA>(actuators);

            sensors.Dispose();
            actuators.Dispose();
        }
    }
}
