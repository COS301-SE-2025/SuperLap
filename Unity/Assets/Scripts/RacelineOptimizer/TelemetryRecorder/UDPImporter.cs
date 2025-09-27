using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace CSVToBinConverter
{
  [Serializable]
  public class PlayerlineSettings
  {
    public float tx;
    public float ty;
    public float scale;
    public float rotation;
    public bool reflect_x;
    public bool reflect_y;
  }

  public static class LoadUDP
  {
    public static LoadCSV.PlayerLine Convert(
        List<MotoGPTelemetry.RecordedData> recordedData,
        int targetLapIndex = 0)
    {
      List<Vector2> playerline = new();
      string trackName = "Unknown";

      foreach (var record in recordedData)
      {
        if (record.CurrentLap != targetLapIndex)
          continue;

        trackName = record.TrackId;

        float x = record.CoordinatesX;
        float y = record.CoordinatesY;

        // Load JSON settings for this track
        string settingsPath = Path.Combine(Application.streamingAssetsPath, "AdjustPlayerlineSettings", $"{trackName}.json");

        PlayerlineSettings settings = LoadSettings(settingsPath);

        if (settings == null)
        {
          Debug.LogError($"Settings not found or invalid for track {trackName}. Using defaults.");
          settings = new PlayerlineSettings
          {
            tx = 0f,
            ty = 0f,
            scale = 1f,
            rotation = 0f,
            reflect_x = false,
            reflect_y = false
          };
        }

        float rad = settings.rotation * (float)Math.PI / 180f;
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);

        float rotatedX = cos * x - sin * y;
        float rotatedY = sin * x + cos * y;

        rotatedX *= settings.scale;
        rotatedY *= settings.scale;

        if (settings.reflect_x) rotatedX = -rotatedX;
        if (settings.reflect_y) rotatedY = -rotatedY;

        rotatedX += settings.tx;
        rotatedY += settings.ty;

        playerline.Add(new Vector2(rotatedX, rotatedY));
      }

      if (playerline.Count == 0)
      {
        Debug.Log("No valid playerline data found.");
        return null;
      }

      string trackPath = Path.Combine(Application.streamingAssetsPath, "MotoGPTracks", $"{trackName}.bin");
      if (!File.Exists(trackPath))
      {
        Debug.LogError($"Edge file not found at {trackPath}");
        return null;
      }

      EdgeData edgeData = EdgeData.LoadFromBinary(trackPath, true);

      if (edgeData.OuterBoundary.Count == 0 || edgeData.InnerBoundary.Count == 0)
      {
        Debug.LogError($"Edge data for {trackName} is empty or malformed.");
        return null;
      }

      List<Vector2> worstSections = DeviationAnalyzer.GetWorstDeviationSections(playerline, edgeData.Raceline, 5);
      Debug.Log($"Found {worstSections.Count} worst deviation sections.");

      return new LoadCSV.PlayerLine
      {
        InnerBoundary = edgeData.InnerBoundary,
        OuterBoundary = edgeData.OuterBoundary,
        Raceline = edgeData.Raceline,
        PlayerPath = playerline,
        WorstDeviationSections = worstSections
      };
    }

    public static PlayerlineSettings LoadSettings(string path)
    {
      try
      {
        if (!File.Exists(path))
          return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<PlayerlineSettings>(json);
      }
      catch (Exception ex)
      {
        Debug.LogError($"Failed to load playerline settings from {path}: {ex.Message}");
        return null;
      }
    }
  }
}
