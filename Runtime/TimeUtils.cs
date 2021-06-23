using Unity.Entities;
using Unity.Core;

namespace Unity.AI.MLAgents
{
    public static class TimeUtils
    {
        /// <summary>
        /// Configure the given ComponentSystemGroup to update at a fixed timestep, given by timeStep.
        /// The group will be updated multiple times.
        /// </summary>
        /// <param name="group">The group whose UpdateCallback will be configured with a fixed time step update call</param>
        /// <param name="timeStep">The fixed time step (in seconds)</param>
        /// <param name="numberOfRepeat">How many times the system will be updated per updates</param>
        public static void EnableFixedRateWithRepeat(ComponentSystemGroup group, float timeStep, int numberOfRepeat)
        {
            var manager = new FixedRateRepeatManager(timeStep, numberOfRepeat);
            group.FixedRateManager = manager;
        }

        /// <summary>
        /// Disable fixed rate updates on the given group, by setting the UpdateCallback to null.
        /// </summary>
        /// <param name="group">The group whose UpdateCallback to set to null.</param>
        public static void DisableFixedRate(ComponentSystemGroup group)
        {
            group.FixedRateManager = null;
        }
    }

    internal class FixedRateRepeatManager : IFixedRateManager
    {
        protected int m_NumberOfRepeat;
        protected int m_CurrentRepeat;
        protected float m_FixedTimeStep;
        protected double m_LastFixedUpdateTime;
        protected int m_FixedUpdateCount;
        protected bool m_DidPushTime;

        internal FixedRateRepeatManager(float fixedStep, int numberOfRepeat)
        {
            m_FixedTimeStep = fixedStep;
            m_NumberOfRepeat = numberOfRepeat;
            m_CurrentRepeat = 0;
        }

        public float Timestep
        {
            get { return m_FixedTimeStep; }
            set { m_FixedTimeStep = value; }
        }

        public bool ShouldGroupUpdate(ComponentSystemGroup group)
        {
            // if this is true, means we're being called a second or later time in a loop
            if (m_DidPushTime)
            {
                group.World.PopTime();
            }

            var elapsedTime = group.World.Time.ElapsedTime;
            if (m_LastFixedUpdateTime == 0.0)
                m_LastFixedUpdateTime = elapsedTime - m_FixedTimeStep;

            if (m_CurrentRepeat < m_NumberOfRepeat)
            {
                // Note that m_FixedTimeStep of 0.0f will never update
                m_LastFixedUpdateTime += m_FixedTimeStep;
                m_FixedUpdateCount++;
                m_CurrentRepeat++;
            }
            else
            {
                m_CurrentRepeat = 0;
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
