using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI.Extensions;
using LibTessDotNet;
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

  public static RacelineDisplayData LoadFromBinaryBytes(byte[] data)
  {
    var racelineData = new RacelineDisplayData();

    using (var ms = new MemoryStream(data))
    using (var br = new BinaryReader(ms))
    {
      racelineData.OuterBoundary = ReadPoints(br);
      racelineData.InnerBoundary = ReadPoints(br);
      racelineData.Raceline = ReadPoints(br);

      int trailing = br.ReadInt32();
      if (trailing != 0)
      {
        Debug.LogWarning("Warning: trailing value is not zero. Data may be corrupted.");
      }
    }

    return racelineData;
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

public class ShowRacingLine : MonoBehaviour, IDragHandler, IScrollHandler, IPointerDownHandler
{
  [Header("Display Settings")]
  public RectTransform trackContainer;
  public RectTransform zoomContainer;
  public RectTransform viewportRect;
  public Slider timeline;
  public Texture2D playTexture;
  public Texture2D pauseTexture;

  public Image playPauseImage;

  [Header("Line Renderer Settings")]
  public Material lineMaterial;
  public float outerBoundaryWidth = 1f;
  public float innerBoundaryWidth = 1f;
  public float racelineWidth = 1f;

  [Header("Track Colors")]
  public Color outerBoundaryColor = Color.blue;
  public Color innerBoundaryColor = Color.red;
  public Color roadColor = new Color(0.0f, 0.0f, 0.0f, 1);
  public Color racelineColor = Color.green;

  [Header("Track Controls")]
  public bool showOuterBoundary = true;
  public bool showInnerBoundary = true;
  public bool showRaceLine = true;

  [Header("Zoom/Pan Settings")]
  public float zoomSpeed = 0.1f;
  public float minZoom = 0.5f;
  public float maxZoom = 3f;
  [SerializeField] public float currentZoom = 1f;
  public float panSpeed = 1f;
  public bool invertZoom = false;
  public bool enablePanZoom = true;

  [Header("Car Cursor Settings")]
  public RectTransform carCursorPrefab;
  public float cursorSize = 5f;
  public float carSpeed = 50f;
  public Color trailColor = new Color(1f, 1f, 0f, 0.8f);

  [Header("Camera Follow")]
  public bool followCar = true;
  public float lerpSpeed = 5f;

  [Header("Time Control")]
  public float currentTime = 0f;
  public float timeSpeed = 1f;
  public bool isRacing = false;
  private RectTransform carCursor;
  private UILineRenderer trailLineRenderer;
  private List<Vector2> trailPositions = new List<Vector2>();
  private Vector2[] racelinePoints;
  private float traveledDistance = 0f;
  private RacelineDisplayData currentTrackData;
  private Dictionary<string, UILineRenderer> lineRenderers = new Dictionary<string, UILineRenderer>();
  private Vector2 panOffset = Vector2.zero;
  private Vector2 initialPosition;
  private bool isDragging = false;
  private Vector2 dragStartPosition;
  private bool firstFollowExecuted = false;
  private float followDelayTimer = 0f;
  private const float followDelay = 0.5f; // 500ms

  void Update()
  {
    if (!isRacing || racelinePoints == null || racelinePoints.Length < 2) return;

    if (!firstFollowExecuted)
    {
      followDelayTimer += Time.deltaTime;
      if (followDelayTimer >= followDelay)
      {
        if (followCar && carCursor)
        {
          Vector2 targetPos = -carCursor.anchoredPosition * currentZoom;
          panOffset = Vector2.Lerp(panOffset, targetPos, Time.deltaTime * lerpSpeed);
          UpdateZoomContainer();
        }
        firstFollowExecuted = true;
      }
    }
    else
    {
      if (followCar && carCursor)
      {
        Vector2 targetPos = -carCursor.anchoredPosition * currentZoom;
        panOffset = Vector2.Lerp(panOffset, targetPos, Time.deltaTime * lerpSpeed);
        UpdateZoomContainer();
      }
    }

    carCursor.sizeDelta = new Vector2(cursorSize, cursorSize);

    currentTime += Time.deltaTime * timeSpeed;

    float totalRaceTime = GetTotalRaceTime();

    if (timeline != null)
    {
      timeline.maxValue = totalRaceTime;
      timeline.minValue = 0f;
    }

    if (currentTime > totalRaceTime) currentTime = 0f;
    if (currentTime < 0f) currentTime = totalRaceTime;

    GoToTime(currentTime, totalRaceTime);

    if (timeline) timeline.value = currentTime;
  }

  void Start()
  {
    if (!zoomContainer && trackContainer) zoomContainer = trackContainer.parent as RectTransform;
    if (!viewportRect) viewportRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    initialPosition = zoomContainer.anchoredPosition;
    UpdateLineWidths();
  }

  private float[] racelineSegmentLengths;
  private float totalRacelineLength;

  private void SetupRacelineSegments()
  {
    if (racelinePoints == null || racelinePoints.Length < 2) return;

    racelineSegmentLengths = new float[racelinePoints.Length - 1];
    totalRacelineLength = 0f;

    for (int i = 1; i < racelinePoints.Length; i++)
    {
      racelineSegmentLengths[i - 1] = Vector2.Distance(racelinePoints[i - 1], racelinePoints[i]);
      totalRacelineLength += racelineSegmentLengths[i - 1];
    }
  }

  private float CalculateTotalLength()
  {
    float totalLength = 0f;
    for (int i = 1; i < racelinePoints.Length; i++)
    {
      totalLength += Vector2.Distance(racelinePoints[i - 1], racelinePoints[i]);
    }
    return totalLength;
  }

  private float GetTotalRaceTime()
  {
    float totalLength = CalculateTotalLength();
    return totalLength / carSpeed;
  }


  public void OnPointerDown(PointerEventData eventData)
  {
    if (!enablePanZoom) return;

    if (currentZoom > 1f)
    {
      isDragging = true;
      dragStartPosition = eventData.position;
      followCar = false;
    }
  }

  public void setZoom(float newZoom)
  {
    currentZoom = newZoom;
  }

  public void GoToTime(float targetTime, float totalLapTime)
  {
    if (racelinePoints == null || racelinePoints.Length < 2 || racelineSegmentLengths == null) return;

    targetTime = Mathf.Clamp(targetTime, 0f, totalLapTime);

    float fraction = targetTime / totalLapTime;

    float targetDistance = totalRacelineLength * fraction;

    float accumulated = 0f;
    for (int i = 0; i < racelineSegmentLengths.Length; i++)
    {
      if (accumulated + racelineSegmentLengths[i] >= targetDistance)
      {
        float segmentFraction = (targetDistance - accumulated) / racelineSegmentLengths[i];
        carCursor.anchoredPosition = Vector2.Lerp(racelinePoints[i], racelinePoints[i + 1], segmentFraction);
        return;
      }
      accumulated += racelineSegmentLengths[i];
    }
    carCursor.anchoredPosition = racelinePoints[racelinePoints.Length - 1];
  }

  public void OnDrag(PointerEventData eventData)
  {
    if (!enablePanZoom) return;

    if (currentZoom <= 1f || !isDragging) return;

    panOffset += (eventData.position - dragStartPosition) * panSpeed;
    dragStartPosition = eventData.position;
    ConstrainToViewport();
    UpdateZoomContainer();
  }

  public void OnScroll(PointerEventData eventData)
  {
    if (!enablePanZoom) return;
    float zoomDelta = eventData.scrollDelta.y * zoomSpeed * (invertZoom ? -1 : 1);
    float previousZoom = currentZoom;
    currentZoom = Mathf.Clamp(currentZoom + zoomDelta, minZoom, maxZoom);


    if (!Mathf.Approximately(previousZoom, currentZoom))
    {
      RectTransformUtility.ScreenPointToLocalPointInRectangle(zoomContainer, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
      float zoomRatio = currentZoom / previousZoom;
      panOffset = (panOffset - localPoint) * zoomRatio + localPoint;
    }

    if (Mathf.Approximately(currentZoom, 1f)) panOffset = Vector2.zero;
    ConstrainToViewport();
    UpdateZoomContainer();
    UpdateLineWidths();
  }

  public void ResetView()
  {
    currentZoom = 1f;
    panOffset = Vector2.zero;
    UpdateZoomContainer();
    UpdateLineWidths();
  }

  public void changeOuterBoundaryWidth(float newWidth)
  {
    outerBoundaryWidth = newWidth;

    if (lineRenderers.TryGetValue("OuterBoundary", out UILineRenderer renderer))
    {
      renderer.LineThickness = outerBoundaryWidth / currentZoom;
    }
  }

  public void changeInnerBoundaryWidth(float newWidth)
  {
    innerBoundaryWidth = newWidth;

    if (lineRenderers.TryGetValue("InnerBoundary", out UILineRenderer renderer))
    {
      renderer.LineThickness = innerBoundaryWidth / currentZoom;
    }
  }

  public void changeRaceLineWidth(float newWidth)
  {
    racelineWidth = newWidth;

    if (lineRenderers.TryGetValue("Raceline", out UILineRenderer renderer))
    {
      renderer.LineThickness = racelineWidth / currentZoom;
    }
  }

  private void ConstrainToViewport()
  {
    if (!viewportRect || !trackContainer || followCar) return;

    Vector2 scaledSize = trackContainer.rect.size * currentZoom;
    Vector2 viewportSize = viewportRect.rect.size;
    Vector2 maxOffset = Vector2.Max((scaledSize - viewportSize) * 0.5f, Vector2.zero);

    panOffset.x = Mathf.Clamp(panOffset.x, -maxOffset.x, maxOffset.x);
    panOffset.y = Mathf.Clamp(panOffset.y, -maxOffset.y, maxOffset.y);
  }


  private void UpdateZoomContainer()
  {
    if (!zoomContainer) return;
    zoomContainer.localScale = Vector3.one * currentZoom;
    zoomContainer.anchoredPosition = initialPosition + panOffset;
  }

  private void UpdateLineWidths()
  {
    foreach (var kvp in lineRenderers)
    {
      if (!kvp.Value) continue;
      float baseWidth = kvp.Key switch
      {
        "OuterBoundary" => outerBoundaryWidth,
        "InnerBoundary" => innerBoundaryWidth,
        "Raceline" => racelineWidth,
        _ => 1f
      };
      kvp.Value.LineThickness = baseWidth / currentZoom;
    }
  }

  List<UnityEngine.Vector2> ConvertToUnityVector2(List<System.Numerics.Vector2> list)
  {
    return list.Select(v => new UnityEngine.Vector2(v.X, v.Y)).ToList();
  }

  public void DisplayRacelineData(RacelineDisplayData trackData)
  {
    if (!trackContainer || trackData == null) return;

    ClearExistingLines();
    currentTrackData = trackData;
    (Vector2 min, Vector2 max, Vector2 size) bounds = CalculateBounds(trackData);
    float scale = CalculateScale(bounds.size);
    Vector2 offset = CalculateOffset(bounds.size, scale);


    CreateRoadArea(trackData.OuterBoundary, trackData.InnerBoundary, bounds.min, scale, offset);

    if (showOuterBoundary) CreateLineRenderer("OuterBoundary", trackData.OuterBoundary, outerBoundaryColor, outerBoundaryWidth, bounds.min, scale, offset);
    if (showInnerBoundary) CreateLineRenderer("InnerBoundary", trackData.InnerBoundary, innerBoundaryColor, innerBoundaryWidth, bounds.min, scale, offset);
    if (showRaceLine) CreateLineRenderer("Raceline", trackData.Raceline, racelineColor, racelineWidth, bounds.min, scale, offset);

    panOffset = Vector2.zero;
    UpdateZoomContainer();
    UpdateLineWidths();
  }


  private void ClearExistingLines()
  {
    foreach (Transform child in trackContainer) Destroy(child.gameObject);
    lineRenderers.Clear();
  }

  private (Vector2 min, Vector2 max, Vector2 size) CalculateBounds(RacelineDisplayData trackData)
  {
    Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
    Vector2 max = new Vector2(float.MinValue, float.MinValue);

    void UpdateBounds(List<Vector2> points)
    {
      if (points == null) return;
      for (int i = 0; i < points.Count; i++)
      {
        min = Vector2.Min(min, points[i]);
        max = Vector2.Max(max, points[i]);
      }
    }

    UpdateBounds(trackData.OuterBoundary);
    UpdateBounds(trackData.InnerBoundary);
    UpdateBounds(trackData.Raceline);

    return (min, max, max - min);
  }

  private float CalculateScale(Vector2 size)
  {
    float margin = 50f;
    return Mathf.Min((trackContainer.rect.width - 2 * margin) / size.x, (trackContainer.rect.height - 2 * margin) / size.y);
  }

  private Vector2 CalculateOffset(Vector2 size, float scale)
  {
    Vector2 scaledSize = size * scale;
    return new Vector2(
        (trackContainer.rect.width - scaledSize.x) * 0.5f,
        (trackContainer.rect.height - scaledSize.y) * 0.5f
    );
  }


  private Vector2 TransformPoint(Vector2 point, Vector2 min, float scale, Vector2 offset)
  {
    Vector2 transformed = (point - min) * scale + offset;
    transformed.y = trackContainer.rect.height - transformed.y;
    return transformed - trackContainer.rect.size * 0.5f;
  }

  private void CreateRoadArea(List<Vector2> outer, List<Vector2> inner, Vector2 min, float scale, Vector2 offset)
  {
    if (outer == null || inner == null || outer.Count < 3 || inner.Count < 3) return;

    List<Vector2> combined = new List<Vector2>(outer.Count + inner.Count);
    combined.AddRange(outer);
    inner.Reverse();
    combined.AddRange(inner);

    GameObject roadGO = new GameObject("RoadMesh", typeof(RectTransform), typeof(CanvasRenderer), typeof(UIMeshPolygon));
    roadGO.transform.SetParent(trackContainer, false);

    UIMeshPolygon roadMesh = roadGO.GetComponent<UIMeshPolygon>();
    roadMesh.Points = combined.ConvertAll(p => TransformPoint(p, min, scale, offset));
    roadMesh.color = roadColor;
  }


  private void CreateLineRenderer(string key, List<Vector2> points, Color color, float width, Vector2 min, float scale, Vector2 offset)
  {
    if (points == null || points.Count < 2) return;

    UILineRenderer lr = new GameObject(key, typeof(RectTransform), typeof(UILineRenderer)).GetComponent<UILineRenderer>();
    lr.transform.SetParent(trackContainer, false);
    lr.material = lineMaterial;
    lr.color = color;
    lr.LineThickness = width / currentZoom;
    lr.Points = points.ConvertAll(p => TransformPoint(p, min, scale, offset)).ToArray();
    lineRenderers[key] = lr;
  }

  public void InitializeWithTrack(string trackName)
  {
    if (string.IsNullOrEmpty(trackName))
    {
      Debug.LogError("Track name is null or empty in InitializeWithTrack");
      return;
    }
    LoadTrackData(trackName);
  }

  private void LoadTrackData(string trackName)
  {
    currentTrackData = null;

    APIManager.Instance.GetTrackBorder(trackName, (success, message, bytes) =>
    {
      if (success && bytes != null)
      {
        RacelineDisplayData Data = RacelineDisplayImporter.LoadFromBinaryBytes(bytes);

        float simplificationTolerance = 10f;

        RacelineDisplayData trackData = new RacelineDisplayData
        {
          InnerBoundary = LineSimplifier.SmoothLine(LineSimplifier.RamerDouglasPeucker(Data.InnerBoundary, simplificationTolerance)),
          OuterBoundary = LineSimplifier.SmoothLine(LineSimplifier.RamerDouglasPeucker(Data.OuterBoundary, simplificationTolerance)),
          Raceline = Data.Raceline
        };
        if (trackData != null)
        {
          DisplayRacelineData(trackData);
          SetupCarCursor();
        }
        else
        {
          Debug.LogError($"Failed to parse track data for {trackName}");
        }
      }
      else
      {
        Debug.LogError($"Failed to load track borders: {message}");

      }
    });
  }

  private void SetupCarCursor()
  {
    if (!carCursorPrefab || !trackContainer) return;

    carCursor = Instantiate(carCursorPrefab, trackContainer);
    carCursor.name = "CarCursor";
    carCursor.anchorMin = carCursor.anchorMax = new Vector2(0.5f, 0.5f);

    GameObject trailGO = new GameObject("CarTrail", typeof(RectTransform), typeof(UILineRenderer));
    trailGO.transform.SetParent(trackContainer, false);
    trailLineRenderer = trailGO.GetComponent<UILineRenderer>();
    trailLineRenderer.material = lineMaterial;
    trailLineRenderer.color = trailColor;
    trailLineRenderer.LineThickness = racelineWidth / currentZoom;

    (Vector2 min, Vector2 max, Vector2 size) bounds = CalculateBounds(currentTrackData);
    float scale = CalculateScale(bounds.size);
    Vector2 offset = CalculateOffset(bounds.size, scale);

    racelinePoints = currentTrackData.Raceline.ConvertAll(p => TransformPoint(p, bounds.min, scale, offset)).ToArray();
    SetupRacelineSegments();

    traveledDistance = 0f;
    trailPositions.Clear();
  }

  public void SetCarSpeed(float newSpeed)
  {
    if (carSpeed <= 0) return;

    float oldTotalTime = GetTotalRaceTime();
    float progress = currentTime / oldTotalTime;

    carSpeed = newSpeed;

    float newTotalTime = GetTotalRaceTime();
    currentTime = progress * newTotalTime;
  }

  public void setCurrentTime(float time)
  {
    currentTime = time;
  }

  public void FastForward()
  {
    switch (timeSpeed)
    {
      case 1f:
        timeSpeed = 2f;
        lerpSpeed = 15f;
        break;
      case 2f:
        timeSpeed = 4f;
        lerpSpeed = 30f;
        break;
      case 4f:
        timeSpeed = 8f;
        lerpSpeed = 60f;
        break;
      case 8f:
        timeSpeed = 16f;
        lerpSpeed = 120f;
        break;
      case 16f:
        timeSpeed = 32f;
        lerpSpeed = 240f;
        break;
      default:
        timeSpeed = 1f;
        lerpSpeed = 10f;
        break;
    }
    playPauseImage.sprite = SpriteFromTexture(playTexture);
  }

  public void PlayPause()
  {
    if (timeSpeed != 0)
    {
      timeSpeed = 0f;
      playPauseImage.sprite = SpriteFromTexture(pauseTexture);
    }
    else
    {
      timeSpeed = 1f;
      playPauseImage.sprite = SpriteFromTexture(playTexture);
    }
  }

  public void Rewind()
  {
    switch (timeSpeed)
    {
      case -1f:
        timeSpeed = -2f;
        lerpSpeed = 15f;
        break;
      case -2f:
        timeSpeed = -4f;
        lerpSpeed = 30f;
        break;
      case -4f:
        timeSpeed = -8f;
        lerpSpeed = 60f;
        break;
      case -8f:
        timeSpeed = -16f;
        lerpSpeed = 120f;
        break;
      case -16f:
        timeSpeed = -32f;
        lerpSpeed = 240f;
        break;
      default:
        timeSpeed = -1f;
        lerpSpeed = 10f;
        break;
    }
    playPauseImage.sprite = SpriteFromTexture(playTexture);
  }


  private Sprite SpriteFromTexture(Texture2D texture)
  {
    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                         new Vector2(0.5f, 0.5f));
  }
}
