using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Collections;

namespace Unity.AI.MLAgents
{
    // This struct will be provided to the user in a custom job inheriting from IActuatorJob
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
        /// for this MLAgentsWorld.
        /// </summary>
        public T GetAction<T>() where T : struct
        {
            if (ActionType == ActionType.DISCRETE)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (ActionSize != UnsafeUtility.SizeOf<T>() / sizeof(float))
                {
                    throw new MLAgentsException("Action space does not match for Discrete action. Expected " + ActionSize);
                }
#endif
                return DiscreteActionSlice.SliceConvert<T>()[0];
            }
            else
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (ActionSize != UnsafeUtility.SizeOf<T>() / sizeof(float))
                {
                    throw new MLAgentsException("Action space does not match for Continuous action. Expected " + ActionSize);
                }
#endif
                return ContinuousActionSlice.SliceConvert<T>()[0];
            }
        }
    }

    /// <summary>
    /// The signature of the Job used to retrieve actuators values from a world
    /// </summary>
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
            inputDeps.Complete(); // TODO : FIND A BETTER WAY TO MAKE SURE ALL THE DATA IS IN THE WORLD
            Academy.Instance.UpdateWorld(mlagentsWorld);
            if (mlagentsWorld.ActionCounter.Count == 0)
            {
                return inputDeps;
            }
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
                world = mlagentsWorld // Need to create this before hand with the actuator data
            };

            // Scheduling a Job out of thin air by using a pointer called jobReflectionData in the ActuatorSystemJobStruct
            var parameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref data), ActuatorDataJobProcess<T>.Initialize(), inputDeps, ScheduleMode.Batched);
            return JobsUtility.Schedule(ref parameters);
        }

        // This is the struct containing all the data needed from both the user and the MLAgents world
        internal unsafe struct ActionEventJobData<T> where T : struct
        {
            public T UserJobData;
            [NativeDisableContainerSafetyRestriction] public MLAgentsWorld world;
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
                int size = jobData.world.ActionSize;
                int actionCount = jobData.world.ActionCounter.Count;
                // Continuous case
                if (jobData.world.ActionType == ActionType.CONTINUOUS)
                {
                    for (int i = 0; i < actionCount; i++)
                    {
                        if (!jobData.world.ActionDoneFlags[i])
                        {
                            jobData.UserJobData.Execute(new ActuatorEvent
                            {
                                ActionSize = size,
                                ActionType = ActionType.CONTINUOUS,
                                Entity = jobData.world.ActionAgentIds[i],
                                ContinuousActionSlice = jobData.world.ContinuousActuators.Slice(i * size, size)
                            });
                        }
                    }
                }
                // Discrete Case
                else
                {
                    for (int i = 0; i < actionCount; i++)
                    {
                        if (!jobData.world.ActionDoneFlags[i])
                        {
                            jobData.UserJobData.Execute(new ActuatorEvent
                            {
                                ActionSize = size,
                                ActionType = ActionType.DISCRETE,
                                Entity = jobData.world.ActionAgentIds[i],
                                DiscreteActionSlice = jobData.world.DiscreteActuators.Slice(i * size, size)
                            });
                        }
                    }
                }
                jobData.world.ResetActionsCounter();
            }
        }
    }
}
