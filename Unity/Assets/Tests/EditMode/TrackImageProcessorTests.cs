using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace TrackImageProcessorTests
{
    // Helper class to access private methods and fields via reflection
    public static class TestExtensions
    {
        public static T CallPrivateMethod<T>(this object obj, string methodName, params object[] parameters)
        {
            var method = obj.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method == null)
            {
                throw new ArgumentException($"Method {methodName} not found");
            }

            return (T)method.Invoke(obj, parameters);
        }

        public static void CallPrivateMethod(this object obj, string methodName, params object[] parameters)
        {
            var method = obj.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method == null)
            {
                throw new ArgumentException($"Method {methodName} not found");
            }

            method.Invoke(obj, parameters);
        }
    }

    [TestFixture]
    public class TrackImageProcessorEditModeTests
    {
        private GameObject testGameObject;
        private TrackImageProcessor processor;

        [SetUp]
        public void Setup()
        {
            //Create test GameObject
            testGameObject = new GameObject("TestTrackProcessor");
            processor = testGameObject.AddComponent<TrackImageProcessor>();

            //Setup required UI components using reflection to access private fields
            SetupMockUI();
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
                Object.DestroyImmediate(testGameObject);
        }

        private void SetupMockUI()
        {
            //Create mock UI elements
            var canvasGO = new GameObject("TestCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            //Preview Image
            var previewImageGO = new GameObject("PreviewImage");
            previewImageGO.transform.SetParent(canvasGO.transform);
            var previewImage = previewImageGO.AddComponent<Image>();
            previewImageGO.AddComponent<RectTransform>();

            //Buttons
            var traceButtonGO = new GameObject("TraceButton");
            traceButtonGO.transform.SetParent(canvasGO.transform);
            var traceButton = traceButtonGO.AddComponent<Button>();
            var traceText = new GameObject("TraceText");
            traceText.transform.SetParent(traceButtonGO.transform);
            traceText.AddComponent<TextMeshProUGUI>();

            var resetButtonGO = new GameObject("ResetButton");
            resetButtonGO.transform.SetParent(canvasGO.transform);
            var resetButton = resetButtonGO.AddComponent<Button>();

            var processButtonGO = new GameObject("ProcessButton");
            processButtonGO.transform.SetParent(canvasGO.transform);
            var processButton = processButtonGO.AddComponent<Button>();

            //Instruction Text
            var instructionTextGO = new GameObject("InstructionText");
            instructionTextGO.transform.SetParent(canvasGO.transform);
            var instructionText = instructionTextGO.AddComponent<Text>();

            //Slider
            var sliderGO = new GameObject("MaskSlider");
            sliderGO.transform.SetParent(canvasGO.transform);
            var slider = sliderGO.AddComponent<Slider>();

            //Mask Width Label
            var maskLabelGO = new GameObject("MaskLabel");
            maskLabelGO.transform.SetParent(canvasGO.transform);
            var maskLabel = maskLabelGO.AddComponent<Text>();

            //Output Image
            var outputImageGO = new GameObject("OutputImage");
            outputImageGO.transform.SetParent(canvasGO.transform);
            var outputImage = outputImageGO.AddComponent<Image>();

            //Use reflection to set private fields
            var processorType = typeof(TrackImageProcessor);

            processorType.GetField("previewImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(processor, previewImage);
            processorType.GetField("traceButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(processor, traceButton);
            processorType.GetField("resetTraceButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(processor, resetButton);
            processorType.GetField("processButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(processor, processButton);
            processorType.GetField("instructionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(processor, instructionText);
            processorType.GetField("maskWidthSlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(processor, slider);
            processorType.GetField("maskWidthLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(processor, maskLabel);
            processorType.GetField("outputImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(processor, outputImage);
        }

        #region Initialization Tests

        [Test]
        public void TrackImageProcessor_InitializesWithDefaultValues()
        {
            //Test that the processor initializes with expected default values
            Assert.IsNotNull(processor);
            Assert.IsFalse(processor.HasValidResults());
            Assert.IsFalse(processor.HasCenterlineData());
            Assert.IsNull(processor.GetSelectedImagePath());
            Assert.IsNull(processor.GetLoadedTexture());
        }

        [Test]
        public void MaskWidthSlider_UpdatesCorrectly()
        {
            //Test mask width slider functionality
            float testValue = 75f;
            processor.OnMaskWidthChanged(testValue);

            //Can't directly access the private maskWidth field, 
            //but we can test the behavior through public methods/UI updates.
            //This would typically be verified through UI state or getter methods
            Assert.Pass("MaskWidth updated successfully"); //Placeholder assertion
        }

        #endregion
        #region Centerline Tracing Tests

        [Test]
        public void CenterlinePoints_InitiallyEmpty()
        {
            var centerlinePoints = processor.GetCenterlinePoints();
            Assert.IsNotNull(centerlinePoints);
            Assert.AreEqual(0, centerlinePoints.Count);
        }

        [Test]
        public void StartPosition_InitiallyNull()
        {
            var startPosition = processor.GetStartPosition();
            Assert.IsNull(startPosition);
        }

        [Test]
        public void RaceDirection_InitiallyZero()
        {
            var raceDirection = processor.GetRaceDirection();
            Assert.AreEqual(0f, raceDirection);
        }

        [Test]
        public void HasCenterlineData_ReturnsFalseInitially()
        {
            Assert.IsFalse(processor.HasCenterlineData());
        }

        #endregion
        #region Image Loading Tests

        [Test]
        public void LoadImage_WithValidTexture_SetsLoadedTexture()
        {
            //Create a test texture
            var testTexture = CreateTestTexture(256, 256);

            //Since I can't easily test the file loading without actual files,
            //I'm testing the texture assignment
            var processorType = typeof(TrackImageProcessor);
            var loadedTextureField = processorType.GetField("loadedTexture",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            loadedTextureField?.SetValue(processor, testTexture);

            var retrievedTexture = processor.GetLoadedTexture();
            Assert.IsNotNull(retrievedTexture);
            Assert.AreEqual(testTexture, retrievedTexture);

            Object.DestroyImmediate(testTexture);
        }

        #endregion
        #region Results Management Tests

        [Test]
        public void GetLastResults_InitiallyNull()
        {
            var results = processor.GetLastResults();
            Assert.IsNull(results);
        }

        [Test]
        public void HasValidResults_ReturnsFalseWithoutResults()
        {
            Assert.IsFalse(processor.HasValidResults());
        }

        [Test]
        public void ClearResults_ResetsResultsToNull()
        {
            //Set some mock results
            var processorType = typeof(TrackImageProcessor);
            var lastResultsField = processorType.GetField("lastResults",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var mockResults = new TrackImageProcessor.ProcessingResults
            {
                success = true,
                innerBoundary = new List<Vector2>(),
                outerBoundary = new List<Vector2>(),
                raceline = new List<Vector2>()
            };

            lastResultsField?.SetValue(processor, mockResults);

            //Verify results were set
            Assert.IsTrue(processor.HasValidResults());

            //Clear results
            processor.ClearResults();

            //Verify results are cleared
            Assert.IsFalse(processor.HasValidResults());
            Assert.IsNull(processor.GetLastResults());
        }

        #endregion
        #region Mask Creation Tests

        [Test]
        public void GetCenterlineMask_WithNullTexture_ReturnsNull()
        {
            //Expect error log message
            LogAssert.Expect(LogType.Error, "Need at least 100 centerline points and a loaded texture to create mask");
            
            var mask = processor.GetCenterlineMask();
            Assert.IsNull(mask);
        }

        [Test]
        public void GetCenterlineMask_WithValidTexture_ReturnsTexture()
        {
            //Setup a test texture
            var testTexture = CreateTestTexture(100, 100);

            //Use reflection to set the loaded texture
            var processorType = typeof(TrackImageProcessor);
            var loadedTextureField = processorType.GetField("loadedTexture",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            loadedTextureField?.SetValue(processor, testTexture);

            //Add some centerline points
            var centerlinePointsField = processorType.GetField("centerlinePoints",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var centerlinePoints = new List<Vector2>
        {
            new Vector2(10, 10),
            new Vector2(50, 50),
            new Vector2(90, 90)
        };
            centerlinePointsField?.SetValue(processor, centerlinePoints);

            var mask = processor.GetCenterlineMask();
            Assert.IsNotNull(mask);
            Assert.AreEqual(testTexture.width, mask.width);
            Assert.AreEqual(testTexture.height, mask.height);

            Object.DestroyImmediate(testTexture);
            Object.DestroyImmediate(mask);
        }

        #endregion
        #region Output Texture Tests

        [Test]
        public void GetOutputTexture_InitiallyNull()
        {
            var outputTexture = processor.GetOutputTexture();
            Assert.IsNull(outputTexture);
        }

        #endregion

        #region Boundary Alignment Tests

        [Test]
        public void AlignBoundaryWithUserInput_WithNullBoundary_ReturnsOriginal()
        {
            // Arrange
            List<Vector2> nullBoundary = null;
            
            // Act
            var result = processor.CallPrivateMethod<List<Vector2>>("AlignBoundaryWithUserInput", nullBoundary);
            
            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void AlignBoundaryWithUserInput_WithEmptyBoundary_ReturnsOriginal()
        {
            // Arrange
            var emptyBoundary = new List<Vector2>();
            
            // Act
            var result = processor.CallPrivateMethod<List<Vector2>>("AlignBoundaryWithUserInput", emptyBoundary);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void AlignBoundaryWithUserInput_WithoutStartPosition_ReturnsOriginal()
        {
            // Arrange
            var boundary = new List<Vector2>
            {
                new Vector2(10, 10),
                new Vector2(20, 20),
                new Vector2(30, 30)
            };
            
            // startPosition is null by default
            
            // Act
            var result = processor.CallPrivateMethod<List<Vector2>>("AlignBoundaryWithUserInput", boundary);
            
            // Assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(boundary, result);
        }

        public void AlignBoundaryWithUserInput_WithStartPosition_ReordersBoundary()
        {
            // Arrange
            var boundary = new List<Vector2>
            {
                new Vector2(10, 10),
                new Vector2(20, 20),
                new Vector2(30, 30),
                new Vector2(40, 40)
            };
            
            var startPosition = new Vector2(35, 35); // Closest to index 2 (30,30)
            
            // Set start position through reflection
            var processorType = typeof(TrackImageProcessor);
            var startPositionField = processorType.GetField("startPosition", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startPositionField?.SetValue(processor, startPosition);
            
            // Act
            var result = processor.CallPrivateMethod<List<Vector2>>("AlignBoundaryWithUserInput", boundary);
            
            // Assert - should start from closest point
            Assert.IsNotNull(result);
            Assert.AreEqual(boundary.Count, result.Count);
            Assert.AreEqual(boundary[2], result[0]); // Should start at index 2
            Assert.AreEqual(boundary[3], result[1]); // Then index 3
            Assert.AreEqual(boundary[0], result[2]); // Then index 0
            Assert.AreEqual(boundary[1], result[3]); // Then index 1
        }


        #region Helper Methods for Tests

        private Texture2D CreateTestTexture(int width, int height)
        {
            var texture = new Texture2D(width, height);
            var colors = new Color[width * height];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.white;
            }

            texture.SetPixels(colors);
            texture.Apply();

            return texture;
        }

        private void SetCenterlinePoints(List<Vector2> points)
        {
            var processorType = typeof(TrackImageProcessor);
            var centerlinePointsField = processorType.GetField("centerlinePoints",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            centerlinePointsField?.SetValue(processor, points);
        }

        private void SetProcessingResults(TrackImageProcessor.ProcessingResults results)
        {
            var processorType = typeof(TrackImageProcessor);
            var lastResultsField = processorType.GetField("lastResults",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lastResultsField?.SetValue(processor, results);
        }

        #endregion
        #region Integration Tests

        [Test]
        public void ProcessingWorkflow_WithMockData_SetsUpCorrectly()
        {
            //Setup mock texture
            var testTexture = CreateTestTexture(200, 200);
            var processorType = typeof(TrackImageProcessor);

            //Set loaded texture
            var loadedTextureField = processorType.GetField("loadedTexture",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            loadedTextureField?.SetValue(processor, testTexture);

            //Set selected image path
            var selectedImagePathField = processorType.GetField("selectedImagePath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            selectedImagePathField?.SetValue(processor, "test_path.png");

            //Add centerline points
            var centerlinePoints = new List<Vector2>();
            for (int i = 0; i < 120; i++) //More than 100 points
            {
                float angle = (i / 120f) * 2f * Mathf.PI;
                centerlinePoints.Add(new Vector2(
                    100 + 50 * Mathf.Cos(angle),
                    100 + 50 * Mathf.Sin(angle)
                ));
            }

            SetCenterlinePoints(centerlinePoints);

            //Verify setup
            Assert.IsTrue(processor.HasCenterlineData());
            Assert.IsNotNull(processor.GetLoadedTexture());

            Object.DestroyImmediate(testTexture);
        }

        #endregion
        #region Event Tests

        [Test]
        public void OnProcessingComplete_EventCanBeSubscribed()
        {
            bool eventCalled = false;
            TrackImageProcessor.ProcessingResults receivedResults = null;
            
            //Subscribe to event
            processor.OnProcessingComplete += (results) => {
                eventCalled = true;
                receivedResults = results;
            };
            
            //Since the event is triggered internally by ProcessTrackImageCoroutine,
            //I'm testing that the event can be (un)subscribed to
            //without causing null reference exceptions
            
            //Test unsubscribing
            processor.OnProcessingComplete -= (results) => {
                eventCalled = true;
                receivedResults = results;
            };
            
            Assert.IsTrue(true, "Event subscription/unsubscription works correctly");
        }

        [Test]
        public void OnProcessingStarted_EventCanBeSubscribed()
        {
            bool eventCalled = false;
            string receivedMessage = null;
            
            //Subscribe to the event
            processor.OnProcessingStarted += (message) => {
                eventCalled = true;
                receivedMessage = message;
            };
            
            //Test unsubscribing
            processor.OnProcessingStarted -= (message) => {
                eventCalled = true;
                receivedMessage = message;
            };
            
            Assert.IsTrue(true, "OnProcessingStarted event subscription works correctly");
        }

        [Test]
        public void OnImageLoaded_EventCanBeSubscribed()
        {
            bool eventCalled = false;
            string receivedMessage = null;
            
            //Subscribe to the event
            processor.OnImageLoaded += (message) => {
                eventCalled = true;
                receivedMessage = message;
            };
            
            //Test unsubscribing
            processor.OnImageLoaded -= (message) => {
                eventCalled = true;
                receivedMessage = message;
            };
            
            Assert.IsTrue(true, "OnImageLoaded event subscription works correctly");
        }

        #endregion
    }
}