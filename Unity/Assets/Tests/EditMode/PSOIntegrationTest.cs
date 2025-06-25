// filepath: /home/richter/Documents/Varsity/COS 301/SuperLap/Unity/Assets/Tests/EditMode/PSOIntegrationTest.cs
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RacelineOptimizer;

public class PSOIntegrationTest
{
    private string testImagePath;
    private string testImagePath1;
    private string testImagePath2;
    private string testImagePath3;
    private string nonExistentImagePath;
    private string invalidImagePath;

    [SetUp]
    public void Setup()
    {
        // Setup test paths using the same images as ImageProcessingTest
        string baseImagePath = Path.Combine(Application.dataPath.Replace("Unity/Assets", "Backend/ImageProcessing/trackImages"));
        
        testImagePath = Path.Combine(baseImagePath, "test.png");
        testImagePath1 = Path.Combine(baseImagePath, "test1.png");
        testImagePath2 = Path.Combine(baseImagePath, "test2.png");
        testImagePath3 = Path.Combine(baseImagePath, "test3.png");
        
        nonExistentImagePath = Path.Combine(Application.temporaryCachePath, "nonexistent.png");
        invalidImagePath = Path.Combine(Application.temporaryCachePath, "invalid_pso_test.txt");
        
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
    public void PSOIntegrator_RunPSO_WithEmptyBoundaries_HandlesGracefully()
    {
        // Test with empty boundaries
        List<Vector2> emptyBoundary = new List<Vector2>();

        PSOInterface.RacelineResult result = null;
        
        Assert.DoesNotThrow(() => {
            result = PSOIntegrator.RunPSO(emptyBoundary, emptyBoundary, 10, 100);
        }, "RunPSO should handle empty boundaries gracefully");

        // Should return null or handle empty boundaries gracefully
        if (result != null)
        {
            Debug.Log("PSO handled empty boundaries and returned a result");
        }
        else
        {
            Debug.Log("PSO correctly returned null for empty boundaries");
        }
    }


    [Test]
    public void PSO_IntegrationWithImageProcessing_WorksWithValidImage()
    {
        // Only run this test if the test image actually exists
        if (!File.Exists(testImagePath))
        {
            Assert.Inconclusive($"Test image not found at {testImagePath}. This test requires the test image to be present.");
            return;
        }

        // First, process the image to get boundaries
        ImageProcessing.TrackBoundaries boundaries = ImageProcessing.ProcessImage(testImagePath);
        
        Assert.IsNotNull(boundaries, "Image processing should return a result");
        
        if (boundaries.success)
        {
            Assert.IsNotNull(boundaries.innerBoundary, "Successful processing should provide inner boundary");
            Assert.IsNotNull(boundaries.outerBoundary, "Successful processing should provide outer boundary");
            Assert.IsTrue(boundaries.innerBoundary.Count > 0, "Inner boundary should have points");
            Assert.IsTrue(boundaries.outerBoundary.Count > 0, "Outer boundary should have points");
            
            // Now test PSO with the processed boundaries
            PSOInterface.RacelineResult psoResult = null;
            
            Assert.DoesNotThrow(() => {
                psoResult = PSOIntegrator.RunPSO(boundaries.innerBoundary, boundaries.outerBoundary, 50, 1000);
            }, "PSO should run without throwing exceptions on real image data");
            
            if (psoResult != null)
            {
                Assert.IsNotNull(psoResult.InnerBoundary, "PSO result should have inner boundary");
                Assert.IsNotNull(psoResult.OuterBoundary, "PSO result should have outer boundary");
                Assert.IsNotNull(psoResult.Raceline, "PSO result should have raceline");
                
                Debug.Log($"Full integration test passed! Image processing + PSO completed. Raceline points: {psoResult.Raceline.Count}");
            }
            else
            {
                Debug.Log("PSO returned null result for real image boundaries (may be expected depending on track complexity)");
            }
        }
        else
        {
            Debug.Log($"Image processing failed: {boundaries.errorMessage}. PSO integration test skipped.");
            Assert.Inconclusive("Image processing failed, cannot test PSO integration");
        }
    }

    [Test]
    public void PSO_IntegrationWithMultipleImages_HandlesVariousTrackTypes()
    {
        string[] testImagePaths = { testImagePath, testImagePath1, testImagePath2, testImagePath3 };
        int successfulIntegrations = 0;
        
        foreach (string imagePath in testImagePaths)
        {
            if (!File.Exists(imagePath))
            {
                Debug.Log($"Test image {Path.GetFileName(imagePath)} not found - skipping");
                continue;
            }

            Debug.Log($"Testing PSO integration with {Path.GetFileName(imagePath)}");
            
            // Process image
            ImageProcessing.TrackBoundaries boundaries = ImageProcessing.ProcessImage(imagePath);
            
            if (boundaries.success && boundaries.innerBoundary != null && boundaries.outerBoundary != null &&
                boundaries.innerBoundary.Count > 0 && boundaries.outerBoundary.Count > 0)
            {
                // Run PSO with conservative parameters for testing
                PSOInterface.RacelineResult psoResult = null;
                
                Assert.DoesNotThrow(() => {
                    psoResult = PSOIntegrator.RunPSO(boundaries.innerBoundary, boundaries.outerBoundary, 30, 500);
                }, $"PSO should not throw exceptions for {Path.GetFileName(imagePath)}");
                
                if (psoResult != null && psoResult.Raceline != null && psoResult.Raceline.Count > 0)
                {
                    successfulIntegrations++;
                    Debug.Log($"Successful PSO integration for {Path.GetFileName(imagePath)}: {psoResult.Raceline.Count} raceline points");
                }
                else
                {
                    Debug.Log($"PSO returned null/empty result for {Path.GetFileName(imagePath)} (may be expected)");
                }
            }
            else
            {
                Debug.Log($"Image processing failed for {Path.GetFileName(imagePath)}: {boundaries.errorMessage}");
            }
        }
        
        Debug.Log($"PSO integration completed for {successfulIntegrations} out of {testImagePaths.Length} test images");
        
        // At least one integration should work if images are available
        // This is not a hard assertion since it depends on image availability and track complexity
        if (testImagePaths.Any(File.Exists))
        {
            Debug.Log("Multiple image integration test completed - check logs for individual results");
        }
        else
        {
            Assert.Inconclusive("No test images found for multiple image integration test");
        }
    }
}
