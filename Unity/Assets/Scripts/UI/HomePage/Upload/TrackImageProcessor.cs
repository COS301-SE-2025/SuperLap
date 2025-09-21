using UnityEngine;
using UnityEngine.UI;
using SFB;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RacelineOptimizer;
using System;
using UnityEngine.EventSystems;
using TMPro;
using System.Threading.Tasks;

public class TrackImageProcessor : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
  [Header("UI References")]
  [SerializeField] private Image previewImage;
  [SerializeField] private Button traceButton;
  [SerializeField] private Button resetTraceButton;
  [SerializeField] private Button processButton;
  [SerializeField] private Slider maskWidthSlider;
  [SerializeField] private TextMeshProUGUI maskWidthLabel;
  [SerializeField] private GameObject errorPopUp;
  [SerializeField] private GameObject LoaderPanel;
  [SerializeField] private ACOTrainer trainer;


  [Header("Upload Settings")]
  [SerializeField]
  private ExtensionFilter[] extensionFilters = new ExtensionFilter[]
  {
        new ExtensionFilter("Image Files", "png", "jpg", "jpeg")
  };

  [Header("Processing Settings")]
  [SerializeField] private int particleCount = 100;
  [SerializeField] private int maxIterations = 6000;

  [Header("Centerline Tracing Settings")]
  [SerializeField] private int maskWidth = 50;
  [SerializeField] private Color centerlineColor = Color.green;
  [SerializeField] private Color startPositionColor = Color.red;
  [SerializeField] private int centerlineThickness = 3;
  [SerializeField] private float minPointDistance = 5f;

  [Header("Output Settings")]
  [SerializeField] private Image outputImage;
  [SerializeField] private int outputImageWidth = 1024;
  [SerializeField] private int outputImageHeight = 1024;
  [SerializeField] private Color innerBoundaryColor = Color.red;
  [SerializeField] private Color outerBoundaryColor = Color.blue;
  [SerializeField] private Color racelineColor = Color.green;
  [SerializeField] private int lineThickness = 3;
  [SerializeField] private int pointCount = 100;
  [SerializeField] private GameObject meshHolder;


  //Centerline tracing state
  private bool isTracingMode = false;
  private bool isDrawing = false;
  private List<Vector2> centerlinePoints = new List<Vector2>();
  private Vector2? startPosition = null;
  private float raceDirection = 0f;
  private Texture2D centerlineOverlay;
  private RectTransform previewImageRect;

  // Processing state
  private bool isProcessing = false;

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
    public List<Vector2> centerlinePoints;
    public Vector2? startPosition;
    public float raceDirection;
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
      previewImageRect = previewImage.GetComponent<RectTransform>();
      previewImage.gameObject.SetActive(false);
    }

    homePageNavigation = FindAnyObjectByType<HomePageNavigation>();

    SetupUI();
    SetTracingMode(false);
  }

  private void SetupUI()
  {
    //Trace button
    if (traceButton != null)
    {
      traceButton.gameObject.SetActive(false);
    }
    //Reset button
    if (resetTraceButton != null)
    {
      resetTraceButton.gameObject.SetActive(false);
    }
    //Process button
    if (processButton != null)
    {
      processButton.gameObject.SetActive(false);
    }
    //Setup mask width slider
    if (maskWidthSlider != null)
    {
      maskWidthSlider.value = maskWidth;
      maskWidthSlider.gameObject.SetActive(false);
    }

    if (maskWidthLabel != null)
    {
      maskWidthLabel.gameObject.SetActive(false);
    }

    if (LoaderPanel != null)
    {
      LoaderPanel.SetActive(false);
    }
    if (errorPopUp != null)
    {
      errorPopUp.SetActive(false);
    }
  }

  private void OnEnable()
  {
    ResetPage();
    SetupUI();
    SetTracingMode(false);
  }


  private void ResetPage()
  {
    // Clear data
    selectedImagePath = null;
    lastResults = null;
    startPosition = null;
    raceDirection = 0f;
    centerlinePoints.Clear();
    isProcessing = false;

    // Destroy textures
    if (loadedTexture != null) Destroy(loadedTexture);
    if (centerlineOverlay != null) Destroy(centerlineOverlay);
    if (outputTexture != null) Destroy(outputTexture);

    loadedTexture = null;
    centerlineOverlay = null;
    outputTexture = null;

    // Reset UI visibility
    if (previewImage != null)
    {
      previewImage.sprite = null;
      previewImage.gameObject.SetActive(false);
    }
    if (traceButton != null) traceButton.gameObject.SetActive(false);
    if (resetTraceButton != null) resetTraceButton.gameObject.SetActive(false);
    if (processButton != null) processButton.gameObject.SetActive(false);
    if (maskWidthSlider != null) maskWidthSlider.gameObject.SetActive(false);
    if (maskWidthLabel != null) maskWidthLabel.gameObject.SetActive(false);
    if (outputImage != null) outputImage.gameObject.SetActive(false);
    if (errorPopUp != null) errorPopUp.SetActive(false);
    if (LoaderPanel != null) LoaderPanel.SetActive(false);

    // Reset slider value
    if (maskWidthSlider != null) maskWidthSlider.value = maskWidth;

    Debug.Log("TrackImageProcessor reset to default state.");
  }


  public void OnMaskWidthChanged(float value)
  {
    maskWidth = Mathf.RoundToInt(value);
    centerlineThickness = maskWidth;
    ResetCenterline();
  }

  public void ToggleTracingMode()
  {
    SetTracingMode(!isTracingMode);
    ResetCenterline();
  }

  private void SetTracingMode(bool enabled)
  {
    isTracingMode = enabled;
    if (traceButton != null)
    {
      traceButton.GetComponentInChildren<TextMeshProUGUI>().text = isTracingMode ? "Stop Tracing" : "Trace Centerline";
    }
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    if (!isTracingMode || loadedTexture == null) return;

    Vector2 localPoint;
    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(previewImageRect, eventData.position, eventData.pressEventCamera, out localPoint))
    {
      Vector2 normalisedPoint = GetNormalisedImagePoint(localPoint);
      Vector2 imagePoint = new Vector2(normalisedPoint.x * loadedTexture.width, normalisedPoint.y * loadedTexture.height);

      isDrawing = true;
      centerlinePoints.Clear();
      centerlinePoints.Add(imagePoint);
      startPosition = imagePoint;

      Debug.Log($"Started centerline at: ({imagePoint.x:F1}, {imagePoint.y:F1})");
      UpdateCenterlineOverlay();
    }
  }


  public void OnDrag(PointerEventData eventData)
  {
    if (!isTracingMode || !isDrawing || loadedTexture == null) return;

    Vector2 localPoint;
    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(previewImageRect, eventData.position, eventData.pressEventCamera, out localPoint))
    {
      Vector2 normalisedPoint = GetNormalisedImagePoint(localPoint);
      Vector2 imagePoint = new Vector2(normalisedPoint.x * loadedTexture.width, normalisedPoint.y * loadedTexture.height);

      if (centerlinePoints.Count == 0 || Vector2.Distance(imagePoint, centerlinePoints[centerlinePoints.Count - 1]) > minPointDistance)
      {
        centerlinePoints.Add(imagePoint);

        //Calculate race direction
        if (centerlinePoints.Count >= 10)
        {
          CalculateRaceDirection();
        }

        UpdateCenterlineOverlay();
      }
    }
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    if (!isTracingMode || !isDrawing) return;

    isDrawing = false;

    // If we're close to the start point, snap to it to close the loop
    if (centerlinePoints.Count >= 5 && startPosition.HasValue)
    {
      float distanceToStart = Vector2.Distance(centerlinePoints[centerlinePoints.Count - 1], startPosition.Value);
      if (distanceToStart < maskWidth * 2f) // If close to start, complete the loop
      {
        centerlinePoints.Add(startPosition.Value);
      }

      CalculateRaceDirection();
      if (processButton != null)
      {
        processButton.gameObject.SetActive(true);
      }
      Debug.Log($"Centerline completed with {centerlinePoints.Count} points");
      Debug.Log($"Race direction: {raceDirection:F1} degrees ({GetCompassDirection(raceDirection)})");
    }

    SetTracingMode(false);
  }

  private void CalculateRaceDirection()
  {
    if (centerlinePoints.Count < 5) return;

    int endInx = Mathf.Min(10, centerlinePoints.Count);
    Vector2 startPoint = centerlinePoints[0];
    Vector2 endPoint = centerlinePoints[endInx - 1];

    Vector2 dir = endPoint - startPoint;
    raceDirection = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

    //normalise to 0-360
    if (raceDirection < 0)
    {
      raceDirection += 360f;
    }
  }

  private string GetCompassDirection(float angle)
  {
    string[] directions = {
      "East", "Northeast", "North", "Northwest",
      "West", "Southwest", "South", "Southeast"
    };

    int idx = Mathf.RoundToInt(angle / 45f) % 8;
    return directions[idx];
  }
  private void UpdateCenterlineOverlay()
  {
    if (loadedTexture == null || centerlinePoints.Count == 0) return;

    if (centerlineOverlay == null ||
        centerlineOverlay.width != loadedTexture.width ||
        centerlineOverlay.height != loadedTexture.height)
    {
      if (centerlineOverlay != null) Destroy(centerlineOverlay);
      centerlineOverlay = new Texture2D(loadedTexture.width, loadedTexture.height);
      centerlineOverlay.SetPixels(loadedTexture.GetPixels());
      centerlineOverlay.Apply();
    }

    if (centerlinePoints.Count >= 2)
    {
      Vector2 start = centerlinePoints[centerlinePoints.Count - 2];
      Vector2 end = centerlinePoints[centerlinePoints.Count - 1];
      DrawLineOnTexture(centerlineOverlay, start, end, centerlineColor);
    }

    if (startPosition.HasValue)
    {
      DrawCircleOnTexture(centerlineOverlay, startPosition.Value, 8, startPositionColor);
    }

    centerlineOverlay.Apply();

    Sprite overlaySprite = Sprite.Create(centerlineOverlay, new Rect(0, 0, centerlineOverlay.width, centerlineOverlay.height), new Vector2(0.5f, 0.5f));
    if (previewImage.sprite != null) previewImage.sprite = null;
    previewImage.sprite = overlaySprite;
  }

  private void DrawCircleOnTexture(Texture2D texture, Vector2 center, int radius, Color color)
  {
    int cx = Mathf.RoundToInt(center.x);
    int cy = Mathf.RoundToInt(center.y);

    for (int x = -radius; x <= radius; x++)
    {
      for (int y = -radius; y <= radius; y++)
      {
        if (x * x + y * y <= radius * radius)
        {
          int pixelX = cx + x;
          int pixelY = cy + y;

          if (pixelX >= 0 && pixelX < texture.width && pixelY >= 0 && pixelY < texture.height)
          {
            texture.SetPixel(pixelX, pixelY, color);
          }
        }
      }
    }
  }
  private void DrawLineOnTexture(Texture2D texture, Vector2 start, Vector2 end, Color color)
  {
    int x1 = Mathf.RoundToInt(start.x);
    int y1 = Mathf.RoundToInt(start.y);
    int x2 = Mathf.RoundToInt(end.x);
    int y2 = Mathf.RoundToInt(end.y);

    int thickness = centerlineThickness;
    int radius = centerlineThickness / 2;
    int halfThickness = thickness / 2;
    int textureWidth = texture.width;
    int textureHeight = texture.height;

    int dx = Mathf.Abs(x2 - x1);
    int dy = Mathf.Abs(y2 - y1);
    int sx = x1 < x2 ? 1 : -1;
    int sy = y1 < y2 ? 1 : -1;
    int err = dx - dy;

    int x = x1;
    int y = y1;

    while (true)
    {
      for (int offsetX = -halfThickness; offsetX <= halfThickness; offsetX++)
      {
        for (int offsetY = -halfThickness; offsetY <= halfThickness; offsetY++)
        {
          if (offsetX * offsetX + offsetY * offsetY <= radius * radius)
          {
            int pixelX = x + offsetX;
            int pixelY = y + offsetY;

            if (pixelX >= 0 && pixelX < textureWidth && pixelY >= 0 && pixelY < textureHeight)
            {
              Color originalPixel = loadedTexture.GetPixel(pixelX, pixelY);
              Color blended = Color.Lerp(originalPixel, color, 0.5f);
              texture.SetPixel(pixelX, pixelY, blended);
            }
          }
        }
      }

      if (x == x2 && y == y2) break;

      int e2 = 2 * err;
      if (e2 > -dy) { err -= dy; x += sx; }
      if (e2 < dx) { err += dx; y += sy; }
    }
  }

  private Vector2 GetNormalisedImagePoint(Vector2 localPoint)
  {
    Rect rect = previewImageRect.rect;
    float normalisedX = (localPoint.x + rect.width * 0.5f) / rect.width;
    float normalisedY = (localPoint.y + rect.height * 0.5f) / rect.height;

    //Clamp to image bounds
    normalisedX = Mathf.Clamp01(normalisedX);
    normalisedY = Mathf.Clamp01(normalisedY);

    return new Vector2(normalisedX, normalisedY);
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

    Texture2D tempTexture = new Texture2D(2, 2);
    bool imageLoaded = tempTexture.LoadImage(imageData);

    if (imageLoaded && previewImage != null)
    {
      loadedTexture = ScaleTexture(tempTexture, outputImageWidth, outputImageHeight);

      Destroy(tempTexture);

      Sprite imageSprite = Sprite.Create(
          loadedTexture,
          new Rect(0, 0, loadedTexture.width, loadedTexture.height),
          new Vector2(0.5f, 0.5f)
      );
      if (previewImage.sprite != null) previewImage.sprite = null;
      previewImage.sprite = imageSprite;
      previewImage.preserveAspect = true;
      previewImage.gameObject.SetActive(true);

      if (traceButton != null)
      {
        traceButton.gameObject.SetActive(true);
      }

      if (resetTraceButton != null)
      {
        resetTraceButton.gameObject.SetActive(true);
      }

      if (maskWidthSlider != null)
      {
        maskWidthSlider.gameObject.SetActive(true);
      }

      if (maskWidthLabel != null)
      {
        maskWidthLabel.gameObject.SetActive(true);
      }

      string fileName = Path.GetFileName(imagePath);
      Debug.Log($"Image loaded successfully: {fileName} ({loadedTexture.width}x{loadedTexture.height})");
      OnImageLoaded?.Invoke($"Image loaded: {fileName}");

      //Reset any existing centerline
      ResetCenterline();
    }
    else
    {
      Debug.LogError("Failed to load image: " + imagePath);
    }

    yield return null;
  }

  private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
  {
    RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
    RenderTexture.active = rt;
    Graphics.Blit(source, rt);

    Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
    result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
    result.Apply();

    RenderTexture.active = null;
    RenderTexture.ReleaseTemporary(rt);

    return result;
  }

  public void ResetCenterline()
  {
    centerlinePoints.Clear();
    startPosition = null;
    raceDirection = 0f;
    isDrawing = false;

    if (centerlineOverlay != null)
    {
      Destroy(centerlineOverlay);
      centerlineOverlay = null;
    }

    //Reset image preview
    if (loadedTexture != null && previewImage != null)
    {
      Sprite imageSprite = Sprite.Create(loadedTexture, new Rect(0, 0, loadedTexture.width, loadedTexture.height), new Vector2(0.5f, 0.5f));
      if (previewImage.sprite != null) previewImage.sprite = null;
      previewImage.sprite = imageSprite;
    }
    if (processButton != null)
    {
      processButton.gameObject.SetActive(false);
    }
    Debug.Log("Centerline reset");
  }

  private void ShowErrorPopUp()
  {
    ResetCenterline();
    if (errorPopUp != null)
    {
      errorPopUp.SetActive(true);
      StartCoroutine(HideAfterDelay(5f)); // 5 seconds
    }
  }

  private IEnumerator HideAfterDelay(float delay)
  {
    yield return new WaitForSeconds(delay);
    errorPopUp.SetActive(false);
  }

  public void ProcessTrackImage()
  {
    if (isProcessing)
    {
      Debug.LogWarning("Processing already in progress");
      return;
    }

    if (string.IsNullOrEmpty(selectedImagePath))
    {
      Debug.LogError("No image selected for processing");
      return;
    }

    if (LoaderPanel != null)
    {
      LoaderPanel.SetActive(true);
    }
    StartCoroutine(ProcessTrackImageCoroutine());
  }

  // Flip Y of CNN points to Unity space
  private List<Vector2> FlipY(List<Vector2> pts, float height)
  {
    if (pts == null) return null;
    return pts.Select(p => new Vector2(p.x, height - p.y)).ToList();
  }

  private IEnumerator ProcessTrackImageCoroutine()
  {
    yield return null;
    if (isProcessing) yield break;

    isProcessing = true;
    float startTime = Time.realtimeSinceStartup;

    // SHOW LOADING SCREEN HERE
    // Example: LoadingScreenManager.Instance.ShowLoadingScreen("Processing track image...");

    Debug.Log("Starting track image processing...");
    OnProcessingStarted?.Invoke("Processing track image...");

    // Disable process button during processing
    if (processButton != null)
    {
      processButton.interactable = false;
    }

    yield return null;
    // Create mask from centerline
    Texture2D centerlineMask = CreateMaskFromCenterline();
    yield return null;
    if (centerlineMask == null)
    {
      string errorMsg = "Failed to create centerline mask";
      Debug.LogError(errorMsg);

      lastResults = new ProcessingResults
      {
        success = false,
        errorMessage = errorMsg,
        processingTime = Time.realtimeSinceStartup - startTime
      };

      // Re-enable button
      if (processButton != null)
      {
        processButton.interactable = true;
      }

      // HIDE LOADING SCREEN HERE
      // Example: LoadingScreenManager.Instance.HideLoadingScreen();

      isProcessing = false;
      OnProcessingComplete?.Invoke(lastResults);
      yield break;
    }
    yield return null;
    // Apply the mask to the original image
    Texture2D maskedImage = ApplyMaskToImage(loadedTexture, centerlineMask);
    yield return null;
    // Save the masked image to a temporary file
    string tempFilePath = Path.Combine(Application.persistentDataPath, "temp_masked_track.png");
    byte[] maskedImageBytes = maskedImage.EncodeToPNG();
    File.WriteAllBytes(tempFilePath, maskedImageBytes);
    yield return null;
    // Clean up textures we don't need anymore
    Destroy(centerlineMask);
    Destroy(maskedImage);

    yield return null; // Allow UI to update

    Debug.Log(tempFilePath);

    // Run both image processing and PSO optimization in background tasks
    Debug.Log($"Starting background image processing and PSO optimization...");
    OnProcessingStarted?.Invoke("Processing boundaries and optimizing raceline...");

    // Create a combined task that handles both image processing and PSO
    List<Vector2> centerlinePointsCopy = new List<Vector2>(centerlinePoints);
    Vector2? startPositionCopy = startPosition;

    yield return null;
    // Run both image processing and PSO optimization in background tasks
    Task<ProcessingTaskResult> combinedTask = Task.Run(() =>
    {
      try
      {
        // Process the MASKED image to get boundaries
        ImageProcessing.TrackBoundaries boundaries = ImageProcessing.ProcessImage(tempFilePath);

        if (!boundaries.success)
        {
          return new ProcessingTaskResult
          {
            success = false,
            errorMessage = boundaries.errorMessage,
            boundaries = null,
            racelineResult = null
          };
        }

        float imageHeight = loadedTexture.height;
        // Flip Y of boundaries to Unity space
        boundaries.innerBoundary = FlipY(boundaries.innerBoundary, imageHeight);
        boundaries.outerBoundary = FlipY(boundaries.outerBoundary, imageHeight);
        // Use the copied variables instead of the original ones
        List<Vector2> alignedInner = AlignBoundaryWithUserInputBackground(boundaries.innerBoundary, centerlinePointsCopy, startPositionCopy);
        List<Vector2> alignedOuter = AlignBoundaryWithUserInputBackground(boundaries.outerBoundary, centerlinePointsCopy, startPositionCopy);

        // Run PSO optimization
        PSOInterface.RacelineResult racelineResult = PSOIntegrator.RunPSO(
            alignedInner,
            alignedOuter,
            particleCount,
            maxIterations
        );

        return new ProcessingTaskResult
        {
          success = true,
          errorMessage = "",
          boundaries = boundaries,
          alignedInner = alignedInner,
          alignedOuter = alignedOuter,
          racelineResult = racelineResult
        };
      }
      catch (System.Exception ex)
      {
        return new ProcessingTaskResult
        {
          success = false,
          errorMessage = $"Background processing failed: {ex.Message}",
          boundaries = null,
          racelineResult = null
        };
      }
    });
    // Wait for combined task to complete while keeping UI responsive


    while (!combinedTask.IsCompleted)
    {
      yield return null; // Keep UI responsive
    }

    if (LoaderPanel != null)
    {
      LoaderPanel.SetActive(false);
    }

    ProcessingTaskResult taskResult;

    // Handle task completion states safely
    if (combinedTask.IsFaulted)
    {
      // Handle background exception
      string errorMsg = "Background processing task failed: " + combinedTask.Exception?.GetBaseException().Message;
      Debug.LogError(errorMsg);

      lastResults = new ProcessingResults
      {
        success = false,
        errorMessage = errorMsg,
        processingTime = Time.realtimeSinceStartup - startTime
      };

      // Re-enable button
      if (processButton != null)
      {
        processButton.interactable = true;
      }

      // HIDE LOADING SCREEN HERE
      // Example: LoadingScreenManager.Instance.HideLoadingScreen();

      isProcessing = false;
      OnProcessingComplete?.Invoke(lastResults);
      yield break;
    }
    else if (combinedTask.IsCanceled)
    {
      string errorMsg = "Background processing task was canceled.";
      Debug.LogError(errorMsg);

      ShowErrorPopUp();

      lastResults = new ProcessingResults
      {
        success = false,
        errorMessage = errorMsg,
        processingTime = Time.realtimeSinceStartup - startTime
      };

      // Re-enable button
      if (processButton != null)
      {
        processButton.interactable = true;
      }

      // HIDE LOADING SCREEN HERE
      // Example: LoadingScreenManager.Instance.HideLoadingScreen();

      isProcessing = false;
      OnProcessingComplete?.Invoke(lastResults);
      yield break;
    }
    else
    {
      taskResult = combinedTask.Result;
    }

    if (!taskResult.success)
    {
      // string errorMsg = taskResult.errorMessage;
      // Debug.LogError(errorMsg);
      ShowErrorPopUp();

      lastResults = new ProcessingResults
      {
        success = false,
        errorMessage = taskResult.errorMessage,
        processingTime = Time.realtimeSinceStartup - startTime
      };

      // Re-enable button
      if (processButton != null)
      {
        processButton.interactable = true;
      }

      // HIDE LOADING SCREEN HERE
      // Example: LoadingScreenManager.Instance.HideLoadingScreen();

      isProcessing = false;
      OnProcessingComplete?.Invoke(lastResults);
      yield break;
    }

    PSOInterface.RacelineResult racelineResult = taskResult.racelineResult;

    if (racelineResult == null)
    {
      string errorMsg = "Raceline optimization failed - no result returned";
      Debug.LogError(errorMsg);
      ShowErrorPopUp();

      lastResults = new ProcessingResults
      {
        success = false,
        errorMessage = errorMsg,
        processingTime = Time.realtimeSinceStartup - startTime
      };

      // Re-enable button
      if (processButton != null)
      {
        processButton.interactable = true;
      }
      // HIDE LOADING SCREEN HERE
      // Example: LoadingScreenManager.Instance.HideLoadingScreen();

      isProcessing = false;
      OnProcessingComplete?.Invoke(lastResults);
      yield break;
    }

    // Convert System.Numerics.Vector2 to UnityEngine.Vector2
    float imageHeight = loadedTexture.height;
    List<Vector2> innerBoundary = FlipY(ConvertToUnityVectors(racelineResult.InnerBoundary), imageHeight);
    List<Vector2> outerBoundary = FlipY(ConvertToUnityVectors(racelineResult.OuterBoundary), imageHeight);
    List<Vector2> raceline = FlipY(ConvertToUnityVectors(racelineResult.Raceline), imageHeight);

    float processingTime = Time.realtimeSinceStartup - startTime;

    // Store results
    lastResults = new ProcessingResults
    {
      success = true,
      errorMessage = "",
      innerBoundary = innerBoundary,
      outerBoundary = outerBoundary,
      raceline = raceline,
      processingTime = processingTime,
      centerlinePoints = centerlinePoints,
      startPosition = startPosition,
      raceDirection = raceDirection
    };

    Debug.Log($"Track processing completed successfully in {processingTime:F2} seconds.");

    // Generate output image
    // GenerateOutputImage();

    // Re-enable button
    if (processButton != null)
    {
      processButton.interactable = true;
    }

    Debug.Log("Training started");

    ACOTrackMaster.LoadTrack(lastResults);

    trainer.StartTraining();

    while (!trainer.IsDone())
    {
      yield break;
    }


    lastResults.raceline = trainer.GetNewRaceline();

    Debug.Log("Training finished");

    // HIDE LOADING SCREEN HERE
    // Example: LoadingScreenManager.Instance.HideLoadingScreen();

    isProcessing = false;

    // Navigate to racing line page with processed data
    NavigateToRacingLineWithProcessedData();

    OnProcessingComplete?.Invoke(lastResults);
  }

  // Helper class for background task results
  private class ProcessingTaskResult
  {
    public bool success;
    public string errorMessage;
    public ImageProcessing.TrackBoundaries boundaries;
    public List<Vector2> alignedInner;
    public List<Vector2> alignedOuter;
    public PSOInterface.RacelineResult racelineResult;
  }

  private List<Vector2> AlignBoundaryWithUserInput(List<Vector2> boundary)
  {
    if (boundary == null || boundary.Count == 0 || centerlinePoints.Count < 2)
    {
      Debug.LogWarning("Cannot align boundary - missing data");
      return boundary;
    }
    // Find the closest point on the boundary to the user-defined start position
    Vector2 userStart = startPosition ?? centerlinePoints[0];
    int closestIndex = FindClosestPointIndex(boundary, userStart);

    // Reorder the boundary to start from this point
    List<Vector2> reorderedBoundary = ReorderBoundary(boundary, closestIndex);

    // Check and correct direction
    List<Vector2> directionCorrectedBoundary = EnsureBoundaryDirection(reorderedBoundary);

    Debug.Log($"Boundary aligned with user input. Start index: {closestIndex}");

    return directionCorrectedBoundary;
  }

  // Background thread version of AlignBoundaryWithUserInput
  private List<Vector2> AlignBoundaryWithUserInputBackground(List<Vector2> boundary, List<Vector2> centerlinePointsCopy, Vector2? startPositionCopy)
  {
    if (boundary == null || boundary.Count == 0 || centerlinePointsCopy.Count < 200)
    {
      Debug.LogWarning("Cannot align boundary - missing data");
      return boundary;
    }

    // Find the closest point on the boundary to the user-defined start position
    Vector2 userStart = startPositionCopy ?? centerlinePointsCopy[0];
    int closestIndex = FindClosestPointIndexBackground(boundary, userStart);

    // Reorder the boundary to start from this point
    List<Vector2> reorderedBoundary = ReorderBoundaryBackground(boundary, closestIndex);

    // Check and correct direction
    List<Vector2> directionCorrectedBoundary = EnsureBoundaryDirectionBackground(reorderedBoundary, centerlinePointsCopy);

    Debug.Log($"Boundary aligned with user input. Start index: {closestIndex}");

    return directionCorrectedBoundary;
  }

  // Background thread versions of helper methods
  private int FindClosestPointIndexBackground(List<Vector2> points, Vector2 target)
  {
    int closestIndex = 0;
    float closestDistSqr = float.MaxValue;

    for (int i = 0; i < points.Count; i++)
    {
      float distSqr = (points[i] - target).sqrMagnitude;
      if (distSqr < closestDistSqr)
      {
        closestDistSqr = distSqr;
        closestIndex = i;
      }
    }

    return closestIndex;
  }

  private List<Vector2> ReorderBoundaryBackground(List<Vector2> boundary, int startIndex)
  {
    List<Vector2> reordered = new List<Vector2>();

    for (int i = 0; i < boundary.Count; i++)
    {
      int index = (startIndex + i) % boundary.Count;
      reordered.Add(boundary[index]);
    }

    return reordered;
  }

  private List<Vector2> EnsureBoundaryDirectionBackground(List<Vector2> boundary, List<Vector2> centerlinePointsCopy)
  {
    if (boundary.Count < 3 || centerlinePointsCopy.Count < 2)
    {
      return boundary;
    }

    // Calculate boundary direction using first three points
    Vector2 bStart = boundary[0];
    Vector2 bMid = boundary[1];
    Vector2 bEnd = boundary[2];

    Vector2 bDir1 = (bMid - bStart).normalized;
    Vector2 bDir2 = (bEnd - bMid).normalized;
    Vector2 boundaryDirection = ((bDir1 + bDir2) * 0.5f).normalized;

    // Calculate user-defined centerline direction
    Vector2 cStart = centerlinePointsCopy[0];
    Vector2 cEnd = centerlinePointsCopy[Math.Min(10, centerlinePointsCopy.Count - 1)];
    Vector2 centerlineDirection = (cEnd - cStart).normalized;

    // Calculate angle between directions
    float angle = Vector2.SignedAngle(boundaryDirection, centerlineDirection);

    // If angle is greater than 90 degrees, reverse the boundary
    if ((float)System.Math.Abs(angle) > 90f)
    {
      boundary.Reverse();
      Debug.Log("Boundary direction reversed to match user-defined direction");
    }
    else
    {
      Debug.Log("Boundary direction matches user-defined direction");
    }

    return boundary;
  }

  private int FindClosestPointIndex(List<Vector2> points, Vector2 target)
  {
    int closestIndex = 0;
    float closestDistSqr = float.MaxValue;

    for (int i = 0; i < points.Count; i++)
    {
      float distSqr = (points[i] - target).sqrMagnitude;
      if (distSqr < closestDistSqr)
      {
        closestDistSqr = distSqr;
        closestIndex = i;
      }
    }

    return closestIndex;
  }

  private List<Vector2> ReorderBoundary(List<Vector2> boundary, int startIndex)
  {
    List<Vector2> reordered = new List<Vector2>();

    for (int i = 0; i < boundary.Count; i++)
    {
      int index = (startIndex + i) % boundary.Count;
      reordered.Add(boundary[index]);
    }

    return reordered;
  }

  private List<Vector2> EnsureBoundaryDirection(List<Vector2> boundary)
  {
    if (boundary.Count < 3 || centerlinePoints.Count < 2)
    {
      return boundary;
    }

    // Calculate boundary direction using first three points
    Vector2 bStart = boundary[0];
    Vector2 bMid = boundary[1];
    Vector2 bEnd = boundary[2];

    Vector2 bDir1 = (bMid - bStart).normalized;
    Vector2 bDir2 = (bEnd - bMid).normalized;
    Vector2 boundaryDirection = ((bDir1 + bDir2) * 0.5f).normalized;

    // Calculate user-defined centerline direction
    Vector2 cStart = centerlinePoints[0];
    Vector2 cEnd = centerlinePoints[Mathf.Min(10, centerlinePoints.Count - 1)];
    Vector2 centerlineDirection = (cEnd - cStart).normalized;

    // Calculate angle between directions
    float angle = Vector2.SignedAngle(boundaryDirection, centerlineDirection);

    // If angle is greater than 90 degrees, reverse the boundary
    if (Mathf.Abs(angle) > 90f)
    {
      boundary.Reverse();
      Debug.Log("Boundary direction reversed to match user-defined direction");
    }
    else
    {
      Debug.Log("Boundary direction matches user-defined direction");
    }

    return boundary;
  }

  private Texture2D ApplyMaskToImage(Texture2D sourceImage, Texture2D mask)
  {
    // Create a new texture for the result
    Texture2D result = new Texture2D(sourceImage.width, sourceImage.height, TextureFormat.RGBA32, false);

    // Ensure mask is the same size as source
    if (mask.width != sourceImage.width || mask.height != sourceImage.height)
    {
      Debug.LogError("Mask dimensions don't match image dimensions");
      return null;
    }

    // Apply the mask - only keep pixels where mask is white
    for (int y = 0; y < sourceImage.height; y++)
    {
      for (int x = 0; x < sourceImage.width; x++)
      {
        Color sourcePixel = sourceImage.GetPixel(x, y);
        Color maskPixel = mask.GetPixel(x, y);

        // If mask pixel is white (or close to white), keep the original pixel
        // Otherwise, make it transparent
        if (maskPixel.grayscale > 0.9f) // Adjust threshold as needed
        {
          result.SetPixel(x, y, sourcePixel);
        }
        else
        {
          result.SetPixel(x, y, Color.white);
        }
      }
    }

    result.Apply();
    return result;
  }

  private Texture2D CreateMaskFromCenterline()
  {
    if (loadedTexture == null)
    {
      Debug.LogError("Need at least 100 centerline points and a loaded texture to create mask");
      return null;
    }

    //Create binary mask
    Texture2D binaryMask = new Texture2D(loadedTexture.width, loadedTexture.height);
    Color[] maskPixels = new Color[loadedTexture.width * loadedTexture.height];

    //Initialize to black (background)
    for (int i = 0; i < maskPixels.Length; i++)
    {
      maskPixels[i] = Color.black;
    }

    binaryMask.SetPixels(maskPixels);

    //Draw centerline with specified width
    for (int i = 1; i < centerlinePoints.Count; i++)
    {
      DrawThickLineOnMask(binaryMask, centerlinePoints[i - 1], centerlinePoints[i], maskWidth);
    }

    if (centerlinePoints.Count > 2)
    {
      DrawThickLineOnMask(binaryMask, centerlinePoints[centerlinePoints.Count - 1], centerlinePoints[0], maskWidth);
    }

    binaryMask.Apply();
    return binaryMask;
  }
  private void DrawThickLineOnMask(Texture2D mask, Vector2 start, Vector2 end, int thickness)
  {
    int x1 = Mathf.RoundToInt(start.x);
    int y1 = Mathf.RoundToInt(start.y);
    int x2 = Mathf.RoundToInt(end.x);
    int y2 = Mathf.RoundToInt(end.y);

    // Bresenham's line algorithm
    int dx = Mathf.Abs(x2 - x1);
    int dy = Mathf.Abs(y2 - y1);
    int sx = x1 < x2 ? 1 : -1;
    int sy = y1 < y2 ? 1 : -1;
    int err = dx - dy;

    int x = x1;
    int y = y1;

    while (true)
    {
      // Draw thick line
      for (int offsetX = -thickness / 2; offsetX <= thickness / 2; offsetX++)
      {
        for (int offsetY = -thickness / 2; offsetY <= thickness / 2; offsetY++)
        {
          int pixelX = x + offsetX;
          int pixelY = y + offsetY;

          if (pixelX >= 0 && pixelX < mask.width && pixelY >= 0 && pixelY < mask.height)
          {
            mask.SetPixel(pixelX, pixelY, Color.white);
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

    homePageNavigation.NavigateToRacingLine();
    // Get the racing line component and send the data
    if (homePageNavigation.racingLinePage != null)
    {
      ShowRacingLine racingLineComponent = homePageNavigation.racingLinePage.GetComponentInChildren<ShowRacingLine>(true);
      if (racingLineComponent != null)
      {
        // Send the processed data to the racing line display
        racingLineComponent.DisplayRacelineData(racelineData);
        Debug.Log($"Successfully sent processed track data to racing line page");
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

  public bool HasCenterlineData()
  {
    return centerlinePoints != null && centerlinePoints.Count > 100;
  }

  public List<Vector2> GetCenterlinePoints()
  {
    return new List<Vector2>(centerlinePoints);
  }

  public Vector2? GetStartPosition()
  {
    return startPosition;
  }

  public float GetRaceDirection()
  {
    return raceDirection;
  }

  public bool IsProcessing()
  {
    return isProcessing;
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
      if (outputImage.sprite != null) outputImage.sprite = null;

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
    if (points.Count > 2 && points != lastResults.centerlinePoints)
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

  public Texture2D GetCenterlineMask()
  {
    return CreateMaskFromCenterline();
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

    if (centerlineOverlay != null)
    {
      Destroy(centerlineOverlay);
    }
  }
}