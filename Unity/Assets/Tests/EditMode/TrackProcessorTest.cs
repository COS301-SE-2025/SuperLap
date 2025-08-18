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
    }
}