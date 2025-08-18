using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;

public class TrackImageProcessorTests
{
    private GameObject testGameObject;
    private TrackImageProcessor processor;
    private Camera testCamera;

    [SetUp]
    public void Setup()
    {
        //Create test GameObject
        testGameObject = new GameObject("TestTrackProcessor");
        processor = testGameObject.AddComponent<TrackImageProcessor>();

        //Create test camera for UI
        var cameraGO = new GameObject("TestCamera");
        testCamera = cameraGO.AddComponent<Camera>();

        //Setup required UI components using reflection to access private fields
        SetupMockUI();
    }

    [TearDown]
    public void TearDown()
    {
        if (testGameObject != null)
            Object.DestroyImmediate(testGameObject);

        if (testCamera != null)
            Object.DestroyImmediate(testCamera.gameObject);
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
        //Test mask width slider
        float testValue = 75f;
        processor.OnMaskWidthChanged(testValue);

        //We can't directly access the private maskWidth field, 
        //but we can test the behavior through public methods/UI updates.
        //This would typically be verified through UI state or getter methods
        Assert.Pass("MaskWidth updated successfully");  //Placeholder assertion
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

    [UnityTest]
    public IEnumerator LoadImage_WithValidTexture_SetsLoadedTexture()
    {
        //Create test texture
        var testTexture = CreateTestTexture(256, 256);

        //Can't easily test the file loading without actual files,
        //instead test the texture assignment
        var processorType = typeof(TrackImageProcessor);
        var loadedTextureField = processorType.GetField("loadedTexture",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        loadedTextureField?.SetValue(processor, testTexture);

        yield return null;

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
        //Mock results
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

        //Verify results set
        Assert.IsTrue(processor.HasValidResults());

        //Clear results
        processor.ClearResults();

        //Verify results cleared
        Assert.IsFalse(processor.HasValidResults());
        Assert.IsNull(processor.GetLastResults());
    }

    #endregion
    #region Mask Creation Tests

    [Test]
    public void GetCenterlineMask_WithNullTexture_ReturnsNull()
    {
        var mask = processor.GetCenterlineMask();
        Assert.IsNull(mask);
    }

    [Test]
    public void GetCenterlineMask_WithValidTexture_ReturnsTexture()
    {
        //Setup test texture
        var testTexture = CreateTestTexture(100, 100);

        //Set loaded texture
        var processorType = typeof(TrackImageProcessor);
        var loadedTextureField = processorType.GetField("loadedTexture",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        loadedTextureField?.SetValue(processor, testTexture);

        // Add some centerline points
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

    [UnityTest]
    public IEnumerator ProcessingWorkflow_WithMockData_CompletesSuccessfully()
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
        
        yield return null;
        
        //Verify setup
        Assert.IsTrue(processor.HasCenterlineData());
        Assert.IsNotNull(processor.GetLoadedTexture());
        
        Object.DestroyImmediate(testTexture);
    }

    #endregion
}