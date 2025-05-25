using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class TrackPanelSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnStart = true;
    
    [Header("UI Layout Settings")]
    [SerializeField] private Vector2 panelSize = new Vector2(800, 600);
    [SerializeField] private Vector2 imagePreviewSize = new Vector2(300, 200);
    [SerializeField] private Vector2 meshViewerSize = new Vector2(400, 300);
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupTrackPanel();
        }
    }
    
    [ContextMenu("Setup Track Panel")]
    public void SetupTrackPanel()
    {
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create main panel
        GameObject panelGO = CreatePanel(canvas.transform, "Track Panel", panelSize);
        TrackPanel trackPanel = panelGO.AddComponent<TrackPanel>();
        
        // Create UI layout
        CreateTrackPanelUI(panelGO, trackPanel);
        
        Debug.Log("Track Panel setup complete! Configure the TrackPanel component references in the inspector.");
    }
    
    GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = Vector2.zero;
        
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        return panel;
    }
    
    void CreateTrackPanelUI(GameObject panelGO, TrackPanel trackPanel)
    {
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        
        // Create title
        GameObject title = CreateText(panelGO.transform, "Track Generator", 24, TextAlignmentOptions.Center);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.anchoredPosition = new Vector2(0, -30);
        titleRect.sizeDelta = new Vector2(0, 40);
        
        // Create left side (controls)
        GameObject leftPanel = CreatePanel(panelGO.transform, "Controls Panel", new Vector2(350, panelSize.y - 80));
        RectTransform leftRect = leftPanel.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0, 1);
        leftRect.anchoredPosition = new Vector2(175, -40);
        leftRect.sizeDelta = new Vector2(350, 0);
        leftPanel.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        
        // Create right side (3D viewer)
        GameObject rightPanel = CreatePanel(panelGO.transform, "3D Viewer Panel", new Vector2(400, panelSize.y - 80));
        RectTransform rightRect = rightPanel.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.anchoredPosition = new Vector2(-200, -40);
        rightRect.sizeDelta = new Vector2(400, 0);
        rightPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // Setup left panel controls
        SetupControlsPanel(leftPanel, trackPanel);
        
        // Setup right panel 3D viewer
        SetupViewerPanel(rightPanel, trackPanel);
    }
    
    void SetupControlsPanel(GameObject controlsPanel, TrackPanel trackPanel)
    {
        float yPos = -30;
        float spacing = 60;
        
        // Upload button
        GameObject uploadBtn = CreateButton(controlsPanel.transform, "Upload Track Image", new Vector2(300, 40));
        PositionElement(uploadBtn, 0, yPos);
        trackPanel.uploadButton = uploadBtn.GetComponent<Button>();
        yPos -= spacing;
        
        // Image preview
        GameObject imagePreview = CreateImagePreview(controlsPanel.transform, imagePreviewSize);
        PositionElement(imagePreview, 0, yPos - imagePreviewSize.y / 2);
        trackPanel.imagePreview = imagePreview.GetComponent<RawImage>();
        imagePreview.SetActive(false);
        yPos -= imagePreviewSize.y + 20;
        
        // Height scale slider
        GameObject heightLabel = CreateText(controlsPanel.transform, "Height Scale: 1.0x", 14, TextAlignmentOptions.Left);
        PositionElement(heightLabel, -100, yPos);
        trackPanel.heightScaleLabel = heightLabel.GetComponent<TextMeshProUGUI>();
        yPos -= 30;
        
        GameObject heightSlider = CreateSlider(controlsPanel.transform, 0.1f, 3.0f, 1.0f);
        PositionElement(heightSlider, 0, yPos);
        trackPanel.heightScaleSlider = heightSlider.GetComponent<Slider>();
        yPos -= spacing;
        
        // Generate mesh button
        GameObject generateBtn = CreateButton(controlsPanel.transform, "Generate 3D Mesh", new Vector2(300, 40));
        PositionElement(generateBtn, 0, yPos);
        trackPanel.generateMeshButton = generateBtn.GetComponent<Button>();
        yPos -= spacing;
        
        // Status text
        GameObject statusText = CreateText(controlsPanel.transform, "Ready to upload track image", 12, TextAlignmentOptions.Left);
        PositionElement(statusText, -140, yPos);
        trackPanel.statusText = statusText.GetComponent<TextMeshProUGUI>();
        statusText.GetComponent<TextMeshProUGUI>().color = Color.green;
    }
    
    void SetupViewerPanel(GameObject viewerPanel, TrackPanel trackPanel)
    {
        // Create camera for 3D viewing
        GameObject cameraGO = new GameObject("Track Viewer Camera");
        cameraGO.transform.SetParent(viewerPanel.transform, false);
        Camera camera = cameraGO.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        camera.cullingMask = LayerMask.GetMask("Default");
        
        // Create render texture for the camera
        RenderTexture renderTexture = new RenderTexture(512, 512, 24);
        camera.targetTexture = renderTexture;
        
        // Create raw image to display the render texture
        GameObject viewerImage = new GameObject("3D Viewer");
        viewerImage.transform.SetParent(viewerPanel.transform, false);
        RectTransform viewerRect = viewerImage.AddComponent<RectTransform>();
        viewerRect.anchorMin = Vector2.zero;
        viewerRect.anchorMax = Vector2.one;
        viewerRect.offsetMin = new Vector2(10, 10);
        viewerRect.offsetMax = new Vector2(-10, -10);
        
        RawImage rawImage = viewerImage.AddComponent<RawImage>();
        rawImage.texture = renderTexture;
        
        // Create mesh parent
        GameObject meshParent = new GameObject("Mesh Parent");
        meshParent.transform.SetParent(viewerPanel.transform, false);
        
        // Assign references
        trackPanel.meshCamera = camera;
        trackPanel.meshParent = meshParent.transform;
        trackPanel.meshViewerContainer = viewerPanel;
        
        // Add instructions text
        GameObject instructions = CreateText(viewerPanel.transform, 
            "3D Viewer Controls:\n• Left Click + Drag: Rotate\n• Right Click + Drag: Pan\n• Scroll: Zoom\n• R: Reset View\n• Space: Auto Rotate", 
            10, TextAlignmentOptions.TopLeft);
        RectTransform instrRect = instructions.GetComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0, 1);
        instrRect.anchorMax = new Vector2(1, 1);
        instrRect.anchoredPosition = new Vector2(0, -10);
        instrRect.sizeDelta = new Vector2(0, 80);
        instructions.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f, 1f);
    }
    
    GameObject CreateButton(Transform parent, string text, Vector2 size)
    {
        GameObject button = new GameObject("Button");
        button.transform.SetParent(parent, false);
        
        RectTransform rectTransform = button.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        
        Image image = button.AddComponent<Image>();
        image.color = new Color(0.3f, 0.5f, 0.8f, 1f);
        
        Button btn = button.AddComponent<Button>();
        
        // Create text child
        GameObject textGO = CreateText(button.transform, text, 14, TextAlignmentOptions.Center);
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        return button;
    }
    
    GameObject CreateText(Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(parent, false);
        
        RectTransform rectTransform = textGO.AddComponent<RectTransform>();
        
        TextMeshProUGUI textMesh = textGO.AddComponent<TextMeshProUGUI>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.color = Color.white;
        textMesh.alignment = alignment;
        
        return textGO;
    }
    
    GameObject CreateImagePreview(Transform parent, Vector2 size)
    {
        GameObject imageGO = new GameObject("Image Preview");
        imageGO.transform.SetParent(parent, false);
        
        RectTransform rectTransform = imageGO.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        
        Image border = imageGO.AddComponent<Image>();
        border.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        // Create inner raw image
        GameObject rawImageGO = new GameObject("Raw Image");
        rawImageGO.transform.SetParent(imageGO.transform, false);
        
        RectTransform rawRect = rawImageGO.AddComponent<RectTransform>();
        rawRect.anchorMin = Vector2.zero;
        rawRect.anchorMax = Vector2.one;
        rawRect.offsetMin = new Vector2(2, 2);
        rawRect.offsetMax = new Vector2(-2, -2);
        
        RawImage rawImage = rawImageGO.AddComponent<RawImage>();
        
        return rawImageGO;
    }
    
    GameObject CreateSlider(Transform parent, float minValue, float maxValue, float value)
    {
        GameObject sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(parent, false);
        
        RectTransform rectTransform = sliderGO.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(280, 20);
        
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = value;
        
        // Create background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderGO.transform, false);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        // Create handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(sliderGO.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 20);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        
        slider.targetGraphic = handleImage;
        slider.handleRect = handleRect;
        
        return sliderGO;
    }
    
    void PositionElement(GameObject element, float x, float y)
    {
        RectTransform rect = element.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, y);
    }
} 