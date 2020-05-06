using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace Unity.AI.MLAgents.Tests.Editor
{
    public class TestCounter
    {
        [Test]
        public void TestCounterSingleThread()
        {
            var c = new Counter(Allocator.Persistent);
            Assert.AreEqual(0, c.Count);
            Assert.AreEqual(1, c.Increment());
            c.Count = 0;
            Assert.AreEqual(0, c.Count);
            Assert.AreEqual(1, c.Increment());
            Assert.AreEqual(1, c.Count);
            c.Dispose();
        }

        struct CountingJob : IJobParallelFor
        {
            public Counter Counter;
            public void Execute(int i)
            {
                Counter.Increment();
            }
        }


        [Test]
        public void TestCounterJob()
        {
            var c = new Counter(Allocator.Persistent);

            var job = new CountingJob { Counter = c };
            foreach (int expectedValue in new int[] {10, 2000})
            {
                c.Count = 0;
                job.Schedule(expectedValue, 64).Complete();
                Assert.AreEqual(c.Count, expectedValue);
            }
            c.Dispose();
        }
    }
}
