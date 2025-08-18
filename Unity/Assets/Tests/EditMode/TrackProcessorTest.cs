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

        [Test]
        public void CalculateRaceDirection_HandlesEmptyPointsList()
        {
            var method = typeof(TrackImageProcessor).GetMethod("CalculateRaceDirection",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            //Set empty centerline points
            var centerlineField = typeof(TrackImageProcessor).GetField("centerlinePoints",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            centerlineField.SetValue(processor, new List<Vector2>());

            //Should not throw exception with empty points
            Assert.DoesNotThrow(() => method.Invoke(processor, null));
        }

        [Test]
        public void CalculateRaceDirection_CalculatesCorrectAngle_ForKnownPoints()
        {
            //Set up points
            var centerlineField = typeof(TrackImageProcessor).GetField("centerlinePoints",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var raceDirectionField = typeof(TrackImageProcessor).GetField("raceDirection",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var testPoints = new List<Vector2>
            {
                new Vector2(0, 0),  // Start point
                new Vector2(10, 0)  // End point (moving east)
            };
            centerlineField.SetValue(processor, testPoints);

            var method = typeof(TrackImageProcessor).GetMethod("CalculateRaceDirection",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            method.Invoke(processor, null);

            float direction = (float)raceDirectionField.GetValue(processor);
            Assert.AreEqual(0f, direction, 0.1f, "Should calculate 0 degrees for eastward movement");
        }
        
        [Test]
        public void HasCenterlineData_ReturnsFalse_WhenNoCenterline()
        {
            Assert.IsFalse(processor.HasCenterlineData());
        }

        [Test]
        public void HasValidResults_ReturnsFalse_Initially()
        {
            Assert.IsFalse(processor.HasValidResults());
        }

        [Test]
        public void GetCenterlinePoints_ReturnsEmptyList_Initially()
        {
            var points = processor.GetCenterlinePoints();
            Assert.IsNotNull(points);
            Assert.AreEqual(0, points.Count);
        }

        [Test]
        public void GetStartPosition_ReturnsNull_Initially()
        {
            Assert.IsNull(processor.GetStartPosition());
        }

        [Test]
        public void GetRaceDirection_ReturnsZero_Initially()
        {
            Assert.AreEqual(0f, processor.GetRaceDirection());
        }
    }
}