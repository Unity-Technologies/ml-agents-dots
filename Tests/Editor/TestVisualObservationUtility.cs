using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace Unity.AI.MLAgents.Tests.Editor
{
    public class TestVisualObservationUtility
    {
        [Test]
        public void TestGetVisObs()
        {
            int width = 80;
            int height = 30;

            var agentGo1 = new GameObject("TestAgent");
            var cam = agentGo1.AddComponent<Camera>();

            var array = VisualObservationUtility.GetVisObs(cam, width, height, Allocator.Persistent);

            Assert.AreEqual(array.Length, width * height * 3);
            array.Dispose();
        }
    }
}
