using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI.Extensions;
using LibTessDotNet;
using System.IO;
using System.Collections;
using System;

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
  [SerializeField] private RectTransform trackContainer;
  [SerializeField] private RectTransform zoomContainer;
  [SerializeField] private RectTransform viewportRect;
  [SerializeField] private Slider timeline;
  [SerializeField] private Texture2D playTexture;
  [SerializeField] private Texture2D pauseTexture;
  [SerializeField] private Texture2D play_0;
  [SerializeField] private Texture2D play_1;
  [SerializeField] private Texture2D play_2;
  [SerializeField] private Texture2D play_3;
  [SerializeField] private Texture2D play_4;
  [SerializeField] private Texture2D play_5;
  [SerializeField] private Image playPauseImage;
  [SerializeField] private Image ForwardImage;
  [SerializeField] private Image ReverseImage;

  [Header("Line Renderer Settings")]
  [SerializeField] private Material lineMaterial;
  [SerializeField] private float outerBoundaryWidth = 1f;
  [SerializeField] private float innerBoundaryWidth = 1f;
  [SerializeField] private float racelineWidth = 1f;

  [Header("Track Colors")]
  [SerializeField] private Color outerBoundaryColor = Color.blue;
  [SerializeField] private Color innerBoundaryColor = Color.red;
  [SerializeField] private Color roadColor = new Color(0.0f, 0.0f, 0.0f, 1);
  [SerializeField] private Color racelineColor = Color.green;

  [Header("Track Controls")]
  [SerializeField] private bool showOuterBoundary = true;
  [SerializeField] private bool showInnerBoundary = true;
  [SerializeField] private bool showRaceLine = true;
  [SerializeField] private bool showBreakPoints = true;

  [Header("Zoom/Pan Settings")]
  [SerializeField] private float zoomSpeed = 0.1f;
  [SerializeField] private float minZoom = 0.5f;
  [SerializeField] private float maxZoom = 3f;
  [SerializeField] private float panPadding = 100f;
  [SerializeField] private float currentZoom = 1f;
  [SerializeField] private float panSpeed = 1f;
  [SerializeField] private bool enablePanZoom = true;

  [Header("Car Cursor Settings")]
  [SerializeField] private RectTransform carCursorPrefab;
  [SerializeField] private float cursorSize = 5f;
  [SerializeField] private float carSpeed = 50f;
  [SerializeField] private Color trailColor = new Color(1f, 1f, 0f, 0.8f);

  [Header("Camera Follow")]
  [SerializeField] private bool followCar = true;
  [SerializeField] private float lerpSpeed = 5f;
  [SerializeField] private bool goingToCar = true;

  [Header("Time Control")]
  [SerializeField] private float currentTime = 0f;
  [SerializeField] private float timeSpeed = 1f;
  [SerializeField] private bool isRacing = false;
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
  private bool isInitialized = false;
  private RacelineDisplayData pendingTrackData = null;

  void OnEnable()
  {
    if (!isInitialized)
    {
      StartCoroutine(InitializeComponent());
    }
    else if (pendingTrackData != null)
    {
      // If we have pending data, display it now that we're enabled
      StartCoroutine(DisplayPendingDataWithDelay());
    }
  }

  private IEnumerator InitializeComponent()
  {
    if (!zoomContainer && trackContainer) zoomContainer = trackContainer.parent as RectTransform;
    if (!viewportRect) viewportRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    initialPosition = zoomContainer.anchoredPosition;
    UpdateLineWidths();

    isInitialized = true;

    // If we have pending data, display it now
    if (pendingTrackData != null)
    {
      yield return DisplayPendingDataWithDelay();
    }

    yield return null;
  }

  private IEnumerator DisplayPendingDataWithDelay()
  {
    // Wait for end of frame to ensure everything is properly set up
    yield return new WaitForEndOfFrame();

    if (pendingTrackData != null)
    {
      DisplayRacelineData(pendingTrackData);
      pendingTrackData = null;
    }
  }

  void Update()
  {
    if (!isRacing || racelinePoints == null || racelinePoints.Length < 2) return;

    HandleCameraFollow();

    carCursor.sizeDelta = new Vector2(cursorSize, cursorSize);

    currentTime += Time.deltaTime * timeSpeed;
    float totalRaceTime = GetTotalRaceTime();

    if (timeline != null)
    {
      timeline.maxValue = totalRaceTime;
      timeline.minValue = 0f;
    }

    currentTime = Mathf.Repeat(currentTime, totalRaceTime);
    GoToTime(currentTime, totalRaceTime);

    if (timeline) timeline.value = currentTime;

    if (Input.GetKeyDown(KeyCode.Space))
    {
      ToggleFollowCar();
    }

    if (Input.GetKeyDown(KeyCode.F))
    {
      ToggleShowBreakPoints();
    }
  }

  void Start()
  {
    if (!zoomContainer && trackContainer) zoomContainer = trackContainer.parent as RectTransform;
    if (!viewportRect) viewportRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    initialPosition = zoomContainer.anchoredPosition;
    UpdateLineWidths();
  }

  private IEnumerator WaitForTrackLoadAndSettle()
  {
    while (racelinePoints == null || racelinePoints.Length < 2)
      yield return null;

    yield return null;

    ConstrainToViewport();
    UpdateZoomContainer();
    UpdateLineWidths();

    yield return new WaitForSeconds(0.5f);
    followCar = true;
    goingToCar = true;
  }

  private void OnDisable()
  {
    StopAllCoroutines();

    racelinePoints = null;
    racelineSegmentLengths = null;
    totalRacelineLength = 0f;
    currentTrackData = null;
    trailPositions.Clear();

    if (carCursor != null)
    {
      Destroy(carCursor.gameObject);
      carCursor = null;
    }

    if (trailLineRenderer != null)
    {
      Destroy(trailLineRenderer.gameObject);
      trailLineRenderer = null;
    }

    foreach (var kvp in lineRenderers)
    {
      if (kvp.Value != null)
        Destroy(kvp.Value.gameObject);
    }
    lineRenderers.Clear();

    if (trackContainer != null)
    {
      foreach (Transform child in trackContainer)
        Destroy(child.gameObject);
    }

    panOffset = Vector2.zero;
    followCar = false;
    goingToCar = false;
    currentTime = 0;
  }



  private void HandleCameraFollow()
  {
    if (!followCar || carCursor == null) return;

    Vector2 targetPos = -carCursor.anchoredPosition * currentZoom;
    if (goingToCar)
    {
      panOffset = Vector2.Lerp(panOffset, targetPos, Time.deltaTime * lerpSpeed);
      if (Vector2.Distance(panOffset, targetPos) < 0.01f)
      {
        panOffset = targetPos;
        goingToCar = false;
      }
    }
    else
    {
      panOffset = targetPos;
    }
    UpdateZoomContainer();
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

    isDragging = true;
    dragStartPosition = eventData.position;
    followCar = false;
    goingToCar = false;
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

    if (!isDragging) return;

    panOffset += (eventData.position - dragStartPosition) * panSpeed;
    dragStartPosition = eventData.position;
    ConstrainToViewport();
    UpdateZoomContainer();
  }

  public void OnScroll(PointerEventData eventData)
  {
    if (!enablePanZoom) return;
    float zoomDelta = eventData.scrollDelta.y * zoomSpeed;
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

  private void ToggleFollowCar()
  {
    followCar = !followCar;
    goingToCar = followCar;
  }

  private void ToggleShowBreakPoints()
  {
    showBreakPoints = !showBreakPoints;
    showRaceLine = !showBreakPoints;

    foreach (var kvp in lineRenderers)
    {
      if (kvp.Key.StartsWith("ReplaySegment"))
      {
        kvp.Value.enabled = showBreakPoints;
      }
      else if (kvp.Key.StartsWith("Raceline"))
      {
        kvp.Value.enabled = showRaceLine;
      }
    }
  }

  private void ConstrainToViewport()
  {
    if (!viewportRect || !trackContainer || followCar) return;

    Vector2 scaledSize = trackContainer.rect.size * currentZoom;
    Vector2 viewportSize = viewportRect.rect.size;

    Vector2 maxOffset = Vector2.Max((scaledSize - viewportSize) * 0.5f + new Vector2(panPadding, panPadding), Vector2.zero);

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
        _ when kvp.Key.StartsWith("ReplaySegment") => racelineWidth,
        _ => 1f
      };
      kvp.Value.LineThickness = baseWidth / currentZoom;
    }
  }

  List<UnityEngine.Vector2> ConvertToUnityVector2(List<System.Numerics.Vector2> list)
  {
    return list.Select(v => new UnityEngine.Vector2(v.X, v.Y)).ToList();
  }

  private List<Vector2> Downsample(List<Vector2> points, int step)
  {
    return points.Where((pt, idx) => idx % step == 0).ToList();
  }

  private List<Vector2> EnsureBelowLimit(List<Vector2> points, int limit = 64000)
  {
    if (points == null)
      return points;

    int step = 2;
    while (points.Count > limit)
    {
      points = Downsample(points, step);
    }

    return points;
  }

  public void DisplayRacelineData(RacelineDisplayData trackData, bool ACOenabled = false)
  {
    if (!isInitialized)
    {
      // Store the data and display it once initialized
      pendingTrackData = trackData;
      return;
    }

    if (!trackContainer || trackData == null) return;

    float simplificationTolerance = 5f;

    trackData = new RacelineDisplayData
    {
      InnerBoundary = LineSimplifier.SmoothLine(LineSimplifier.RamerDouglasPeucker(EnsureLooped(EnsureBelowLimit(trackData.InnerBoundary)), simplificationTolerance)),
      OuterBoundary = LineSimplifier.SmoothLine(LineSimplifier.RamerDouglasPeucker(EnsureLooped(EnsureBelowLimit(trackData.OuterBoundary)), simplificationTolerance)),
      Raceline = EnsureLooped(EnsureBelowLimit(trackData.Raceline)),
    };

    ClearExistingLines();
    currentTrackData = trackData;
    (Vector2 min, Vector2 max, Vector2 size) bounds = CalculateBounds(trackData);
    float scale = CalculateScale(bounds.size);
    Vector2 offset = CalculateOffset(bounds.size, scale);


    CreateRoadArea(trackData.OuterBoundary, trackData.InnerBoundary, bounds.min, scale, offset);

    if (ACOenabled)
    {
      ACOAgentReplay replay = gameObject.AddComponent<ACOAgentReplay>();
      replay.InitializeTextFile(Path.Combine(Application.persistentDataPath, "bestAgent.txt"));

      CreateBreakingPoints(replay.getReplays(), racelineWidth);
    }

    if (showOuterBoundary) CreateLineRenderer("OuterBoundary", trackData.OuterBoundary, outerBoundaryColor, outerBoundaryWidth, bounds.min, scale, offset);
    if (showInnerBoundary) CreateLineRenderer("InnerBoundary", trackData.InnerBoundary, innerBoundaryColor, innerBoundaryWidth, bounds.min, scale, offset);
    if (showRaceLine) CreateLineRenderer("Raceline", trackData.Raceline, racelineColor, racelineWidth, bounds.min, scale, offset);

    panOffset = Vector2.zero;
    UpdateZoomContainer();
    UpdateLineWidths();

    isRacing = true;
    currentTime = 0f;

    SetupCarCursor();
    SetupRacelineSegments();
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

  [SerializeField] private float minTrackScale = 0.2f;
  [SerializeField] private float maxTrackScale = 2.0f;

  private float CalculateScale(Vector2 size)
  {
    float margin = 50f;
    float scale = Mathf.Min(
        (trackContainer.rect.width - 2 * margin) / size.x,
        (trackContainer.rect.height - 2 * margin) / size.y
    );

    // Clamp to avoid extreme zooming
    return Mathf.Clamp(scale, minTrackScale, maxTrackScale);
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


  private void CreateBreakingPoints(List<ReplayState> replays, float width)
  {
    if (replays == null || replays.Count < 2) return;

    ACOAgentReplay replay = GetComponent<ACOAgentReplay>();
    var segments = replay.GetColoredSegments();

    if (segments == null || segments.Count == 0) return;

    (Vector2 min, Vector2 max, Vector2 size) bounds = CalculateBounds(currentTrackData);
    float scale = CalculateScale(bounds.size);
    Vector2 offset = CalculateOffset(bounds.size, scale);

    UILineRenderer currentLine = null;
    List<Vector2> currentPoints = null;
    Color currentColor = Color.clear;

    int count = 0;

    Vector2? firstPoint = null;

    foreach (var seg in segments)
    {
      if (currentLine == null || seg.color != currentColor)
      {
        if (currentLine != null)
        {
          currentLine.Points = currentPoints.ToArray();
        }

        currentLine = new GameObject($"ReplaySegment_{count}", typeof(RectTransform), typeof(UILineRenderer))
            .GetComponent<UILineRenderer>();

        currentLine.transform.SetParent(trackContainer, false);
        currentLine.material = lineMaterial;
        currentLine.color = seg.color;
        currentLine.LineThickness = width / currentZoom;

        currentPoints = new List<Vector2>();
        currentColor = seg.color;

        lineRenderers[$"ReplaySegment_{count++}"] = currentLine;
      }

      Vector2 startPoint = TransformPoint(seg.start, bounds.min, scale, offset);
      Vector2 endPoint = TransformPoint(seg.end, bounds.min, scale, offset);

      if (firstPoint == null)
      {
        firstPoint = startPoint;
      }

      currentPoints.Add(startPoint);
      currentPoints.Add(endPoint);
    }

    if (currentLine != null && currentPoints != null && currentPoints.Count >= 2 && firstPoint.HasValue)
    {
      currentPoints.Add(firstPoint.Value);
      currentLine.Points = currentPoints.ToArray();
    }
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

  private async void LoadTrackData(string trackName)
  {
    currentTrackData = null;

    try
    {
      var (success, message, bytes) = await APIManager.Instance.GetTrackBorderAsync(trackName);

      if (success && bytes != null)
      {
        RacelineDisplayData trackData = RacelineDisplayImporter.LoadFromBinaryBytes(bytes);

        if (trackData != null)
        {
          DisplayRacelineData(trackData);
          StartCoroutine(WaitForTrackLoadAndSettle());
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
    }
    catch (Exception ex)
    {
      Debug.LogError($"Exception while loading track border: {ex.Message}");
    }
  }


  private List<Vector2> EnsureLooped(List<Vector2> points)
  {
    if (points == null || points.Count < 2)
      return points;

    if (points[0] != points[points.Count - 1])
    {
      points.Add(points[0]);
    }
    return points;
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
    ReverseImage.sprite = SpriteFromTexture(play_0);
    switch (timeSpeed)
    {
      case 1f:
        timeSpeed = 2f;
        lerpSpeed = 15f;
        ForwardImage.sprite = SpriteFromTexture(play_2);
        break;
      case 2f:
        timeSpeed = 4f;
        lerpSpeed = 30f;
        ForwardImage.sprite = SpriteFromTexture(play_3);
        break;
      case 4f:
        timeSpeed = 8f;
        lerpSpeed = 60f;
        ForwardImage.sprite = SpriteFromTexture(play_4);
        break;
      case 8f:
        timeSpeed = 16f;
        lerpSpeed = 120f;
        ForwardImage.sprite = SpriteFromTexture(play_5);
        break;
      default:
        timeSpeed = 1f;
        lerpSpeed = 10f;
        ForwardImage.sprite = SpriteFromTexture(play_1);
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
      ForwardImage.sprite = SpriteFromTexture(play_0);
      ReverseImage.sprite = SpriteFromTexture(play_0);
    }
    else
    {
      timeSpeed = 1f;
      playPauseImage.sprite = SpriteFromTexture(playTexture);
      ForwardImage.sprite = SpriteFromTexture(play_1);
      ReverseImage.sprite = SpriteFromTexture(play_0);
    }
  }

  public void Rewind()
  {
    ForwardImage.sprite = SpriteFromTexture(play_0);
    switch (timeSpeed)
    {
      case -1f:
        timeSpeed = -2f;
        lerpSpeed = 15f;
        ReverseImage.sprite = SpriteFromTexture(play_2);
        break;
      case -2f:
        timeSpeed = -4f;
        lerpSpeed = 30f;
        ReverseImage.sprite = SpriteFromTexture(play_3);
        break;
      case -4f:
        timeSpeed = -8f;
        lerpSpeed = 60f;
        ReverseImage.sprite = SpriteFromTexture(play_4);
        break;
      case -8f:
        timeSpeed = -16f;
        lerpSpeed = 120f;
        ReverseImage.sprite = SpriteFromTexture(play_5);
        break;
      default:
        timeSpeed = -1f;
        lerpSpeed = 10f;
        ReverseImage.sprite = SpriteFromTexture(play_1);
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
