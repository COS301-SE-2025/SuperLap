using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class RacelineDisplayData
{
  public List<Vector2> OuterBoundary { get; set; }
  public List<Vector2> InnerBoundary { get; set; }
  public List<Vector2> Raceline { get; set; }
}

public static class RacelineDisplayImporter
{
  public static RacelineDisplayData LoadFromBinary(string filePath)
  {
    var data = new RacelineDisplayData();

    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
    using (var br = new BinaryReader(fs))
    {
      data.OuterBoundary = ReadPoints(br);
      data.InnerBoundary = ReadPoints(br);
      data.Raceline = ReadPoints(br);

      int trailing = br.ReadInt32();
      if (trailing != 0)
      {
        Debug.LogWarning("Warning: trailing value is not zero. File format may be corrupted.");
      }
    }

    return data;
  }

  private static List<Vector2> ReadPoints(BinaryReader br)
  {
    int count = br.ReadInt32();
    var list = new List<Vector2>(count);
    for (int i = 0; i < count; i++)
    {
      float x = br.ReadSingle();
      float y = br.ReadSingle();
      list.Add(new Vector2(x, y));
    }
    return list;
  }
}

public class ShowRacingLine : MonoBehaviour
{
  [Header("Display Settings")]
  public Image racelineImage;

  [Header("Data Source")]
  public string binaryDataPath = "tracks/";

  [Header("Animation Settings")]
  public float animationSpeed = 200.0f;
  public Color cursorColor = Color.yellow;
  public int cursorSize = 8;
  public Color trailColor = Color.green;
  public float trailFadeLength = 50f;

  [Header("Track Colors")]
  public Color outerBoundaryColor = Color.blue;
  public Color innerBoundaryColor = Color.red;
  public Color backgroundColor = new Color(54f / 255f, 54f / 255f, 54f / 255f, 1.0f);

  private RacelineDisplayData currentTrackData;
  private string currentTrackName = "";
  private Texture2D baseTexture;
  private bool isAnimating = false;
  private float animationTime = 0f;
  private Vector2 trackMin, trackMax;
  private float trackScale;
  private Vector2 trackOffset;
  private int textureWidth = 1024;
  private int textureHeight = 1024;

  void Start()
  {

  }

  void Update()
  {
    if (isAnimating && currentTrackData != null && currentTrackData.Raceline != null &&
        currentTrackData.Raceline.Count > 0 && baseTexture != null)
    {
      animationTime += Time.deltaTime * animationSpeed;

      if (animationTime >= currentTrackData.Raceline.Count)
      {
        animationTime = 0f;
      }

      UpdateAnimatedTexture();
    }
  }

  public void DisplayRacelineData(RacelineDisplayData trackData, string trackName = "")
  {
    if (trackData == null)
    {
      Debug.LogError("Track data is null");
      return;
    }

    currentTrackData = trackData;
    currentTrackName = trackName;
    baseTexture = GenerateTrackTexture(currentTrackData);

    if (baseTexture != null && racelineImage != null)
    {
      Sprite sprite = Sprite.Create(baseTexture, new Rect(0, 0, baseTexture.width, baseTexture.height), Vector2.one * 0.5f);
      racelineImage.sprite = sprite;

      if (currentTrackData.Raceline != null && currentTrackData.Raceline.Count > 0)
      {
        isAnimating = true;
        animationTime = 0f;
        Debug.Log($"Racing line animation started for track: {currentTrackName}");
      }
    }
    else
    {
      Debug.LogError("Failed to generate texture or racelineImage is null");
    }
  }

  private Texture2D GenerateTrackTexture(RacelineDisplayData trackData)
  {
    Texture2D texture = new Texture2D(textureWidth, textureHeight);
    Color[] pixels = new Color[textureWidth * textureHeight];

    for (int i = 0; i < pixels.Length; i++)
    {
      pixels[i] = backgroundColor;
    }

    Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
    Vector2 max = new Vector2(float.MinValue, float.MinValue);

    if (trackData.OuterBoundary != null && trackData.OuterBoundary.Count > 0)
    {
      foreach (var point in trackData.OuterBoundary)
      {
        min = Vector2.Min(min, point);
        max = Vector2.Max(max, point);
      }
    }

    if (trackData.InnerBoundary != null && trackData.InnerBoundary.Count > 0)
    {
      foreach (var point in trackData.InnerBoundary)
      {
        min = Vector2.Min(min, point);
        max = Vector2.Max(max, point);
      }
    }

    if (trackData.Raceline != null && trackData.Raceline.Count > 0)
    {
      foreach (var point in trackData.Raceline)
      {
        min = Vector2.Min(min, point);
        max = Vector2.Max(max, point);
      }
    }

    Vector2 size = max - min;

    float margin = 50f;
    float scaleX = (textureWidth - 2 * margin) / size.x;
    float scaleY = (textureHeight - 2 * margin) / size.y;
    float scale = Mathf.Min(scaleX, scaleY);

    Vector2 scaledSize = size * scale;
    Vector2 offset = new Vector2(
        (textureWidth - scaledSize.x) * 0.5f,
        (textureHeight - scaledSize.y) * 0.5f
    );

    trackMin = min;
    trackMax = max;
    trackScale = scale;
    trackOffset = offset;

    if (trackData.OuterBoundary != null && trackData.OuterBoundary.Count > 0)
    {
      DrawLine(pixels, textureWidth, textureHeight, trackData.OuterBoundary, outerBoundaryColor, min, scale, offset);
    }

    if (trackData.InnerBoundary != null && trackData.InnerBoundary.Count > 0)
    {
      DrawLine(pixels, textureWidth, textureHeight, trackData.InnerBoundary, innerBoundaryColor, min, scale, offset);
    }

    texture.SetPixels(pixels);
    texture.Apply();

    return texture;
  }

  private void DrawLine(Color[] pixels, int width, int height, List<Vector2> points, Color color, Vector2 min, float scale, Vector2 offset)
  {
    if (points == null || points.Count < 2) return;

    for (int i = 0; i < points.Count - 1; i++)
    {
      Vector2 from = TransformPoint(points[i], min, scale, offset);
      Vector2 to = TransformPoint(points[i + 1], min, scale, offset);

      DrawPixelLine(pixels, width, height, from, to, color);
    }

    if (points.Count > 2)
    {
      Vector2 from = TransformPoint(points[points.Count - 1], min, scale, offset);
      Vector2 to = TransformPoint(points[0], min, scale, offset);
      DrawPixelLine(pixels, width, height, from, to, color);
    }
  }

  private Vector2 TransformPoint(Vector2 point, Vector2 min, float scale, Vector2 offset)
  {
    Vector2 transformed = (point - min) * scale + offset;
    transformed.y = textureHeight - transformed.y;
    return transformed;
  }

  private void DrawPixelLine(Color[] pixels, int width, int height, Vector2 from, Vector2 to, Color color)
  {
    int x0 = Mathf.RoundToInt(from.x);
    int y0 = Mathf.RoundToInt(from.y);
    int x1 = Mathf.RoundToInt(to.x);
    int y1 = Mathf.RoundToInt(to.y);

    int dx = Mathf.Abs(x1 - x0);
    int dy = Mathf.Abs(y1 - y0);
    int sx = x0 < x1 ? 1 : -1;
    int sy = y0 < y1 ? 1 : -1;
    int err = dx - dy;

    while (true)
    {
      for (int offsetX = -1; offsetX <= 1; offsetX++)
      {
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
          int pixelX = x0 + offsetX;
          int pixelY = y0 + offsetY;
          if (pixelX >= 0 && pixelX < width && pixelY >= 0 && pixelY < height)
          {
            Color existingColor = pixels[pixelY * width + pixelX];
            pixels[pixelY * width + pixelX] = Color.Lerp(existingColor, color, color.a);
          }
        }
      }

      if (x0 == x1 && y0 == y1) break;

      int e2 = 2 * err;
      if (e2 > -dy)
      {
        err -= dy;
        x0 += sx;
      }
      if (e2 < dx)
      {
        err += dx;
        y0 += sy;
      }
    }
  }

  private void UpdateAnimatedTexture()
  {
    Texture2D animatedTexture = new Texture2D(baseTexture.width, baseTexture.height);
    Color[] pixels = baseTexture.GetPixels();

    int currentPointIndex = Mathf.FloorToInt(animationTime);
    float t = animationTime - currentPointIndex;

    Vector2 currentPoint = currentTrackData.Raceline[currentPointIndex];
    Vector2 nextPoint = currentTrackData.Raceline[(currentPointIndex + 1) % currentTrackData.Raceline.Count];

    Vector2 animatedPosition = Vector2.Lerp(currentPoint, nextPoint, t);

    DrawAnimatedTrail(pixels, baseTexture.width, baseTexture.height, currentPointIndex, t);

    Vector2 texturePos = TransformPoint(animatedPosition, trackMin, trackScale, trackOffset);

    DrawCursor(pixels, baseTexture.width, baseTexture.height, texturePos, cursorColor, cursorSize);

    animatedTexture.SetPixels(pixels);
    animatedTexture.Apply();

    Sprite animatedSprite = Sprite.Create(animatedTexture, new Rect(0, 0, animatedTexture.width, animatedTexture.height), Vector2.one * 0.5f);
    racelineImage.sprite = animatedSprite;
  }

  private void DrawAnimatedTrail(Color[] pixels, int width, int height, int currentPointIndex, float t)
  {
    int totalPoints = currentTrackData.Raceline.Count;
    int trailLength = Mathf.Min(Mathf.RoundToInt(trailFadeLength), totalPoints);

    for (int i = 0; i < trailLength; i++)
    {
      int pointIndex = (currentPointIndex - i + totalPoints) % totalPoints;
      int nextPointIndex = (pointIndex + 1) % totalPoints;

      float fadeIntensity = 1.0f - (float)i / trailLength;

      if (i == 0)
      {
        fadeIntensity *= (1.0f - t);
      }

      Color segmentColor = new Color(trailColor.r, trailColor.g, trailColor.b, trailColor.a * fadeIntensity);

      Vector2 from = TransformPoint(currentTrackData.Raceline[pointIndex], trackMin, trackScale, trackOffset);
      Vector2 to = TransformPoint(currentTrackData.Raceline[nextPointIndex], trackMin, trackScale, trackOffset);

      DrawPixelLine(pixels, width, height, from, to, segmentColor);
    }
  }

  private void DrawCursor(Color[] pixels, int width, int height, Vector2 position, Color color, int size)
  {
    int centerX = Mathf.RoundToInt(position.x);
    int centerY = Mathf.RoundToInt(position.y);
    int radius = size / 2;

    for (int x = centerX - radius; x <= centerX + radius; x++)
    {
      for (int y = centerY - radius; y <= centerY + radius; y++)
      {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
          float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
          if (distance <= radius)
          {
            float intensity = 1.0f - (distance / radius);
            Color finalColor = Color.Lerp(Color.clear, color, intensity);

            Color existingColor = pixels[y * width + x];
            pixels[y * width + x] = Color.Lerp(existingColor, finalColor, finalColor.a);
          }
        }
      }
    }
  }

  public void ToggleAnimation()
  {
    isAnimating = !isAnimating;
    Debug.Log($"Animation toggled: {isAnimating}");
  }

  public void SetAnimationSpeed(float speed)
  {
    animationSpeed = speed;
  }

  public void StartAnimation()
  {
    if (currentTrackData != null && currentTrackData.Raceline != null && currentTrackData.Raceline.Count > 0)
    {
      isAnimating = true;
      animationTime = 0f;
    }
  }

  public void StopAnimation()
  {
    isAnimating = false;
  }

  public void SetTrailLength(float length)
  {
    trailFadeLength = length;
  }

  public void OnSpeedSliderChanged(float sliderValue)
  {
    animationSpeed = Mathf.Lerp(50f, 1000f, sliderValue);
    Debug.Log($"Animation speed changed to: {animationSpeed:F1}");
  }

  public void OnCursorSizeSliderChanged(float sliderValue)
  {
    cursorSize = Mathf.RoundToInt(Mathf.Lerp(5f, 50f, sliderValue));
    Debug.Log($"Cursor size changed to: {cursorSize}");
  }

  public void OnTrailLengthSliderChanged(float sliderValue)
  {
    trailFadeLength = Mathf.Lerp(10f, 300f, sliderValue);
    Debug.Log($"Trail length changed to: {trailFadeLength:F1}");
  }

  public void SetAnimationSpeedFromSlider(float sliderValue, float minSpeed = 25f, float maxSpeed = 750f)
  {
    animationSpeed = Mathf.Lerp(minSpeed, maxSpeed, sliderValue);
    Debug.Log($"Animation speed set to: {animationSpeed:F1}");
  }

  public void SetCursorSizeFromSlider(float sliderValue, int minSize = 2, int maxSize = 30)
  {
    cursorSize = Mathf.RoundToInt(Mathf.Lerp(minSize, maxSize, sliderValue));
    Debug.Log($"Cursor size set to: {cursorSize}");
  }

  public void SetTrailLengthFromSlider(float sliderValue, float minLength = 5f, float maxLength = 150f)
  {
    trailFadeLength = Mathf.Lerp(minLength, maxLength, sliderValue);
    Debug.Log($"Trail length set to: {trailFadeLength:F1}");
  }

  public float GetNormalizedAnimationSpeed(float minSpeed = 50f, float maxSpeed = 500f)
  {
    return Mathf.InverseLerp(minSpeed, maxSpeed, animationSpeed);
  }

  public float GetNormalizedCursorSize(int minSize = 4, int maxSize = 20)
  {
    return Mathf.InverseLerp(minSize, maxSize, cursorSize);
  }

  public float GetNormalizedTrailLength(float minLength = 10f, float maxLength = 100f)
  {
    return Mathf.InverseLerp(minLength, maxLength, trailFadeLength);
  }

  public string GetCurrentTrackName()
  {
    return currentTrackName;
  }

  public void SetTrackName(string trackName)
  {
    currentTrackName = trackName;
    Debug.Log($"Track name set to: {currentTrackName}");
  }

  public void InitializeWithTrack(string trackName)
  {
    SetTrackName(trackName);
    LoadTrackData(trackName);
  }

  public void InitializeWithTrack(APIManager.Track track)
  {
    if (track != null)
    {
      SetTrackName(track.name);
      LoadTrackData(track.name);
    }
    else
    {
      Debug.LogError("Track object is null");
      SetTrackName("Unknown Track");
    }
  }

  public void InitializeWithTrackByIndex(int trackIndex)
  {
    Debug.Log($"Initialize racing line with track index: {trackIndex}");

    string defaultTrackName = $"Track_{trackIndex}";
    SetTrackName(defaultTrackName);
    LoadTrackData(defaultTrackName);
  }

  private void LoadTrackData(string trackName)
  {
    Debug.Log($"Loading track data for: {trackName}");

    string binaryFilePath = GetBinaryFilePath(trackName);

    string fullPath = Path.GetFullPath(binaryFilePath);
    Debug.Log($"Looking for binary file at relative path: {binaryFilePath}");
    Debug.Log($"Full absolute path: {fullPath}");
    Debug.Log($"Current working directory: {System.IO.Directory.GetCurrentDirectory()}");
    Debug.Log($"Application data path: {Application.dataPath}");
    Debug.Log($"Application persistent data path: {Application.persistentDataPath}");
    Debug.Log($"File exists check: {File.Exists(binaryFilePath)}");
    Debug.Log($"File exists check (full path): {File.Exists(fullPath)}");

    if (File.Exists(binaryFilePath))
    {
      LoadTrackFromBinary(binaryFilePath);
    }
    else
    {
      Debug.LogWarning($"Binary file not found for track: {trackName} at path: {binaryFilePath}");
      Debug.LogWarning($"Also checked full path: {fullPath}");

      string tracksDir = binaryDataPath;
      if (Directory.Exists(tracksDir))
      {
        string[] files = Directory.GetFiles(tracksDir);
        Debug.Log($"Files found in tracks directory ({tracksDir}):");
        foreach (string file in files)
        {
          Debug.Log($"  - {file}");
        }
      }
      else
      {
        Debug.LogWarning($"Tracks directory does not exist: {tracksDir}");
        Debug.Log($"Full tracks directory path: {Path.GetFullPath(tracksDir)}");
      }
    }
  }

  private string GetBinaryFilePath(string trackName)
  {
    string fileName = trackName.ToLower().Replace(" ", "_") + ".bin";
    if (trackName.ToLower().Contains("test"))
    {
      fileName = "test1.bin";
    }

    string assetsPath = Path.Combine(Application.dataPath, "tracks", fileName);
    return assetsPath;
  }

  private void LoadTrackFromBinary(string filePath)
  {
    try
    {
      RacelineDisplayData trackData = RacelineDisplayImporter.LoadFromBinary(filePath);
      DisplayRacelineData(trackData, currentTrackName);
      Debug.Log($"Successfully loaded binary track data from: {filePath}");
    }
    catch (System.Exception e)
    {
      Debug.LogError($"Error loading track from binary file {filePath}: {e.Message}");
    }
  }

  public bool IsTrackDataLoaded()
  {
    return currentTrackData != null &&
           currentTrackData.Raceline != null &&
           currentTrackData.Raceline.Count > 0;
  }

  public void LogTrackInfo()
  {
    if (currentTrackData != null)
    {
      Debug.Log($"Track: {currentTrackName}");
      Debug.Log($"Outer Boundary Points: {currentTrackData.OuterBoundary?.Count ?? 0}");
      Debug.Log($"Inner Boundary Points: {currentTrackData.InnerBoundary?.Count ?? 0}");
      Debug.Log($"Raceline Points: {currentTrackData.Raceline?.Count ?? 0}");
      Debug.Log($"Animation Active: {isAnimating}");
    }
    else
    {
      Debug.Log("No track data loaded");
    }
  }
}
