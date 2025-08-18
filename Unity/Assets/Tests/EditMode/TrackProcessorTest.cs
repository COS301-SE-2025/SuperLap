using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Linq;

namespace TrackProcessorTest
{
    public class TrackProcessorEditModeTests
    {
        private TrackImageProcessor processor;
        private GameObject testGameObject;

        [SetUp]
        public void Setup()
        {
            testGameObject = new GameObject("TestTrackProcessor");
            processor = testGameObject.AddComponent<TrackImageProcessor>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }

        [Test]
        public void GetCompassDirection_ReturnsCorrectDirection_ForValidAngles()
        {
            var method = typeof(TrackImageProcessor).GetMethod("GetCompassDirection",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(method, "GetCompassDirection method not found");

            Assert.AreEqual("East", method.Invoke(processor, new object[] { 0f }));
            Assert.AreEqual("Southeast", method.Invoke(processor, new object[] { 45f }));
            Assert.AreEqual("South", method.Invoke(processor, new object[] { 90f }));
            Assert.AreEqual("Southwest", method.Invoke(processor, new object[] { 135f }));
            Assert.AreEqual("West", method.Invoke(processor, new object[] { 180f }));
            Assert.AreEqual("Northwest", method.Invoke(processor, new object[] { 225f }));
            Assert.AreEqual("North", method.Invoke(processor, new object[] { 270f }));
            Assert.AreEqual("Northeast", method.Invoke(processor, new object[] { 315f }));
        }
        
        
    }
}