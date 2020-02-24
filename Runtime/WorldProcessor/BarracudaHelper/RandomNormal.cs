using Unity.Mathematics;

namespace Unity.AI.MLAgents.Inference
{
    /// <summary>
    /// RandomNormal - A random number generator that produces normally distributed random
    /// numbers using the Marsaglia polar method:
    /// https://en.wikipedia.org/wiki/Marsaglia_polar_method
    /// TODO: worth overriding System.Random instead of aggregating?
    /// </summary>
    internal class RandomNormal
    {
        readonly float m_Mean;
        readonly float m_Stddev;
        Random m_Random;

        public RandomNormal(uint seed, float mean = 0.0f, float stddev = 1.0f)
        {
            m_Mean = mean;
            m_Stddev = stddev;
            m_Random = new Random(seed);
        }

        // Each iteration produces two numbers. Hold one here for next call
        bool m_HasSpare;
        float m_SpareUnscaled;

        /// <summary>
        /// Return the next random double number
        /// </summary>
        /// <returns>Next random double number</returns>
        public float NextFloat()
        {
            if (m_HasSpare)
            {
                m_HasSpare = false;
                return m_SpareUnscaled * m_Stddev + m_Mean;
            }

            float u, v, s;
            do
            {
                u = m_Random.NextFloat() * 2.0f - 1.0f;
                v = m_Random.NextFloat() * 2.0f - 1.0f;
                s = u * u + v * v;
            }
            while (s >= 1.0f || math.abs(s) < float.Epsilon);

            s = math.sqrt(-2.0f * math.log(s) / s);
            m_SpareUnscaled = u * s;
            m_HasSpare = true;

            return v * s * m_Stddev + m_Mean;
        }
    }
}
