using UnityEngine;
using UnityEngine.UI;

public class ShowData : MonoBehaviour
{
  [Header("Graph Panel")]
  [SerializeField] private Image graphPanelImage; // Assign your UI Image here
  //[SerializeField] private RectTransform graphPanel;
  [SerializeField] private int textureWidth = 400;
  [SerializeField] private int textureHeight = 300;

  [Header("Graph Settings")]
  [SerializeField, Range(1, 10)] private int pointWidth = 4;
  [SerializeField] private Color graphColor = Color.blue;
  [SerializeField] private Color backgroundColor = Color.white;

  [Header("Padding (pixels)")]
  [SerializeField] private int paddingLeft = 10;
  [SerializeField] private int paddingRight = 10;
  [SerializeField] private int paddingTop = 10;
  [SerializeField] private int paddingBottom = 10;

  [Header("Supersampling")]
  [SerializeField, Range(1, 4)] private int superSampleFactor = 2;

  private Texture2D graphTexture;         // Final display texture
  private Texture2D superSampleTexture;   // High-res drawing texture
  private int superWidth;
  private int superHeight;

  private APIManager apiManager;

  private void Awake()
  {
    // Initialize sizes for supersampling
    superWidth = textureWidth * superSampleFactor;
    superHeight = textureHeight * superSampleFactor;

    // Create textures
    graphTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
    graphTexture.filterMode = FilterMode.Bilinear;

    superSampleTexture = new Texture2D(superWidth, superHeight, TextureFormat.RGBA32, false);
    superSampleTexture.filterMode = FilterMode.Bilinear;

    ClearTexture(graphTexture, backgroundColor);
    ClearTexture(superSampleTexture, backgroundColor);

    // Set initial sprite
    //graphPanelImage.sprite = Sprite.Create(graphTexture, new Rect(0, 0, textureWidth, textureHeight), new Vector2(0.5f, 0.5f));
    //graphPanel.GetComponent<Image>().color = backgroundColor;

    UpdateGraphData(APIManager.Instance.GetDataPoints());
  }

  public void UpdateGraphData(Vector2[] dataPoints)
  {
    if (dataPoints == null || dataPoints.Length == 0) return;

    ClearTexture(superSampleTexture, backgroundColor);

    // Find min and max for scaling
    float minX = dataPoints[0].x;
    float maxX = dataPoints[0].x;
    float minY = dataPoints[0].y;
    float maxY = dataPoints[0].y;

    foreach (var pt in dataPoints)
    {
      if (pt.x < minX) minX = pt.x;
      if (pt.x > maxX) maxX = pt.x;
      if (pt.y < minY) minY = pt.y;
      if (pt.y > maxY) maxY = pt.y;
    }

    // Prevent zero range (avoid divide by zero)
    float rangeX = Mathf.Max(0.0001f, maxX - minX);
    float rangeY = Mathf.Max(0.0001f, maxY - minY);

    // Calculate usable drawing area (supersampled)
    float usableWidth = superWidth - paddingLeft * superSampleFactor - paddingRight * superSampleFactor;
    float usableHeight = superHeight - paddingTop * superSampleFactor - paddingBottom * superSampleFactor;

    Vector2? lastPixelPos = null;

    for (int i = 0; i < dataPoints.Length; i++)
    {
      // Normalize data points [0..1]
      float xNorm = (dataPoints[i].x - minX) / rangeX;
      float yNorm = (dataPoints[i].y - minY) / rangeY;

      // Scale to pixel positions inside padding
      float xPixel = paddingLeft * superSampleFactor + xNorm * usableWidth;
      float yPixel = paddingBottom * superSampleFactor + yNorm * usableHeight;

      Vector2 currentPos = new Vector2(xPixel, yPixel);

      // Draw thick point
      DrawCircle(superSampleTexture, Mathf.RoundToInt(xPixel), Mathf.RoundToInt(yPixel), pointWidth * superSampleFactor, graphColor);

      // Draw line from last point to current point
      if (lastPixelPos.HasValue)
      {
        DrawLine(superSampleTexture, lastPixelPos.Value, currentPos, pointWidth * superSampleFactor, graphColor);
      }

      lastPixelPos = currentPos;
    }

    // Downscale supersampled texture into final display texture
    DownscaleTexture(superSampleTexture, graphTexture);

    graphTexture.Apply();

    // Update the UI Image sprite
    graphPanelImage.sprite = Sprite.Create(graphTexture, new Rect(0, 0, textureWidth, textureHeight), new Vector2(0.5f, 0.5f));
  }

  private void ClearTexture(Texture2D tex, Color clearColor)
  {
    Color[] clearColors = new Color[tex.width * tex.height];
    for (int i = 0; i < clearColors.Length; i++)
    {
      clearColors[i] = clearColor;
    }
    tex.SetPixels(clearColors);
    tex.Apply();
  }

  // Draw filled circle with simple alpha blending for smooth edges
  private void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color color)
  {
    int rSquared = radius * radius;

    for (int x = -radius; x <= radius; x++)
    {
      for (int y = -radius; y <= radius; y++)
      {
        int px = cx + x;
        int py = cy + y;
        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
        {
          float distSq = x * x + y * y;
          if (distSq <= rSquared)
          {
            float dist = Mathf.Sqrt(distSq);
            // Blend edges (fade alpha near border)
            float alpha = Mathf.Clamp01(1f - (dist / radius));
            Color existingColor = tex.GetPixel(px, py);
            Color blended = Color.Lerp(existingColor, color, alpha * color.a);
            tex.SetPixel(px, py, blended);
          }
        }
      }
    }
  }

  // Bresenham’s line algorithm with thickness (circle brush along the line)
  private void DrawLine(Texture2D tex, Vector2 start, Vector2 end, int thickness, Color color)
  {
    int x0 = Mathf.RoundToInt(start.x);
    int y0 = Mathf.RoundToInt(start.y);
    int x1 = Mathf.RoundToInt(end.x);
    int y1 = Mathf.RoundToInt(end.y);

    int dx = Mathf.Abs(x1 - x0);
    int dy = Mathf.Abs(y1 - y0);

    int sx = x0 < x1 ? 1 : -1;
    int sy = y0 < y1 ? 1 : -1;

    int err = dx - dy;

    while (true)
    {
      DrawCircle(tex, x0, y0, thickness, color);

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

  // Downscale supersampled texture into lower-res texture with simple box filter averaging
  private void DownscaleTexture(Texture2D src, Texture2D dest)
  {
    int factor = superSampleFactor;

    for (int y = 0; y < dest.height; y++)
    {
      for (int x = 0; x < dest.width; x++)
      {
        Color avgColor = Color.clear;
        int pxCount = 0;

        for (int sy = 0; sy < factor; sy++)
        {
          for (int sx = 0; sx < factor; sx++)
          {
            int px = x * factor + sx;
            int py = y * factor + sy;
            avgColor += src.GetPixel(px, py);
            pxCount++;
          }
        }

        avgColor /= pxCount;
        dest.SetPixel(x, y, avgColor);
      }
    }
  }
}
