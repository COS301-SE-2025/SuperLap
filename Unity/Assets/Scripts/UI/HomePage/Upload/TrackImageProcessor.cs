using UnityEngine;
using UnityEngine.UI;
using SFB;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RacelineOptimizer;
using RainbowArt.CleanFlatUI;
public class TrackImageProcessor : MonoBehaviour
{


  [Header("UI References")]
  [SerializeField] private Image previewImage;

  [Header("Progress Bar")]
  [SerializeField] public ProgressBar progressBar;

  [Header("Upload Settings")]
  [SerializeField]
  private ExtensionFilter[] extensionFilters = new ExtensionFilter[]
  {
        new ExtensionFilter("Image Files", "png", "jpg", "jpeg")
  };

  [Header("Processing Settings")]
  [SerializeField] private int particleCount = 100;
  [SerializeField] private int maxIterations = 6000;

  [Header("Output Settings")]
  [SerializeField] private Image outputImage;
  [SerializeField] private int outputImageWidth = 1024;
  [SerializeField] private int outputImageHeight = 1024;
  [SerializeField] private Color innerBoundaryColor = Color.red;
  [SerializeField] private Color outerBoundaryColor = Color.blue;
  [SerializeField] private Color racelineColor = Color.green;
  [SerializeField] private int lineThickness = 3;

  // Results data

  private HomePageNavigation homePageNavigation;
  public class ProcessingResults
  {
    public bool success;
    public string errorMessage;
    public List<Vector2> innerBoundary;
    public List<Vector2> outerBoundary;
    public List<Vector2> raceline;
    public float processingTime;
  }

  private string selectedImagePath;
  private Texture2D loadedTexture;
  private ProcessingResults lastResults;
  private Texture2D outputTexture;

  // Events for UI updates
  public System.Action<ProcessingResults> OnProcessingComplete;
  public System.Action<string> OnProcessingStarted;
  public System.Action<string> OnImageLoaded;

  private void Start()
  {
    if (previewImage != null)
    {
      previewImage.gameObject.SetActive(false);
    }

    homePageNavigation = FindAnyObjectByType<HomePageNavigation>();
    progressBar.gameObject.SetActive(false);
  }

  public void OpenImageDialog()
  {
    var paths = StandaloneFileBrowser.OpenFilePanel("Select Track Image", "", extensionFilters, false);

    if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
    {
      selectedImagePath = paths[0];
      StartCoroutine(LoadImage(selectedImagePath));
    }
  }

  private IEnumerator LoadImage(string imagePath)
  {
    if (!File.Exists(imagePath))
    {
      Debug.LogError("Selected image file does not exist: " + imagePath);
      yield break;
    }

    byte[] imageData = File.ReadAllBytes(imagePath);

    if (loadedTexture != null)
    {
      Destroy(loadedTexture);
    }

    loadedTexture = new Texture2D(2, 2);
    bool imageLoaded = loadedTexture.LoadImage(imageData);

    if (imageLoaded && previewImage != null)
    {
      Sprite imageSprite = Sprite.Create(
          loadedTexture,
          new Rect(0, 0, loadedTexture.width, loadedTexture.height),
          new Vector2(0.5f, 0.5f)
      );

      previewImage.sprite = imageSprite;
      previewImage.gameObject.SetActive(true);

      string fileName = Path.GetFileName(imagePath);
      Debug.Log($"Image loaded successfully: {fileName} ({loadedTexture.width}x{loadedTexture.height})");
      OnImageLoaded?.Invoke($"Image loaded: {fileName}");
    }
    else
    {
      Debug.LogError("Failed to load image: " + imagePath);
    }

    yield return null;
  }

  public void ProcessTrackImage()
  {
    if (string.IsNullOrEmpty(selectedImagePath))
    {
      Debug.LogError("No image selected for processing");
      return;
    }

    StartCoroutine(ProcessTrackImageCoroutine());

  }

  private IEnumerator ProcessTrackImageCoroutine()
  {
    float startTime = Time.realtimeSinceStartup;

    Debug.Log("Starting track image processing...");
    OnProcessingStarted?.Invoke("Processing track image...");

    progressBar.CurrentValue = 0;
    progressBar.gameObject.SetActive(true);

    ImageProcessing.TrackBoundaries boundaries = ImageProcessing.ProcessImage(selectedImagePath);

    for (int i = 0; i <= 90; i += 10)
    {
      progressBar.CurrentValue = i;
      yield return new WaitForSeconds(0.1f);
    }
    for (int i = 90; i <= 99; i += 1)
    {
      progressBar.CurrentValue = i;
      yield return new WaitForSeconds(0.1f);
    }

    if (!boundaries.success)
    {
      string errorMsg = $"Image processing failed: {boundaries.errorMessage}";
      Debug.LogError(errorMsg);

      lastResults = new ProcessingResults
      {
        success = false,
        errorMessage = boundaries.errorMessage,
        processingTime = Time.realtimeSinceStartup - startTime
      };

      OnProcessingComplete?.Invoke(lastResults);
      yield break;
    }

    Debug.Log($"Image processing successful. Running PSO optimization...");
    OnProcessingStarted?.Invoke("Optimizing raceline...");

    // Run PSO optimization
    PSOInterface.RacelineResult racelineResult = PSOIntegrator.RunPSO(
        boundaries.innerBoundary,
        boundaries.outerBoundary,
        particleCount,
        maxIterations
    );

    yield return null; // Allow UI to update

    // Convert System.Numerics.Vector2 to UnityEngine.Vector2
    List<Vector2> innerBoundary = ConvertToUnityVectors(racelineResult.InnerBoundary);
    List<Vector2> outerBoundary = ConvertToUnityVectors(racelineResult.OuterBoundary);
    List<Vector2> raceline = ConvertToUnityVectors(racelineResult.Raceline);

    float processingTime = Time.realtimeSinceStartup - startTime;

    // Store results
    lastResults = new ProcessingResults
    {
      success = true,
      errorMessage = "",
      innerBoundary = innerBoundary,
      outerBoundary = outerBoundary,
      raceline = raceline,
      processingTime = processingTime
    };

    Debug.Log($"Track processing completed successfully in {processingTime:F2} seconds.");

    // Generate output image
    GenerateOutputImage();

    // Navigate to racing line page with processed data
    NavigateToRacingLineWithProcessedData();
    progressBar.gameObject.SetActive(false);
    OnProcessingComplete?.Invoke(lastResults);
  }

  // Helper method to convert System.Numerics.Vector2 to UnityEngine.Vector2
  private List<Vector2> ConvertToUnityVectors(List<System.Numerics.Vector2> numericsVectors)
  {
    if (numericsVectors == null)
      return new List<Vector2>();

    return numericsVectors.Select(v => new Vector2(v.X, v.Y)).ToList();
  }

  // Convert ProcessingResults to RacelineDisplayData for ShowRacingLine
  private RacelineDisplayData ConvertToRacelineDisplayData(ProcessingResults results)
  {
    if (results == null || !results.success)
      return null;

    var racelineData = new RacelineDisplayData();
    racelineData.OuterBoundary = results.outerBoundary ?? new List<Vector2>();
    racelineData.InnerBoundary = results.innerBoundary ?? new List<Vector2>();
    racelineData.Raceline = results.raceline ?? new List<Vector2>();

    return racelineData;
  }

  // Navigate to racing line page with the processed track data
  private void NavigateToRacingLineWithProcessedData()
  {
    if (homePageNavigation == null)
    {
      Debug.LogWarning("HomePageNavigation reference is null. Cannot navigate to racing line page.");
      return;
    }

    if (lastResults == null || !lastResults.success)
    {
      Debug.LogWarning("No valid processing results to send to racing line page.");
      return;
    }

    // Convert processing results to racing line display data
    RacelineDisplayData racelineData = ConvertToRacelineDisplayData(lastResults);

    if (racelineData == null)
    {
      Debug.LogError("Failed to convert processing results to racing line display data.");
      return;
    }

    // Navigate to racing line page
    homePageNavigation.NavigateToRacingLine();

    // Get the racing line component and send the data
    if (homePageNavigation.racingLinePage != null)
    {
      ShowRacingLine racingLineComponent = homePageNavigation.racingLinePage.GetComponentInChildren<ShowRacingLine>();
      if (racingLineComponent != null)
      {
        // Generate a track name from the selected image
        string trackName = GenerateTrackNameFromImage();

        // Send the processed data to the racing line display
        racingLineComponent.DisplayRacelineData(racelineData, trackName);

        Debug.Log($"Successfully sent processed track data to racing line page. Track: {trackName}");
      }
      else
      {
        Debug.LogError("ShowRacingLine component not found in racing line page.");
      }
    }
    else
    {
      Debug.LogError("Racing line page reference is null in HomePageNavigation.");
    }
  }

  // Generate a track name from the selected image file
  private string GenerateTrackNameFromImage()
  {
    if (string.IsNullOrEmpty(selectedImagePath))
      return "Processed Track";

    string fileName = Path.GetFileNameWithoutExtension(selectedImagePath);

    // Clean up the filename to make it a nice track name
    string trackName = fileName.Replace("_", " ").Replace("-", " ");

    // Capitalize first letter of each word
    string[] words = trackName.Split(' ');
    for (int i = 0; i < words.Length; i++)
    {
      if (!string.IsNullOrEmpty(words[i]))
      {
        words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
      }
    }

    return string.Join(" ", words);
  }

  // Public getters for accessing results
  public ProcessingResults GetLastResults()
  {
    return lastResults;
  }

  public string GetSelectedImagePath()
  {
    return selectedImagePath;
  }

  public Texture2D GetLoadedTexture()
  {
    return loadedTexture;
  }

  public bool HasValidResults()
  {
    return lastResults != null && lastResults.success;
  }

  // Public method to manually navigate to racing line with current results
  public void ViewRacingLine()
  {
    if (HasValidResults())
    {
      NavigateToRacingLineWithProcessedData();
    }
    else
    {
      Debug.LogWarning("No valid results available to view racing line.");
    }
  }

  public void ClearResults()
  {
    lastResults = null;

    if (outputTexture != null)
    {
      Destroy(outputTexture);
      outputTexture = null;
    }

    if (outputImage != null)
    {
      outputImage.gameObject.SetActive(false);
    }
  }

  private void GenerateOutputImage()
  {
    if (lastResults == null || !lastResults.success)
      return;

    // Clean up previous output texture
    if (outputTexture != null)
    {
      Destroy(outputTexture);
    }

    // Create new texture
    outputTexture = new Texture2D(outputImageWidth, outputImageHeight);

    // Clear to white background
    Color[] pixels = new Color[outputImageWidth * outputImageHeight];
    for (int i = 0; i < pixels.Length; i++)
    {
      pixels[i] = Color.white;
    }
    outputTexture.SetPixels(pixels);

    // Calculate bounds for scaling
    var allPoints = new List<Vector2>();
    allPoints.AddRange(lastResults.innerBoundary);
    allPoints.AddRange(lastResults.outerBoundary);
    allPoints.AddRange(lastResults.raceline);

    if (allPoints.Count == 0)
      return;

    float minX = allPoints.Min(p => p.x);
    float maxX = allPoints.Max(p => p.x);
    float minY = allPoints.Min(p => p.y);
    float maxY = allPoints.Max(p => p.y);

    float margin = 50f; // Pixel margin
    float scaleX = (outputImageWidth - 2 * margin) / (maxX - minX);
    float scaleY = (outputImageHeight - 2 * margin) / (maxY - minY);
    float scale = Mathf.Min(scaleX, scaleY);

    // Draw lines
    DrawLineOnTexture(lastResults.innerBoundary, innerBoundaryColor, scale, minX, minY, margin);
    DrawLineOnTexture(lastResults.outerBoundary, outerBoundaryColor, scale, minX, minY, margin);
    DrawLineOnTexture(lastResults.raceline, racelineColor, scale, minX, minY, margin);

    // Apply texture changes
    outputTexture.Apply();

    // Create sprite and assign to output image
    if (outputImage != null)
    {
      Sprite outputSprite = Sprite.Create(
          outputTexture,
          new Rect(0, 0, outputImageWidth, outputImageHeight),
          new Vector2(0.5f, 0.5f)
      );

      outputImage.sprite = outputSprite;
      outputImage.gameObject.SetActive(true);
    }

    Debug.Log("Output image generated successfully.");
  }

  private void DrawLineOnTexture(List<Vector2> points, Color color, float scale, float minX, float minY, float margin)
  {
    if (points == null || points.Count < 2)
      return;

    for (int i = 0; i < points.Count - 1; i++)
    {
      Vector2 start = points[i];
      Vector2 end = points[i + 1];

      // Convert world coordinates to texture coordinates
      int x1 = Mathf.RoundToInt((start.x - minX) * scale + margin);
      int y1 = Mathf.RoundToInt((start.y - minY) * scale + margin);
      int x2 = Mathf.RoundToInt((end.x - minX) * scale + margin);
      int y2 = Mathf.RoundToInt((end.y - minY) * scale + margin);

      DrawLinePixels(x1, y1, x2, y2, color);
    }

    // Connect last point to first for closed loop
    if (points.Count > 2)
    {
      Vector2 start = points[points.Count - 1];
      Vector2 end = points[0];

      int x1 = Mathf.RoundToInt((start.x - minX) * scale + margin);
      int y1 = Mathf.RoundToInt((start.y - minY) * scale + margin);
      int x2 = Mathf.RoundToInt((end.x - minX) * scale + margin);
      int y2 = Mathf.RoundToInt((end.y - minY) * scale + margin);

      DrawLinePixels(x1, y1, x2, y2, color);
    }
  }

  private void DrawLinePixels(int x1, int y1, int x2, int y2, Color color)
  {
    // Bresenham's line algorithm with thickness
    int dx = Mathf.Abs(x2 - x1);
    int dy = Mathf.Abs(y2 - y1);
    int sx = x1 < x2 ? 1 : -1;
    int sy = y1 < y2 ? 1 : -1;
    int err = dx - dy;

    int x = x1;
    int y = y1;

    while (true)
    {
      // Draw thick line by drawing multiple pixels around the center
      for (int offsetX = -lineThickness / 2; offsetX <= lineThickness / 2; offsetX++)
      {
        for (int offsetY = -lineThickness / 2; offsetY <= lineThickness / 2; offsetY++)
        {
          int pixelX = x + offsetX;
          int pixelY = y + offsetY;

          if (pixelX >= 0 && pixelX < outputImageWidth &&
              pixelY >= 0 && pixelY < outputImageHeight)
          {
            outputTexture.SetPixel(pixelX, pixelY, color);
          }
        }
      }

      if (x == x2 && y == y2) break;

      int e2 = 2 * err;
      if (e2 > -dy)
      {
        err -= dy;
        x += sx;
      }
      if (e2 < dx)
      {
        err += dx;
        y += sy;
      }
    }
  }

  public Texture2D GetOutputTexture()
  {
    return outputTexture;
  }

  private void OnDestroy()
  {
    if (loadedTexture != null)
    {
      Destroy(loadedTexture);
    }

    if (outputTexture != null)
    {
      Destroy(outputTexture);
    }
  }
}