using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace DOTS_MLAgents.Core
{
    // The struct I want the job to be called with (for now float3 is hard coded)
    public struct ActuatorEvent
    {
        public Entity Entity;
        public NativeSlice<float> ContinuousActionSlice;
        public NativeSlice<int> DiscreteActionSlice;

        public void GetDiscreteAction<T>(out T action) where T : struct
        {
            // Do some check
            action = DiscreteActionSlice.SliceConvert<T>()[0];
        }
        public void GetContinuousAction<T>(out T action) where T : struct
        {
            // Do some check
            action = ContinuousActionSlice.SliceConvert<T>()[0];
        }
    }

    // The signature of the Job the user is able to create
    [JobProducerType(typeof(IActuatorJobExtensions.ActuatorDataJobProcess<>))]
    public interface IActuatorJob
    {
        void Execute(ActuatorEvent jobData);
    }

    public static class IActuatorJobExtensions
    {

        // The scheduler the user will call when scheduling their job
        public static unsafe JobHandle Schedule<T>(this T jobData, MLAgentsWorld mlagentsWorld, JobHandle inputDeps)
            where T : struct, IActuatorJob
        {
            return ScheduleImpl(jobData, mlagentsWorld, inputDeps);
        }

        // Passing this along
        internal static unsafe JobHandle ScheduleImpl<T>(this T jobData, MLAgentsWorld mlagentsWorld, JobHandle inputDeps)
         where T : struct, IActuatorJob
        {
            // inputDeps.Complete();
            // Creating a data struct that contains the data the user passed into the job (This is what T is here)
            var data = new ActionEventJobData<T>
            {
                UserJobData = jobData,
                EventReader = mlagentsWorld // Need to create this before hand with the actuator data
            };

            // Scheduling a Job out of thin air by using a pointer called jobReflectionData in the ActuatorSystemJobStruct
            // UnityEngine.Debug.Log("Pointer : " + new IntPtr(UnsafeUtility.AddressOf(ref data)));
            var parameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref data), ActuatorDataJobProcess<T>.Initialize(), inputDeps, ScheduleMode.Batched);
            return JobsUtility.Schedule(ref parameters);

        }

        // This is the struct containing all the data needed from both the user and us
        internal unsafe struct ActionEventJobData<T> where T : struct
        {
            public T UserJobData;
            [NativeDisableContainerSafetyRestriction] public MLAgentsWorld EventReader;

            #region experimental

            // public Enumerator GetEnumerator()
            // {
            //     return new Enumerator(EventReader);
            // }

            // public struct Enumerator /* : IEnumerator<TriggerEvent> */
            // {
            //     ActionDataHolder EventReader;
            //     private int m_CurrentWorkItem;
            //     private readonly int m_NumWorkItems;
            //     public ActuatorEvent Current { get; private set; }

            //     public Enumerator(ActionDataHolder eventReader)
            //     {
            //         m_CurrentWorkItem = 0;
            //         Current = default;
            //         m_NumWorkItems = eventReader.NumAgents;
            //         EventReader = eventReader;

            //     }

            //     public bool MoveNext()
            //     {
            //         if (m_CurrentWorkItem < m_NumWorkItems)
            //         {
            //             if (m_CurrentWorkItem < m_NumWorkItems)
            //                 Current = new ActuatorEvent
            //                 {
            //                     Entity = EventReader.AgentIds[m_CurrentWorkItem],
            //                     //     Action = 
            //                     //         EventReader.Actuators[m_CurrentWorkItem * 3 + 0],
            //                     //         EventReader.Actuators[m_CurrentWorkItem * 3 + 1],
            //                     //         EventReader.Actuators[m_CurrentWorkItem * 3 + 2]
            //                     // )
            //                 };
            //             AdvanceReader();
            //             return true;
            //         }
            //         return false;
            //     }

            //     private void AdvanceReader()
            //     {
            //         while (m_CurrentWorkItem < m_NumWorkItems)
            //         {
            //             m_CurrentWorkItem++;
            //         }
            //     }
            // }
            #endregion

        }

        internal struct ActuatorDataJobProcess<T> where T : struct, IActuatorJob
        {
            #region The pointer to the job
            public static IntPtr jobReflectionData; // This is a pointer to the Job so we know how to schedule it

            public static IntPtr Initialize()
            {
                // if (jobReflectionData == IntPtr.Zero)
                jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(ActionEventJobData<T>), typeof(T), JobType.Single, (ExecuteJobFunction)Execute);
                return jobReflectionData;
            }
            #endregion


            public delegate void ExecuteJobFunction(ref ActionEventJobData<T> jobData, IntPtr listDataPtr, IntPtr unusedPtr, ref JobRanges ranges, int jobIndex);
            public static unsafe void Execute(ref ActionEventJobData<T> jobData, IntPtr listDataPtr, IntPtr unusedPtr, ref JobRanges ranges, int jobIndex)
            {

                int size = jobData.EventReader.ActionSize;
                for (int i = 0; i < jobData.EventReader.AgentCounter.Count; i++)
                {

                    jobData.UserJobData.Execute(new ActuatorEvent
                    {
                        Entity = jobData.EventReader.AgentIds[i],
                        // DiscreteActionSlice = jobData.EventReader.DiscreteActuators.Slice(i * size, size),
                        ContinuousActionSlice = jobData.EventReader.ContinuousActuators.Slice(i * size, size)
                    });
                }
                jobData.EventReader.AgentCounter.Count = 0;

            }
        }
    }
}