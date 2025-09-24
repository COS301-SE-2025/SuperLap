using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Globalization;

public class ImageProcessing
{
  [Serializable]
  public class TrackBoundaries
  {
    public List<Vector2> outerBoundary;
    public List<Vector2> innerBoundary;
    public bool success;
    public string errorMessage;
  }

  public static TrackBoundaries ProcessImage(string imagePath, bool isGrayScale = true)
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

      Debug.Log($"IsGrayScale: {isGrayScale}");

      // Get the path to the executable
      string exeName;

      if (isGrayScale)
      {
        exeName = "TrackProcessor";
      }
      else
      {
        exeName = "CNN";
      }

      if (Application.platform == RuntimePlatform.WindowsPlayer ||
          Application.platform == RuntimePlatform.WindowsEditor)
      {
        exeName += ".exe";
      }

      Debug.Log($"Using executable: {exeName}");
      string exePath = Path.Combine(Application.streamingAssetsPath, exeName);
      if (!File.Exists(exePath))
      {
        result.errorMessage = $"TrackProcessor executable not found at: {exePath}";
        return result;
      }

      Debug.Log($"Using executable at: {exePath}");

      // Create unique output file path in temp directory
      string outputFileName = $"track_result_{Guid.NewGuid():N}.json";
      string outputFilePath = Path.Combine(Application.streamingAssetsPath, outputFileName);
      // string outputFilePath = Path.Combine(Path.GetTempPath(), outputFileName);
      Debug.Log($"Output file will be: {outputFilePath}");

      // Create and configure the process
      ProcessStartInfo startInfo = new ProcessStartInfo
      {
        FileName = exePath,
        Arguments = $"\"{imagePath}\" \"{outputFilePath}\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
      };

      // Execute the process
      using (Process process = Process.Start(startInfo))
      {
        if (process == null)
        {
          result.errorMessage = "Failed to start TrackProcessor";
          return result;
        }

        // Read any error output
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        // Check if process executed successfully
        if (process.ExitCode != 0)
        {
          result.errorMessage = $"TrackProcessor failed with exit code {process.ExitCode}. Error: {error}";
          Debug.Log(result.errorMessage);
          // CleanupOutputFile(outputFilePath);
          return result;
        }

        // Check if output file was created
        if (!File.Exists(outputFilePath))
        {
          result.errorMessage = "TrackProcessor did not create output file";
          Debug.Log(result.errorMessage);
          return result;
        }

        // Read the output file
        string jsonContent = File.ReadAllText(outputFilePath);
        Debug.Log($"Read output file with {jsonContent.Length} characters");

        // Parse the results
        result = ParseTrackBoundaries(jsonContent);

        // Cleanup the temporary file
        // CleanupOutputFile(outputFilePath);

        if (result.success)
        {
          Debug.Log($"Successfully processed track image. Outer boundary: {result.outerBoundary.Count} points, Inner boundary: {result.innerBoundary.Count} points");
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

  private static void CleanupOutputFile(string filePath)
  {
    try
    {
      if (File.Exists(filePath))
      {
        File.Delete(filePath);
      }
    }
    catch (Exception ex)
    {
      Debug.LogWarning($"Could not delete temporary file {filePath}: {ex.Message}");
    }
  }

  private static TrackBoundaries ParseTrackBoundaries(string jsonContent)
  {
    var result = new TrackBoundaries { success = false };

    try
    {
      // Parse JSON manually - look for the structure we expect
      if (jsonContent.Contains("\"success\":true"))
      {
        result.success = true;

        // Extract outer boundary
        result.outerBoundary = ExtractBoundaryFromJson(jsonContent, "outer_boundary");

        // Extract inner boundary  
        result.innerBoundary = ExtractBoundaryFromJson(jsonContent, "inner_boundary");

        Debug.Log($"Parsed boundaries - Outer: {result.outerBoundary.Count}, Inner: {result.innerBoundary.Count}");
      }
      else
      {
        result.success = false;
        result.errorMessage = ExtractErrorFromJson(jsonContent);
      }
    }
    catch (Exception ex)
    {
      result.success = false;
      result.errorMessage = $"Error parsing JSON: {ex.Message}";
      Debug.LogError($"JSON parsing error: {ex.Message}");
    }

    return result;
  }

  private static List<Vector2> ExtractBoundaryFromJson(string json, string boundaryKey)
  {
    List<Vector2> boundary = new List<Vector2>();

    try
    {
      // Find the start of the boundary array
      string searchKey = $"\"{boundaryKey}\":[";
      int startIndex = json.IndexOf(searchKey);

      if (startIndex == -1)
      {
        Debug.LogWarning($"Could not find {boundaryKey} in JSON");
        return boundary;
      }

      // Move to start of array content
      startIndex += searchKey.Length;

      // Find the end of the array (matching closing bracket)
      int bracketCount = 1;
      int endIndex = startIndex;

      while (endIndex < json.Length && bracketCount > 0)
      {
        char c = json[endIndex];
        if (c == '[') bracketCount++;
        else if (c == ']') bracketCount--;
        endIndex++;
      }

      if (bracketCount != 0)
      {
        Debug.LogError($"Could not find matching closing bracket for {boundaryKey}");
        return boundary;
      }
      // Extract the array content (without outer brackets)
      string arrayContent = json.Substring(startIndex, endIndex - startIndex - 1);
      // Parse coordinate pairs
      boundary = ParseCoordinatePairs(arrayContent);

    }
    catch (Exception ex)
    {
      Debug.LogError($"Error extracting {boundaryKey}: {ex.Message}");
    }

    return boundary;
  }

  private static List<Vector2> ParseCoordinatePairs(string arrayContent)
  {
    var coordinates = new List<Vector2>();
    try
    {
      int index = 0;
      while (index < arrayContent.Length)
      {
        int pairStart = arrayContent.IndexOf('[', index);
        if (pairStart == -1) break;

        int pairEnd = arrayContent.IndexOf(']', pairStart);
        if (pairEnd == -1) break;

        string pairContent = arrayContent.Substring(pairStart + 1, pairEnd - pairStart - 1);

        string[] coords = pairContent.Split(',');
        if (coords.Length >= 2)
        {
          string sx = coords[0].Trim();
          string sy = coords[1].Trim();

          // Use InvariantCulture so JSON-style floats with '.' always parse
          if (float.TryParse(sx, NumberStyles.Float | NumberStyles.AllowThousands,
                             CultureInfo.InvariantCulture, out float x) &&
              float.TryParse(sy, NumberStyles.Float | NumberStyles.AllowThousands,
                             CultureInfo.InvariantCulture, out float y))
          {
            coordinates.Add(new Vector2(x, y));
          }
          else
          {
            Debug.LogWarning($"Failed to parse '{pairContent}'. sx='{sx}', sy='{sy}', currentCulture={CultureInfo.CurrentCulture.Name}");
          }
        }

        index = pairEnd + 1;
      }
    }
    catch (Exception ex)
    {
      Debug.LogError($"Error parsing coordinate pairs: {ex}");
    }

    Debug.Log($"ParseCoordinatePairs produced {coordinates.Count} coordinates");
    return coordinates;
  }


  private static string ExtractErrorFromJson(string json)
  {
    try
    {
      string searchKey = "\"error\":\"";
      int startIndex = json.IndexOf(searchKey);

      if (startIndex == -1) return "Unknown error";

      startIndex += searchKey.Length;
      int endIndex = json.IndexOf("\"", startIndex);

      if (endIndex == -1) return "Unknown error";

      return json.Substring(startIndex, endIndex - startIndex);
    }
    catch
    {
      return "Unknown error";
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
  /// Check if the TrackProcessor executable is available
  /// </summary>
  /// <returns>True if the executable exists and is accessible</returns>
  public static bool CheckExecutableAvailability()
  {
    try
    {
      string exeName = (Application.platform == RuntimePlatform.WindowsPlayer) ? "TrackProcessor.exe" : "TrackProcessor";
      string exePath = Path.Combine(Application.streamingAssetsPath, exeName);

      if (!File.Exists(exePath))
      {
        Debug.LogError($"TrackProcessor executable not found at: {exePath}");
        return false;
      }

      return true;
    }
    catch (Exception ex)
    {
      Debug.LogError($"Error checking executable availability: {ex.Message}");
      return false;
    }
  }
}