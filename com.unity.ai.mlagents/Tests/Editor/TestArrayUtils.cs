using NUnit.Framework;
using Unity.Collections;

namespace Unity.AI.MLAgents.Tests.Editor
{
    public class TestArrayUtils
    {
        [Test]
        public void TestSum()
        {
            var testArray = new NativeArray<int>(0, Allocator.Persistent);
            Assert.AreEqual(0, testArray.Sum());
            testArray.Dispose();
            testArray = new NativeArray<int>(1, Allocator.Persistent);
            Assert.AreEqual(0, testArray.Sum());
            testArray.Dispose();
            testArray = new NativeArray<int>(1, Allocator.Persistent);
            testArray[0] = 33;
            Assert.AreEqual(33, testArray.Sum());
            testArray.Dispose();

            for (int capacity = 2; capacity < 300; capacity++)
            {
                testArray = new NativeArray<int>(capacity, Allocator.Persistent);
                for (int i = 0; i < capacity; i++)
                {
                    testArray[i] = i;
                }
                Assert.AreEqual(capacity * (capacity - 1) / 2, testArray.Sum());
                testArray.Dispose();
            }
        }

        [Test]
        public void TestCumSumAt()
        {
            var testArray = new NativeArray<int>(0, Allocator.Persistent);
            Assert.AreEqual(0, testArray.CumSumAt(0));
            testArray.Dispose();
            testArray = new NativeArray<int>(1, Allocator.Persistent);
            Assert.AreEqual(0, testArray.CumSumAt(0));
            testArray.Dispose();
            testArray = new NativeArray<int>(1, Allocator.Persistent);
            testArray[0] = 33;
            Assert.AreEqual(0, testArray.CumSumAt(0));
            Assert.AreEqual(33, testArray.CumSumAt(1));
            testArray.Dispose();

            int capacity = 300;
            testArray = new NativeArray<int>(capacity, Allocator.Persistent);
            for (int i = 0; i < capacity; i++)
            {
                testArray[i] = i;
            }
            for (int i = 2; i < capacity; i++)
            {
                Assert.AreEqual(i * (i - 1) / 2, testArray.CumSumAt(i));
            }

            Assert.Throws<System.IndexOutOfRangeException>(() => testArray.CumSumAt(capacity * 2));
            testArray.Dispose();

        }

        [Test]
        public void TestIncreaseArraySizeHeuristic()
        {
            for (int reqCapacity = 0; reqCapacity < 20; reqCapacity++)
            {
                int actualCapacity = ArrayUtils.IncreaseArraySizeHeuristic(reqCapacity);
                Assert.Greater(actualCapacity, reqCapacity);
            }
        }

    }
}