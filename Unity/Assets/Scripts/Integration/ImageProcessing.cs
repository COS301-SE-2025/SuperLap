using UnityEngine;
using System;
using System.IO;
using Python.Runtime;

public class ImageProcessing
{
    [Serializable]
    public class TrackBoundaries
    {
        public Vector2[] outerBoundary;
        public Vector2[] innerBoundary;
        public bool success;
        public string errorMessage;
    }

    public static TrackBoundaries ProcessImage(string imagePath)
    {
        Debug.Log($"Processing image: {imagePath}");
        var result = new TrackBoundaries { success = false };
        
        try
        {
            // Validate input
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                result.errorMessage = $"Image file not found: {imagePath}";
                return result;
            }

            // Check if PythonNet is initialized
            if (!PythonNet.Instance.IsInitialized())
            {
                result.errorMessage = "Python.NET is not initialized";
                return result;
            }

            // Get the path to the Python script
            string scriptPath = Application.dataPath.Replace("Unity/Assets", "Backend/ImageProcessing/TrackProcessor.py");
            if (!File.Exists(scriptPath))
            {
                result.errorMessage = $"TrackProcessor.py not found at: {scriptPath}";
                return result;
            }

            // Create simple Python code to call the dedicated integration function
            string pythonCode = $@"
import sys
import os
sys.path.append(r'{Path.GetDirectoryName(scriptPath).Replace("\\", "\\\\")}')

from TrackProcessor import process_track_for_csharp

# Process the image using the dedicated function
result_data = process_track_for_csharp(r'{imagePath.Replace("\\", "\\\\")}')
";

            // Execute the Python code and get the result
            PyObject pythonResult = PythonNet.Instance.RunPythonScriptWithReturn(pythonCode, "result_data");
            
            if (pythonResult == null)
            {
                result.errorMessage = "Failed to execute Python script";
                return result;
            }

            // Parse the Python result
            using (Py.GIL())
            {
                bool success = pythonResult["success"].As<bool>();
                
                if (success)
                {
                    // Extract outer boundary
                    PyObject outerBoundaryPy = pythonResult["outer_boundary"];
                    Vector2[] outerBoundary = ConvertPythonListToVector2Array(outerBoundaryPy);
                    
                    // Extract inner boundary
                    PyObject innerBoundaryPy = pythonResult["inner_boundary"];
                    Vector2[] innerBoundary = ConvertPythonListToVector2Array(innerBoundaryPy);
                    
                    result.outerBoundary = outerBoundary;
                    result.innerBoundary = innerBoundary;
                    result.success = true;

                    Debug.Log($"Successfully processed track image. Outer boundary: {result.outerBoundary.Length} points, Inner boundary: {result.innerBoundary.Length} points");
                }
                else
                {
                    PyObject errorPy = pythonResult["error"];
                    result.errorMessage = errorPy != null ? errorPy.As<string>() : "Unknown error occurred in Python script";
                }
            }
        }
        catch (Exception ex)
        {
            result.errorMessage = $"Error processing image: {ex.Message}";
            Debug.LogError(result.errorMessage);
        }

        return result;
    }

    private static Vector2[] ConvertPythonListToVector2Array(PyObject pythonList)
    {
        try
        {
            using (Py.GIL())
            {
                if (pythonList == null || !pythonList.HasAttr("__len__"))
                {
                    return new Vector2[0];
                }

                long length = pythonList.Length();
                Vector2[] result = new Vector2[length];

                for (int i = 0; i < length; i++)
                {
                    PyObject point = pythonList[i];
                    if (point.Length() >= 2)
                    {
                        float x = point[0].As<float>();
                        float y = point[1].As<float>();
                        result[i] = new Vector2(x, y);
                    }
                }

                return result;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error converting Python list to Vector2 array: {ex.Message}");
            return new Vector2[0];
        }
    }

    /// <summary>
    /// Process multiple track images in batch
    /// </summary>
    /// <param name="imagePaths">Array of image file paths</param>
    /// <returns>Array of TrackBoundaries results</returns>
    public static TrackBoundaries[] ProcessMultipleImages(string[] imagePaths)
    {
        TrackBoundaries[] results = new TrackBoundaries[imagePaths.Length];
        
        for (int i = 0; i < imagePaths.Length; i++)
        {
            results[i] = ProcessImage(imagePaths[i]);
        }
        
        return results;
    }

    /// <summary>
    /// Check if the required Python dependencies are available
    /// </summary>
    /// <returns>True if all dependencies are available</returns>
    public static bool CheckPythonDependencies()
    {
        try
        {
            if (!PythonNet.Instance.IsInitialized())
            {
                Debug.LogError("Python.NET is not initialized");
                return false;
            }

            string checkScript = @"
try:
    import cv2
    import numpy
    import scipy
    import skimage
    dependencies_available = True
    missing_deps = []
except ImportError as e:
    dependencies_available = False
    missing_deps = [str(e)]
";

            PyObject result = PythonNet.Instance.RunPythonScriptWithReturn(checkScript, "dependencies_available");
            
            using (Py.GIL())
            {
                bool available = result.As<bool>();
                if (!available)
                {
                    PyObject missingDeps = PythonNet.Instance.RunPythonScriptWithReturn(checkScript, "missing_deps");
                    Debug.LogError($"Missing Python dependencies: {missingDeps}");
                }
                return available;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error checking Python dependencies: {ex.Message}");
            return false;
        }
    }
}
