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
        /// The size of the action vector.
        /// </summary>
        [ReadOnly] public int ActionSize;

        /// <summary>
        /// The Entity the vector action is for.
        /// </summary>
        [ReadOnly] public Entity Entity;

        /// <summary>
        /// The type of action : can be DISCRETE or CONTINUOUS.
        /// </summary>
        [ReadOnly] public ActionType ActionType;

        [ReadOnly] internal NativeSlice<float> ContinuousActionSlice;
        [ReadOnly] internal NativeSlice<int> DiscreteActionSlice;

        /// <summary>
        /// Retrieve the action that was decided for the Entity.
        /// This method uses a generic type, as such you must provide a
        /// type that is compatible with the Action Type and Action Size
        /// for this Policy.
        /// </summary>
        /// <typeparam name="T"> The type of action struct.</typeparam>
        /// <returns> The action struct for the Entity.</returns>
        public T GetAction<T>() where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (ActionSize != UnsafeUtility.SizeOf<T>() / 4)
            {
                var receivedSize = UnsafeUtility.SizeOf<T>() / 4;
                throw new MLAgentsException($"Action space size does not match for action. Expected {ActionSize} but received {receivedSize}");
            }
#endif
            if (ActionType == ActionType.DISCRETE)
            {
                return DiscreteActionSlice.SliceConvert<T>()[0];
            }
            else
            {
                return ContinuousActionSlice.SliceConvert<T>()[0];
            }
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
            inputDeps.Complete(); // TODO : FIND A BETTER WAY TO MAKE SURE ALL THE DATA IS IN THE POLICY
            Academy.Instance.UpdatePolicy(policy);
            if (policy.ActionCounter.Count == 0)
            {
                return inputDeps;
            }
            return ScheduleImpl(jobData, policy, inputDeps);
        }

        // Passing this along
        internal static unsafe JobHandle ScheduleImpl<T>(this T jobData, Policy policy, JobHandle inputDeps)
            where T : struct, IActuatorJob
        {
            // Creating a data struct that contains the data the user passed into the job (This is what T is here)
            var data = new ActionEventJobData<T>
            {
                UserJobData = jobData,
                Policy = policy // Need to create this before hand with the actuator data
            };

            // Scheduling a Job out of thin air by using a pointer called jobReflectionData in the ActuatorSystemJobStruct
            var parameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref data), ActuatorDataJobProcess<T>.Initialize(), inputDeps, ScheduleMode.Batched);
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
                    jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(ActionEventJobData<T>), typeof(T), JobType.Single, (ExecuteJobFunction)Execute);
                return jobReflectionData;
            }

            #endregion


            public delegate void ExecuteJobFunction(ref ActionEventJobData<T> jobData, IntPtr listDataPtr, IntPtr unusedPtr, ref JobRanges ranges, int jobIndex);

            /// Calls the user implemented Execute method with ActuatorEvent struct
            public static unsafe void Execute(ref ActionEventJobData<T> jobData, IntPtr listDataPtr, IntPtr unusedPtr, ref JobRanges ranges, int jobIndex)
            {
                int size = jobData.Policy.ActionSize;
                int actionCount = jobData.Policy.ActionCounter.Count;

                // Continuous case
                if (jobData.Policy.ActionType == ActionType.CONTINUOUS)
                {
                    for (int i = 0; i < actionCount; i++)
                    {
                        jobData.UserJobData.Execute(new ActuatorEvent
                        {
                            ActionSize = size,
                            ActionType = ActionType.CONTINUOUS,
                            Entity = jobData.Policy.ActionAgentEntityIds[i],
                            ContinuousActionSlice = jobData.Policy.ContinuousActuators.Slice(i * size, size)
                        });
                    }
                }
                // Discrete Case
                else
                {
                    for (int i = 0; i < actionCount; i++)
                    {
                        jobData.UserJobData.Execute(new ActuatorEvent
                        {
                            ActionSize = size,
                            ActionType = ActionType.DISCRETE,
                            Entity = jobData.Policy.ActionAgentEntityIds[i],
                            DiscreteActionSlice = jobData.Policy.DiscreteActuators.Slice(i * size, size)
                        });
                    }
                }
                jobData.Policy.ResetActionsCounter();
            }
        }
    }
}
