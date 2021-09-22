using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Collections;

namespace Unity.AI.MLAgents
{
    /// <summary>
    /// This struct will be provided to the user in a custom job inheriting from IActuatorJob
    /// </summary>
    public struct ActuatorEvent
    {
        /// <summary>
        /// The size of the continuous action vector.
        /// </summary>
        [ReadOnly] public int ContinuousActionSize;

        /// <summary>
        /// The size of the discrete action vector.
        /// </summary>
        [ReadOnly] public int DiscreteActionSize;

        /// <summary>
        /// The Entity the vector action is for.
        /// </summary>
        [ReadOnly] public Entity Entity;


        [ReadOnly] internal NativeSlice<float> ContinuousActionSlice;
        [ReadOnly] internal NativeSlice<int> DiscreteActionSlice;

        /// <summary>
        /// Retrieve the continuous action that was decided for the Entity.
        /// This method uses a generic type, as such you must provide a
        /// type that is compatible with the Continuous Action Size
        /// for this Policy.
        /// </summary>
        /// <typeparam name="T"> The type of action struct.</typeparam>
        /// <returns> The continuous action struct for the Entity.</returns>
        public T GetContinuousAction<T>() where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (ContinuousActionSize != UnsafeUtility.SizeOf<T>() / 4)
            {
                var receivedSize = UnsafeUtility.SizeOf<T>() / 4;
                throw new MLAgentsException($"Action space size does not match for action. Expected {ContinuousActionSize} but received {receivedSize}");
            }
#endif
            return ContinuousActionSlice.SliceConvert<T>()[0];
        }

        /// <summary>
        /// Retrieve the discrete action that was decided for the Entity.
        /// This method uses a generic type, as such you must provide a
        /// type that is compatible with the Discrete Action Size
        /// for this Policy.
        /// </summary>
        /// <typeparam name="T"> The type of action struct.</typeparam>
        /// <returns> The discrete action struct for the Entity.</returns>
        public T GetDiscreteAction<T>() where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (DiscreteActionSize != UnsafeUtility.SizeOf<T>() / 4)
            {
                var receivedSize = UnsafeUtility.SizeOf<T>() / 4;
                throw new MLAgentsException($"Action space size does not match for action. Expected {DiscreteActionSize} but received {receivedSize}");
            }
#endif
            return DiscreteActionSlice.SliceConvert<T>()[0];
        }
    }


    /// <summary>
    /// The signature of the Job used to retrieve actuators values from a Policy
    /// </summary>
    [JobProducerType(typeof(IActuatorJobExtensions.ActuatorDataJobProcess<>))]
    public interface IActuatorJob
    {
        void Execute(ActuatorEvent jobData);
    }

    public static class IActuatorJobExtensions
    {
        /// <summary>
        /// Schedule the Job that will generate the action data for the Entities that requested a decision.
        /// </summary>
        /// <param name="jobData"> The IActuatorJob struct.</param>
        /// <param name="policy"> The Policy containing the data needed for decision making.</param>
        /// <param name="inputDeps"> The jobHandle for the job.</param>
        /// <typeparam name="T"> The type of the IActuatorData struct.</typeparam>
        /// <returns> The updated jobHandle for the job.</returns>
        public static unsafe JobHandle Schedule<T>(this T jobData, Policy policy, JobHandle inputDeps)
            where T : struct, IActuatorJob
        {
            inputDeps.Complete();
            Academy.Instance.UpdatePolicy(policy);
            if (policy.ActionCounter.Count == 0)
            {
                return inputDeps;
            }
            return ScheduleImpl(jobData, policy, inputDeps);
        }

        internal static unsafe JobHandle ScheduleImpl<T>(this T jobData, Policy policy, JobHandle inputDeps)
            where T : struct, IActuatorJob
        {
            var data = new ActionEventJobData<T>
            {
                UserJobData = jobData,
                Policy = policy
            };

            // Scheduling a Job out of thin air by using a pointer called jobReflectionData in the ActuatorSystemJobStruct
            var parameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref data), ActuatorDataJobProcess<T>.Initialize(), inputDeps, ScheduleMode.Parallel);
            return JobsUtility.Schedule(ref parameters);
        }

        // This is the struct containing all the data needed from both the user and the Policy
        internal unsafe struct ActionEventJobData<T> where T : struct
        {
            public T UserJobData;
            [NativeDisableContainerSafetyRestriction] public Policy Policy;
        }

        internal struct ActuatorDataJobProcess<T> where T : struct, IActuatorJob
        {
            #region The pointer to the job
            public static IntPtr jobReflectionData; // This is a pointer to the Job so we know how to schedule it

            public static IntPtr Initialize()
            {
                if (jobReflectionData == IntPtr.Zero)
                { jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(ActionEventJobData<T>), typeof(T), (ExecuteJobFunction)Execute); }
                return jobReflectionData;
            }

            #endregion


            public delegate void ExecuteJobFunction(ref ActionEventJobData<T> jobData, IntPtr listDataPtr, IntPtr unusedPtr, ref JobRanges ranges, int jobIndex);

            /// Calls the user implemented Execute method with ActuatorEvent struct
            public static unsafe void Execute(ref ActionEventJobData<T> jobData, IntPtr listDataPtr, IntPtr unusedPtr, ref JobRanges ranges, int jobIndex)
            {
                int contSize = jobData.Policy.ContinuousActionSize;
                int discSize = jobData.Policy.DiscreteActionBranches.Length;
                int actionCount = jobData.Policy.ActionCounter.Count;


                for (int i = 0; i < actionCount; i++)
                {
                    jobData.UserJobData.Execute(new ActuatorEvent
                    {
                        ContinuousActionSize = contSize,
                        DiscreteActionSize = discSize,
                        Entity = jobData.Policy.ActionAgentEntityIds[i],
                        ContinuousActionSlice = jobData.Policy.ContinuousActuators.Slice(i * contSize, contSize),
                        DiscreteActionSlice = jobData.Policy.DiscreteActuators.Slice(i * discSize, discSize)
                    });
                }

                jobData.Policy.ResetActionsCounter();
            }
        }
    }
}
