using Unity.Entities;
using Unity.Core;

namespace Unity.AI.MLAgents
{
    public static class TimeUtils
    {
        /// <summary>
        /// Configure the given ComponentSystemGroup to update at a fixed timestep, given by timeStep.
        /// If the interval between the current time and the last update is bigger than the timestep
        /// multiplied by the time scale,
        /// the group's systems will be updated more than once.
        /// </summary>
        /// <param name="group">The group whose UpdateCallback will be configured with a fixed time step update call</param>
        /// <param name="timeStep">The fixed time step (in seconds)</param>
        /// <param name="timeScale">How much time passes in the group compared to other systems</param>
        public static void EnableFixedRateWithCatchUpAndMultiplier(ComponentSystemGroup group, float timeStep, float timeScale)
        {
            var manager = new FixedRateTimeScaleCatchUpAndMultiplierManager(timeStep, timeScale);
            group.UpdateCallback = manager.UpdateCallback;
        }

        /// <summary>
        /// Disable fixed rate updates on the given group, by setting the UpdateCallback to null.
        /// </summary>
        /// <param name="group">The group whose UpdateCallback to set to null.</param>
        public static void DisableFixedRate(ComponentSystemGroup group)
        {
            group.UpdateCallback = null;
        }
    }


    internal class FixedRateTimeScaleCatchUpAndMultiplierManager
    {
        protected float m_TimeScale;
        protected float m_FixedTimeStep;
        protected double m_LastFixedUpdateTime;
        protected int m_FixedUpdateCount;
        protected bool m_DidPushTime;

        internal FixedRateTimeScaleCatchUpAndMultiplierManager(float fixedStep, float timeScale)
        {
            m_FixedTimeStep = fixedStep;
            m_TimeScale = timeScale;
        }

        internal bool UpdateCallback(ComponentSystemGroup group)
        {
            // if this is true, means we're being called a second or later time in a loop
            if (m_DidPushTime)
            {
                group.World.PopTime();
            }

            var elapsedTime = group.World.Time.ElapsedTime * m_TimeScale;
            if (m_LastFixedUpdateTime == 0.0)
                m_LastFixedUpdateTime = elapsedTime - m_FixedTimeStep;

            if (elapsedTime - m_LastFixedUpdateTime >= m_FixedTimeStep)
            {
                // Note that m_FixedTimeStep of 0.0f will never update
                m_LastFixedUpdateTime += m_FixedTimeStep;
                m_FixedUpdateCount++;
            }
            else
            {
                m_DidPushTime = false;
                return false;
            }

            group.World.PushTime(new TimeData(
                elapsedTime: m_LastFixedUpdateTime,
                deltaTime: m_FixedTimeStep));

            m_DidPushTime = true;
            return true;
        }
    }
}
