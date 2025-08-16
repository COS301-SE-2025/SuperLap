using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI.Extensions;

public class UIMeshPolygon : MaskableGraphic
{
  public List<Vector2> Points = new List<Vector2>();

  protected override void OnPopulateMesh(VertexHelper vh)
  {
    vh.Clear();
    if (Points == null || Points.Count < 3) return;

    List<int> indices = Triangulate(Points);

    foreach (Vector2 point in Points)
    {
      vh.AddVert(point, color, Vector2.zero);
    }

    for (int i = 0; i < indices.Count; i += 3)
    {
      vh.AddTriangle(indices[i], indices[i + 1], indices[i + 2]);
    }
  }

  private List<int> Triangulate(List<Vector2> points)
  {
    List<int> indices = new List<int>();

    if (points.Count < 3)
      return indices;

    List<int> indexList = new List<int>();
    for (int i = 0; i < points.Count; i++)
    {
      indexList.Add(i);
    }

    int totalTriangleCount = points.Count - 2;
    int triangleCount = 0;

    while (triangleCount < totalTriangleCount)
    {
      for (int i = 0; i < indexList.Count; i++)
      {
        int a = indexList[i];
        int b = indexList[(i + 1) % indexList.Count];
        int c = indexList[(i + 2) % indexList.Count];

        Vector2 va = points[a];
        Vector2 vb = points[b];
        Vector2 vc = points[c];

        Vector2 ab = vb - va;
        Vector2 ac = vc - va;

        if (ab.x * ac.y - ab.y * ac.x <= 0)
          continue;

        bool isValid = true;
        for (int j = 0; j < points.Count; j++)
        {
          if (j == a || j == b || j == c)
            continue;

          if (IsPointInTriangle(points[j], va, vb, vc))
          {
            isValid = false;
            break;
          }
        }

        if (isValid)
        {
          indices.Add(a);
          indices.Add(b);
          indices.Add(c);
          indexList.RemoveAt((i + 1) % indexList.Count);
          triangleCount++;
          break;
        }
      }
    }

    return indices;
  }

  private bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
  {
    Vector2 ab = b - a;
    Vector2 bc = c - b;
    Vector2 ca = a - c;

    Vector2 ap = p - a;
    Vector2 bp = p - b;
    Vector2 cp = p - c;

    float cross1 = ab.x * ap.y - ab.y * ap.x;
    float cross2 = bc.x * bp.y - bc.y * bp.x;
    float cross3 = ca.x * cp.y - ca.y * cp.x;

    return cross1 >= 0 && cross2 >= 0 && cross3 >= 0 ||
           cross1 <= 0 && cross2 <= 0 && cross3 <= 0;
  }
}

[System.Serializable]
public class MotoGPDisplayData
{
  public List<Vector2> OuterBoundary { get; set; }
  public List<Vector2> InnerBoundary { get; set; }
  public List<Vector2> Raceline { get; set; }
  public List<Vector2> PlayerPath { get; set; }
}

public class ShowMotoGP : MonoBehaviour, IDragHandler, IScrollHandler, IPointerDownHandler
{
  [Header("Display Settings")]
  public RectTransform trackContainer;
  public RectTransform zoomContainer;
  public RectTransform viewportRect;

  [Header("Line Renderer Settings")]
  public Material lineMaterial;
  public float outerBoundaryWidth = 5f;
  public float innerBoundaryWidth = 5f;
  public float racelineWidth = 3f;
  public float playerPathWidth = 3f;

  [Header("Track Colors")]
  public Color outerBoundaryColor = new Color(0, 0, 1, 1);        // Blue
  public Color innerBoundaryColor = new Color(1, 0, 0, 1);         // Red
  public Color roadColor = new Color(0.2f, 0.2f, 0.2f, 1);         // Dark gray
  public Color racelineColor = new Color(0, 1, 0, 1);              // Green
  public Color playerRaceLineColor = new Color(0, 0.5f, 1, 1);     // Light blue
  public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1);   // Dark gray

  [Header("Track Controls")]
  public bool showOuterBoundary = true;
  public bool showInnerBoundary = true;
  public bool showRaceLine = true;
  public bool showPlayerRaceLine = true;

  [Header("Zoom/Pan Settings")]
  public float zoomSpeed = 0.1f;
  public float minZoom = 0.5f;
  public float maxZoom = 3f;
  public float panSpeed = 1f;
  public bool invertZoom = false;

  private MotoGPDisplayData currentTrackData;
  private Dictionary<string, UILineRenderer> lineRenderers = new Dictionary<string, UILineRenderer>();

  private float currentZoom = 1f;
  private Vector2 panOffset = Vector2.zero;
  private Vector2 initialPosition;
  private bool isDragging = false;
  private Vector2 dragStartPosition;
  private Vector2 lastValidPanOffset;
  private (Vector2 min, Vector2 max, Vector2 size) bounds;

  void Start()
  {
    if (zoomContainer == null && trackContainer != null)
    {
      zoomContainer = trackContainer.parent as RectTransform;
    }

    if (viewportRect == null)
    {
      viewportRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    }

    initialPosition = zoomContainer.anchoredPosition;
    lastValidPanOffset = panOffset;
    UpdateLineWidths();

  }

  public void OnPointerDown(PointerEventData eventData)
  {
    if (currentZoom > 1f)
    {
      isDragging = true;
      dragStartPosition = eventData.position;
    }
  }

  public void OnDrag(PointerEventData eventData)
  {
    if (currentZoom > 1f && isDragging)
    {
      Vector2 delta = (eventData.position - dragStartPosition) * panSpeed;
      panOffset += delta;

      Vector2 previousPanOffset = panOffset;
      ConstrainToViewport();

      if (panOffset == previousPanOffset)
      {
        dragStartPosition = eventData.position;
      }

      UpdateZoomContainer();
    }
  }

  public void OnScroll(PointerEventData eventData)
  {
    float zoomDelta = eventData.scrollDelta.y * zoomSpeed * (invertZoom ? -1 : 1);
    float previousZoom = currentZoom;
    currentZoom = Mathf.Clamp(currentZoom + zoomDelta, minZoom, maxZoom);

    if (!Mathf.Approximately(previousZoom, currentZoom))
    {
      Vector2 localPoint;
      RectTransformUtility.ScreenPointToLocalPointInRectangle(zoomContainer,
          eventData.position, eventData.pressEventCamera, out localPoint);

      float zoomRatio = currentZoom / previousZoom;
      panOffset = (panOffset - localPoint) * zoomRatio + localPoint;
    }

    if (Mathf.Approximately(currentZoom, 1f))
    {
      panOffset = Vector2.zero;
    }

    ConstrainToViewport();
    UpdateZoomContainer();
    UpdateLineWidths();
  }

  private void ConstrainToViewport()
  {
    if (viewportRect == null || trackContainer == null) return;

    Vector2 scaledSize = trackContainer.rect.size * currentZoom;
    Vector2 viewportSize = viewportRect.rect.size;

    Vector2 maxOffset = (scaledSize - viewportSize) * 0.5f;
    maxOffset = Vector2.Max(maxOffset, Vector2.zero);

    panOffset.x = Mathf.Clamp(panOffset.x, -maxOffset.x, maxOffset.x);
    panOffset.y = Mathf.Clamp(panOffset.y, -maxOffset.y, maxOffset.y);

    if (Mathf.Approximately(currentZoom, 1f))
    {
      panOffset = Vector2.zero;
    }
  }

  private void UpdateZoomContainer()
  {
    if (zoomContainer != null)
    {
      zoomContainer.localScale = Vector3.one * currentZoom;
      zoomContainer.anchoredPosition = initialPosition + panOffset;
    }
  }

  private void UpdateLineWidths()
  {
    if (lineRenderers == null || lineRenderers.Count == 0)
      return;

    foreach (var kvp in lineRenderers.ToList())
    {
      if (kvp.Value == null)
      {
        lineRenderers.Remove(kvp.Key);
        continue;
      }

      float baseWidth = 0f;
      switch (kvp.Key)
      {
        case "OuterBoundary": baseWidth = outerBoundaryWidth; break;
        case "InnerBoundary": baseWidth = innerBoundaryWidth; break;
        case "Raceline": baseWidth = racelineWidth; break;
        case "PlayerPath": baseWidth = playerPathWidth; break;
      }

      kvp.Value.LineThickness = baseWidth / currentZoom;
    }
  }

  public void ResetView()
  {
    currentZoom = 1f;
    panOffset = Vector2.zero;
    UpdateZoomContainer();
    UpdateLineWidths();
  }

  public void DisplayPlayerLineData(CSVToBinConverter.LoadCSV.PlayerLine playerline)
  {
    if (playerline == null)
    {
      Debug.LogError("Playerline data is null");
      return;
    }

    MotoGPDisplayData trackData = new MotoGPDisplayData
    {
      OuterBoundary = playerline.OuterBoundary.Select(v => new Vector2(v.X, v.Y)).ToList(),
      InnerBoundary = playerline.InnerBoundary.Select(v => new Vector2(v.X, v.Y)).ToList(),
      Raceline = playerline.Raceline.Select(v => new Vector2(v.X, v.Y)).ToList(),
      PlayerPath = playerline.PlayerPath.Select(v => new Vector2(v.X, v.Y)).ToList()
    };

    DisplayRacelineData(trackData);
  }

  public void DisplayRacelineData(MotoGPDisplayData trackData)
  {
    if (trackData == null || trackContainer == null) return;

    ClearExistingLines();
    currentTrackData = trackData;
    bounds = CalculateBounds(currentTrackData);
    float scale = CalculateScale(bounds.size);
    Vector2 offset = CalculateOffset(bounds.size, scale);

    CreateRoadArea(trackData.OuterBoundary, trackData.InnerBoundary, bounds.min, scale, offset);

    if (showOuterBoundary)
    {
      CreateLineRenderer("OuterBoundary", trackData.OuterBoundary, outerBoundaryColor, outerBoundaryWidth, bounds.min, scale, offset);
    }
    if (showInnerBoundary)
    {
      CreateLineRenderer("InnerBoundary", trackData.InnerBoundary, innerBoundaryColor, innerBoundaryWidth, bounds.min, scale, offset);
    }
    if (showRaceLine)
    {
      CreateLineRenderer("Raceline", trackData.Raceline, racelineColor, racelineWidth, bounds.min, scale, offset);
    }
    if (showPlayerRaceLine)
    {
      CreateLineRenderer("PlayerPath", trackData.PlayerPath, playerRaceLineColor, playerPathWidth, bounds.min, scale, offset);
    }

    ResetView();
  }

  private void ClearExistingLines()
  {
    foreach (Transform child in trackContainer)
    {
      Destroy(child.gameObject);
    }
    lineRenderers.Clear();
  }

  private (Vector2 min, Vector2 max, Vector2 size) CalculateBounds(MotoGPDisplayData trackData)
  {
    Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
    Vector2 max = new Vector2(float.MinValue, float.MinValue);

    void UpdateBounds(List<Vector2> points)
    {
      if (points == null) return;
      foreach (var p in points)
      {
        min = Vector2.Min(min, p);
        max = Vector2.Max(max, p);
      }
    }

    UpdateBounds(trackData.OuterBoundary);
    UpdateBounds(trackData.InnerBoundary);
    UpdateBounds(trackData.Raceline);
    UpdateBounds(trackData.PlayerPath);

    return (min, max, max - min);
  }

  private float CalculateScale(Vector2 size)
  {
    float margin = 50f;
    float scaleX = (trackContainer.rect.width - 2 * margin) / size.x;
    float scaleY = (trackContainer.rect.height - 2 * margin) / size.y;
    return Mathf.Min(scaleX, scaleY);
  }

  private Vector2 CalculateOffset(Vector2 size, float scale)
  {
    Vector2 scaledSize = size * scale;
    return new Vector2(
        (trackContainer.rect.width - scaledSize.x) * 0.5f,
        (trackContainer.rect.height - scaledSize.y) * 0.5f
    );
  }

  private void CreateRoadArea(List<Vector2> outer, List<Vector2> inner, Vector2 min, float scale, Vector2 offset)
  {
    if (outer == null || outer.Count < 3) return;

    Debug.Log(outer.Count);
    GameObject outerObj = new GameObject("RoadAreaOuter");
    outerObj.transform.SetParent(trackContainer, false);

    RectTransform outerRT = outerObj.AddComponent<RectTransform>();
    outerRT.anchorMin = Vector2.zero;
    outerRT.anchorMax = Vector2.one;
    outerRT.sizeDelta = Vector2.zero;
    outerRT.anchoredPosition = Vector2.zero;

    CanvasRenderer outerCR = outerObj.AddComponent<CanvasRenderer>();

    UIMeshPolygon outerMesh = outerObj.AddComponent<UIMeshPolygon>();
    outerMesh.material = new Material(Shader.Find("UI/Default"));
    outerMesh.color = roadColor;

    List<Vector2> outerPoints = outer.Select(p => TransformPoint(p, min, scale, offset)).ToList();

    outerMesh.Points = outerPoints;

    if (inner != null && inner.Count >= 3)
    {
      GameObject maskObj = new GameObject("InnerMask");
      maskObj.transform.SetParent(outerObj.transform, false);

      RectTransform maskRT = maskObj.AddComponent<RectTransform>();
      maskRT.anchorMin = Vector2.zero;
      maskRT.anchorMax = Vector2.one;
      maskRT.sizeDelta = Vector2.zero;
      maskRT.anchoredPosition = Vector2.zero;

      CanvasRenderer innerCR = maskObj.AddComponent<CanvasRenderer>();

      Mask mask = maskObj.AddComponent<Mask>();
      mask.showMaskGraphic = false;

      UIMeshPolygon innerMesh = maskObj.AddComponent<UIMeshPolygon>();
      innerMesh.material = new Material(Shader.Find("UI/Default"));
      innerMesh.color = backgroundColor;

      List<Vector2> innerPoints = inner.Select(p => TransformPoint(p, min, scale, offset)).ToList();
      innerMesh.Points = innerPoints;
    }
  }

  private void CreateLineRenderer(string name, List<Vector2> points, Color color, float width, Vector2 min, float scale, Vector2 offset)
  {
    if (points == null || points.Count < 2) return;

    GameObject lineObj = new GameObject(name);
    lineObj.transform.SetParent(trackContainer, false);

    UILineRenderer lineRenderer = lineObj.AddComponent<UILineRenderer>();
    lineRenderer.material = new Material(Shader.Find("UI/Default"));
    lineRenderer.color = color;
    lineRenderer.LineThickness = width;
    lineRenderer.RelativeSize = false;
    lineRenderer.drivenExternally = false;

    Vector2[] linePoints = points
        .Select(p => TransformPoint(p, min, scale, offset))
        .ToArray();

    lineRenderer.Points = linePoints;

    RectTransform rt = lineObj.GetComponent<RectTransform>();
    if (rt == null) rt = lineObj.AddComponent<RectTransform>();
    rt.anchorMin = Vector2.zero;
    rt.anchorMax = Vector2.one;
    rt.sizeDelta = Vector2.zero;
    rt.anchoredPosition = Vector2.zero;

    lineRenderers[name] = lineRenderer;
  }

  private Vector2 TransformPoint(Vector2 point, Vector2 min, float scale, Vector2 offset)
  {
    Vector2 transformed = (point - min) * scale + offset;
    transformed.y = trackContainer.rect.height - transformed.y;
    return transformed - trackContainer.rect.size * 0.5f;
  }
}