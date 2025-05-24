using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using SFB; // StandaloneFileBrowser

//git clone and add to asssets https://github.com/gkngkc/UnityStandaloneFileBrowser.git

public class TrackPanel : MonoBehaviour
{
    [Header("UI References")]
    public Button uploadButton;
    public Button generateMeshButton;
    public RawImage imagePreview;
    public GameObject meshViewerContainer;
    public TextMeshProUGUI statusText;
    public Slider heightScaleSlider;
    public TextMeshProUGUI heightScaleLabel;
    
    [Header("Texture Controls")]
    public Toggle useTextureToggle;
    public Toggle useVertexColorsToggle;
    public Toggle separateTexturesToggle;
    public Button uploadColorTextureButton;
    public RawImage colorTexturePreview;
    
    [Header("Mesh Generation Settings")]
    public Material trackMaterial;
    public int meshResolution = 100;
    public float baseHeight = 0.1f;
    public float maxHeight = 2.0f;
    
    [Header("3D Viewer")]
    public Camera meshCamera;
    public Transform meshParent;
    
    private Texture2D uploadedTexture;
    private Texture2D uploadedColorTexture;
    private GameObject generatedMeshObject;
    private TrackViewer trackViewer;
    private TrackMeshGenerator meshGenerator;
    
    void Start()
    {
        InitializeComponents();
        SetupUI();
    }
    
    void InitializeComponents()
    {
        // Initialize mesh generator
        meshGenerator = gameObject.AddComponent<TrackMeshGenerator>();
        
        // Setup track viewer if mesh viewer container exists
        if (meshViewerContainer != null)
        {
            trackViewer = meshViewerContainer.AddComponent<TrackViewer>();
            trackViewer.Initialize(meshCamera, meshParent);
        }
        
        // Set default values
        if (heightScaleSlider != null)
        {
            heightScaleSlider.value = 1.0f;
            heightScaleSlider.onValueChanged.AddListener(OnHeightScaleChanged);
        }
        
        // Setup texture control defaults
        SetupTextureControls();
    }
    
    void SetupTextureControls()
    {
        // Setup texture toggles
        if (useTextureToggle != null)
        {
            useTextureToggle.isOn = true;
            useTextureToggle.onValueChanged.AddListener(OnUseTextureChanged);
        }
        
        if (useVertexColorsToggle != null)
        {
            useVertexColorsToggle.isOn = true;
            useVertexColorsToggle.onValueChanged.AddListener(OnUseVertexColorsChanged);
        }
        
        if (separateTexturesToggle != null)
        {
            separateTexturesToggle.isOn = false;
            separateTexturesToggle.onValueChanged.AddListener(OnSeparateTexturesChanged);
        }
        
        // Setup color texture upload
        if (uploadColorTextureButton != null)
        {
            uploadColorTextureButton.onClick.AddListener(OpenColorTextureDialog);
            uploadColorTextureButton.interactable = false; // Disabled until separate textures is enabled
        }
        
        if (colorTexturePreview != null)
        {
            colorTexturePreview.gameObject.SetActive(false);
        }
    }
    
    void SetupUI()
    {
        // Setup upload button
        if (uploadButton != null)
            uploadButton.onClick.AddListener(OpenFileDialog);
            
        // Setup generate mesh button
        if (generateMeshButton != null)
        {
            generateMeshButton.onClick.AddListener(GenerateTrackMesh);
            generateMeshButton.interactable = false; // Disabled until image is loaded
        }
        
        // Update status
        UpdateStatus("Ready to upload track image");
        
        // Update height scale label
        UpdateHeightScaleLabel();
    }
    
    public void OpenFileDialog()
    {
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg", "bmp", "tga"),
        };
        
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Track Image", "", extensions, false);
        
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            StartCoroutine(LoadImageFromFile(paths[0]));
        }
    }
    
    public void OpenColorTextureDialog()
    {
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg", "bmp", "tga"),
        };
        
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Color Texture", "", extensions, false);
        
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            StartCoroutine(LoadColorTextureFromFile(paths[0]));
        }
    }
    
    IEnumerator LoadImageFromFile(string path)
    {
        UpdateStatus("Loading image...");
        
        byte[] fileData = System.IO.File.ReadAllBytes(path);
        
        if (uploadedTexture != null)
            DestroyImmediate(uploadedTexture);
            
        uploadedTexture = new Texture2D(2, 2);
        
        if (uploadedTexture.LoadImage(fileData))
        {
            // Display preview
            if (imagePreview != null)
            {
                imagePreview.texture = uploadedTexture;
                imagePreview.gameObject.SetActive(true);
            }
            
            // Enable generate button
            if (generateMeshButton != null)
                generateMeshButton.interactable = true;
                
            UpdateStatus($"Height map loaded: {uploadedTexture.width}x{uploadedTexture.height}");
        }
        else
        {
            UpdateStatus("Failed to load image");
        }
        
        yield return null;
    }
    
    IEnumerator LoadColorTextureFromFile(string path)
    {
        UpdateStatus("Loading color texture...");
        
        byte[] fileData = System.IO.File.ReadAllBytes(path);
        
        if (uploadedColorTexture != null)
            DestroyImmediate(uploadedColorTexture);
            
        uploadedColorTexture = new Texture2D(2, 2);
        
        if (uploadedColorTexture.LoadImage(fileData))
        {
            // Display preview
            if (colorTexturePreview != null)
            {
                colorTexturePreview.texture = uploadedColorTexture;
                colorTexturePreview.gameObject.SetActive(true);
            }
            
            // Set the color texture in the mesh generator
            if (meshGenerator != null)
                meshGenerator.SetColorTexture(uploadedColorTexture);
                
            UpdateStatus($"Color texture loaded: {uploadedColorTexture.width}x{uploadedColorTexture.height}");
            
            // Regenerate mesh if one exists
            if (generatedMeshObject != null && uploadedTexture != null)
            {
                GenerateTrackMesh();
            }
        }
        else
        {
            UpdateStatus("Failed to load color texture");
        }
        
        yield return null;
    }
    
    public void GenerateTrackMesh()
    {
        if (uploadedTexture == null)
        {
            UpdateStatus("No image loaded");
            return;
        }
        
        UpdateStatus("Generating 3D mesh...");
        
        // Destroy previous mesh if exists
        if (generatedMeshObject != null)
            DestroyImmediate(generatedMeshObject);
        
        // Generate new mesh
        generatedMeshObject = meshGenerator.GenerateTrackMesh(
            uploadedTexture, 
            meshResolution, 
            baseHeight, 
            maxHeight * heightScaleSlider.value,
            trackMaterial
        );
        
        if (generatedMeshObject != null)
        {
            // Parent to mesh container
            if (meshParent != null)
                generatedMeshObject.transform.SetParent(meshParent);
            
            // Position and scale appropriately
            generatedMeshObject.transform.localPosition = Vector3.zero;
            generatedMeshObject.transform.localRotation = Quaternion.identity;
            
            // Setup viewer
            if (trackViewer != null)
                trackViewer.SetTarget(generatedMeshObject);
            
            // Show mesh viewer
            if (meshViewerContainer != null)
                meshViewerContainer.SetActive(true);
                
            UpdateStatus("3D track mesh generated successfully!");
        }
        else
        {
            UpdateStatus("Failed to generate mesh");
        }
    }
    
    void OnHeightScaleChanged(float value)
    {
        UpdateHeightScaleLabel();
        
        // Regenerate mesh if one exists
        if (generatedMeshObject != null && uploadedTexture != null)
        {
            GenerateTrackMesh();
        }
    }
    
    void OnUseTextureChanged(bool value)
    {
        if (meshGenerator != null)
            meshGenerator.SetUseImageAsTexture(value);
            
        // Regenerate mesh if one exists
        if (generatedMeshObject != null && uploadedTexture != null)
        {
            GenerateTrackMesh();
        }
    }
    
    void OnUseVertexColorsChanged(bool value)
    {
        if (meshGenerator != null)
            meshGenerator.SetUseVertexColors(value);
            
        // Regenerate mesh if one exists
        if (generatedMeshObject != null && uploadedTexture != null)
        {
            GenerateTrackMesh();
        }
    }
    
    void OnSeparateTexturesChanged(bool value)
    {
        if (meshGenerator != null)
            meshGenerator.SetSeparateTextures(value);
            
        // Enable/disable color texture upload button
        if (uploadColorTextureButton != null)
            uploadColorTextureButton.interactable = value;
            
        // Regenerate mesh if one exists
        if (generatedMeshObject != null && uploadedTexture != null)
        {
            GenerateTrackMesh();
        }
    }
    
    void UpdateHeightScaleLabel()
    {
        if (heightScaleLabel != null && heightScaleSlider != null)
        {
            heightScaleLabel.text = $"Height Scale: {heightScaleSlider.value:F1}x";
        }
    }
    
    void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log($"TrackPanel: {message}");
    }
    
    void OnDestroy()
    {
        if (uploadedTexture != null)
            DestroyImmediate(uploadedTexture);
            
        if (uploadedColorTexture != null)
            DestroyImmediate(uploadedColorTexture);
    }
} 