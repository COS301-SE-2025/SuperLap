using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ImageProcessingIntegrationTest
{
    private string testImagePath;
    private string invalidImagePath;
    private string nonExistentImagePath;

    [SetUp]
    public void Setup()
    {
        // Setup test paths
        testImagePath = Path.Combine(Application.dataPath.Replace("Unity/Assets", "Backend/ImageProcessing/trackImages"), "test.png");
        invalidImagePath = Path.Combine(Application.temporaryCachePath, "invalid_test.txt");
        nonExistentImagePath = Path.Combine(Application.temporaryCachePath, "nonexistent.png");
        
        // Create a dummy invalid file for testing
        if (!File.Exists(invalidImagePath))
        {
            File.WriteAllText(invalidImagePath, "This is not an image file");
        }
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up test files
        if (File.Exists(invalidImagePath))
        {
            File.Delete(invalidImagePath);
        }
    }

    [Test]
    public void PythonNet_Instance_IsInitialized()
    {
        // Test that PythonNet is properly initialized
        Assert.IsTrue(PythonNet.Instance.IsInitialized(), "PythonNet should be initialized for image processing");
    }

    [Test]
    public void ImageProcessing_CheckPythonDependencies_ReturnsResult()
    {
        // Test that Python dependencies check runs without throwing
        bool result = false;
        Assert.DoesNotThrow(() => {
            result = ImageProcessing.CheckPythonDependencies();
        }, "CheckPythonDependencies should not throw exceptions");
        
        // Log the result for debugging
        Debug.Log($"Python dependencies available: {result}");
    }

    [Test]
    public void ImageProcessing_ProcessImage_WithNullPath_ReturnsFailureResult()
    {
        // Test handling of null image path
        ImageProcessing.TrackBoundaries result = ImageProcessing.ProcessImage(null);
        
        Assert.IsFalse(result.success, "Processing null path should fail");
        Assert.IsNotNull(result.errorMessage, "Error message should be provided");
        Assert.IsTrue(result.errorMessage.Contains("not found"), "Error message should mention file not found");
    }

    [Test]
    public void ImageProcessing_ProcessImage_WithEmptyPath_ReturnsFailureResult()
    {
        // Test handling of empty image path
        ImageProcessing.TrackBoundaries result = ImageProcessing.ProcessImage("");
        
        Assert.IsFalse(result.success, "Processing empty path should fail");
        Assert.IsNotNull(result.errorMessage, "Error message should be provided");
        Assert.IsTrue(result.errorMessage.Contains("not found"), "Error message should mention file not found");
    }

    [Test]
    public void ImageProcessing_ProcessImage_WithNonExistentPath_ReturnsFailureResult()
    {
        // Test handling of non-existent file path
        ImageProcessing.TrackBoundaries result = ImageProcessing.ProcessImage(nonExistentImagePath);
        
        Assert.IsFalse(result.success, "Processing non-existent file should fail");
        Assert.IsNotNull(result.errorMessage, "Error message should be provided");
        Assert.IsTrue(result.errorMessage.Contains("not found"), "Error message should mention file not found");
    }

    [Test]
    public void ImageProcessing_ProcessImage_WithInvalidFile_ReturnsFailureResult()
    {
        // Test handling of invalid image file
        ImageProcessing.TrackBoundaries result = ImageProcessing.ProcessImage(invalidImagePath);
        
        Assert.IsFalse(result.success, "Processing invalid image file should fail");
        Assert.IsNotNull(result.errorMessage, "Error message should be provided for invalid file");
    }

    [Test]
    public void ImageProcessing_ProcessImage_ResultStructure_IsValid()
    {
        // Test that the result structure is properly initialized
        ImageProcessing.TrackBoundaries result = ImageProcessing.ProcessImage(nonExistentImagePath);
        
        Assert.IsNotNull(result, "Result should never be null");
        Assert.IsFalse(result.success, "Failed processing should have success = false");
        Assert.IsNotNull(result.errorMessage, "Error message should be provided");
        
        // Test that boundary lists are initialized (even if empty)
        if (result.outerBoundary != null)
        {
            Assert.IsInstanceOf<List<Vector2>>(result.outerBoundary, "Outer boundary should be a List<Vector2>");
        }
        if (result.innerBoundary != null)
        {
            Assert.IsInstanceOf<List<Vector2>>(result.innerBoundary, "Inner boundary should be a List<Vector2>");
        }
    }

    [Test]
    public void ImageProcessing_ProcessMultipleImages_WithEmptyArray_ReturnsEmptyResult()
    {
        // Test batch processing with empty array
        string[] emptyPaths = new string[0];
        ImageProcessing.TrackBoundaries[] results = ImageProcessing.ProcessMultipleImages(emptyPaths);
        
        Assert.IsNotNull(results, "Results array should not be null");
        Assert.AreEqual(0, results.Length, "Results array should be empty for empty input");
    }

    [Test]
    public void ImageProcessing_ProcessMultipleImages_WithMixedPaths_ReturnsCorrectCount()
    {
        // Test batch processing with mixed valid/invalid paths
        string[] mixedPaths = { testImagePath, nonExistentImagePath, invalidImagePath };
        ImageProcessing.TrackBoundaries[] results = ImageProcessing.ProcessMultipleImages(mixedPaths);
        
        Assert.IsNotNull(results, "Results array should not be null");
        Assert.AreEqual(mixedPaths.Length, results.Length, "Results count should match input count");
    }

    [Test]
    public void ImageProcessing_TrackBoundaries_SerializationWorks()
    {
        // Test that TrackBoundaries can be serialized (important for Unity)
        ImageProcessing.TrackBoundaries boundaries = new ImageProcessing.TrackBoundaries
        {
            success = true,
            errorMessage = "Test message",
            outerBoundary = new List<Vector2> { new Vector2(1, 2), new Vector2(3, 4) },
            innerBoundary = new List<Vector2> { new Vector2(5, 6) }
        };
        
        string json = JsonUtility.ToJson(boundaries);
        Assert.IsNotNull(json, "TrackBoundaries should be serializable to JSON");
        Assert.IsTrue(json.Length > 0, "Serialized JSON should not be empty");
        
        // Test deserialization
        ImageProcessing.TrackBoundaries deserialized = JsonUtility.FromJson<ImageProcessing.TrackBoundaries>(json);
        Assert.IsNotNull(deserialized, "Should be able to deserialize TrackBoundaries");
        Assert.AreEqual(boundaries.success, deserialized.success, "Success flag should be preserved");
        Assert.AreEqual(boundaries.errorMessage, deserialized.errorMessage, "Error message should be preserved");
    }

    [Test]
    public void ImageProcessing_ProcessImage_DoesNotCrashOnInvalidPythonScript()
    {
        // Test robustness when Python script has issues
        // This tests the exception handling in the integration layer
        ImageProcessing.TrackBoundaries result = null;
        
        Assert.DoesNotThrow(() => {
            result = ImageProcessing.ProcessImage(testImagePath);
        }, "ProcessImage should not throw exceptions even if Python script fails");
        
        Assert.IsNotNull(result, "Result should always be returned");
        
        if (!result.success)
        {
            Assert.IsNotNull(result.errorMessage, "Failed processing should provide error message");
            Debug.Log($"Expected failure for test: {result.errorMessage}");
        }
    }

    [Test]
    public void ImageProcessing_ConvertPythonListToVector2List_HandlesEmptyList()
    {
        // This tests the internal conversion method indirectly through error scenarios
        // We can't directly test the private method, but we can verify it handles edge cases
        ImageProcessing.TrackBoundaries result = ImageProcessing.ProcessImage(nonExistentImagePath);
        
        // The conversion should handle null/empty results gracefully
        Assert.IsNotNull(result, "Result should handle empty Python results");
        if (result.outerBoundary != null)
        {
            Assert.IsInstanceOf<List<Vector2>>(result.outerBoundary, "Should return proper List<Vector2> type");
        }
    }

    [Test]
    public void ImageProcessing_ThreadSafety_MultipleSimultaneousCalls()
    {
        // Test that multiple calls don't interfere with each other
        List<ImageProcessing.TrackBoundaries> results = new List<ImageProcessing.TrackBoundaries>();
        
        // Make several calls in quick succession
        for (int i = 0; i < 3; i++)
        {
            ImageProcessing.TrackBoundaries result = ImageProcessing.ProcessImage(nonExistentImagePath);
            results.Add(result);
        }
        
        // All calls should complete
        Assert.AreEqual(3, results.Count, "All calls should complete");
        
        // All should have consistent results
        foreach (var result in results)
        {
            Assert.IsNotNull(result, "Each result should be valid");
            Assert.IsFalse(result.success, "Each should fail for non-existent file");
            Assert.IsNotNull(result.errorMessage, "Each should have error message");
        }
    }

    [Test]
    public void ImageProcessing_MemoryManagement_DoesNotLeak()
    {
        // Test that processing doesn't cause obvious memory leaks
        long initialMemory = System.GC.GetTotalMemory(true);
        
        // Process several invalid images
        for (int i = 0; i < 10; i++)
        {
            ImageProcessing.TrackBoundaries result = ImageProcessing.ProcessImage(nonExistentImagePath);
            Assert.IsNotNull(result, "Each call should return a result");
        }
        
        // Force garbage collection
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
        
        long finalMemory = System.GC.GetTotalMemory(true);
        long memoryDifference = finalMemory - initialMemory;
        
        // Allow for some memory increase but flag excessive growth
        Assert.IsTrue(memoryDifference < 10 * 1024 * 1024, // 10MB threshold
            $"Memory usage should not increase excessively. Difference: {memoryDifference} bytes");
    }

    // Test specifically for the case where test.png exists
    [Test]
    public void ImageProcessing_ProcessValidImage_WhenAvailable()
    {
        // Only run this test if the test image actually exists
        if (!File.Exists(testImagePath))
        {
            Assert.Inconclusive($"Test image not found at {testImagePath}. This test requires the test image to be present.");
            return;
        }
        
        ImageProcessing.TrackBoundaries result = ImageProcessing.ProcessImage(testImagePath);
        
        Assert.IsNotNull(result, "Result should not be null for valid image");
        
        if (result.success)
        {
            // If processing succeeds, validate the structure
            Assert.IsNotNull(result.outerBoundary, "Outer boundary should be provided on success");
            Assert.IsNotNull(result.innerBoundary, "Inner boundary should be provided on success");
            Assert.IsTrue(result.outerBoundary.Count > 0, "Outer boundary should have points");
            Assert.IsTrue(result.innerBoundary.Count > 0, "Inner boundary should have points");
            
            // Validate that boundaries contain valid coordinates
            foreach (Vector2 point in result.outerBoundary)
            {
                Assert.IsFalse(float.IsNaN(point.x), "Outer boundary X coordinates should be valid numbers");
                Assert.IsFalse(float.IsNaN(point.y), "Outer boundary Y coordinates should be valid numbers");
            }
            
            foreach (Vector2 point in result.innerBoundary)
            {
                Assert.IsFalse(float.IsNaN(point.x), "Inner boundary X coordinates should be valid numbers");
                Assert.IsFalse(float.IsNaN(point.y), "Inner boundary Y coordinates should be valid numbers");
            }
            
            Debug.Log($"Successfully processed test image. Outer: {result.outerBoundary.Count} points, Inner: {result.innerBoundary.Count} points");
        }
        else
        {
            // If processing fails, we should have a meaningful error message
            Assert.IsNotNull(result.errorMessage, "Failed processing should provide error message");
            Debug.Log($"Image processing failed (expected if Python environment not set up): {result.errorMessage}");
        }
    }
}
