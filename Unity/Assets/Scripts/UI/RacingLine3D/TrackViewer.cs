using UnityEngine;
using UnityEngine.EventSystems;

public class TrackViewer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler
{
    [Header("Camera Controls")]
    public float rotationSpeed = 2.0f;
    public float zoomSpeed = 2.0f;
    public float panSpeed = 1.0f;
    public float minZoomDistance = 2.0f;
    public float maxZoomDistance = 50.0f;
    
    [Header("Auto Rotation")]
    public bool enableAutoRotation = false;
    public float autoRotationSpeed = 10.0f;
    
    private Camera viewerCamera;
    private Transform meshParent;
    private GameObject targetMesh;
    
    private Vector3 lastMousePosition;
    private bool isRotating = false;
    private bool isPanning = false;
    private float currentZoomDistance = 15.0f;
    
    // Camera orbit variables
    private Vector3 targetPosition = Vector3.zero;
    private float currentRotationX = 0f;
    private float currentRotationY = 0f;
    
    public void Initialize(Camera camera, Transform parent)
    {
        viewerCamera = camera;
        meshParent = parent;
        
        if (viewerCamera == null)
        {
            Debug.LogError("TrackViewer: Camera is null");
            return;
        }
        
        // Set initial camera position
        ResetCameraPosition();
    }
    
    public void SetTarget(GameObject mesh)
    {
        targetMesh = mesh;
        
        if (targetMesh != null)
        {
            // Calculate bounds to position camera appropriately
            Bounds bounds = GetMeshBounds(targetMesh);
            targetPosition = bounds.center;
            
            // Set zoom distance based on mesh size
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            currentZoomDistance = Mathf.Clamp(maxSize * 2.0f, minZoomDistance, maxZoomDistance);
            
            UpdateCameraPosition();
        }
    }
    
    void Update()
    {
        if (viewerCamera == null) return;
        
        // Handle keyboard input
        HandleKeyboardInput();
        
        // Auto rotation
        if (enableAutoRotation && !isRotating && !isPanning)
        {
            currentRotationY += autoRotationSpeed * Time.deltaTime;
            UpdateCameraPosition();
        }
    }
    
    void HandleKeyboardInput()
    {
        // Reset camera with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCameraPosition();
        }
        
        // Toggle auto rotation with Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            enableAutoRotation = !enableAutoRotation;
        }
        
        // Zoom with +/- keys
        if (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus))
        {
            ZoomCamera(-zoomSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
        {
            ZoomCamera(zoomSpeed * Time.deltaTime);
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        lastMousePosition = Input.mousePosition;
        
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isRotating = true;
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            isPanning = true;
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isRotating = false;
        isPanning = false;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (viewerCamera == null) return;
        
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
        
        if (isRotating)
        {
            // Rotate around target
            currentRotationY += mouseDelta.x * rotationSpeed;
            currentRotationX -= mouseDelta.y * rotationSpeed;
            
            // Clamp vertical rotation
            currentRotationX = Mathf.Clamp(currentRotationX, -80f, 80f);
            
            UpdateCameraPosition();
        }
        else if (isPanning)
        {
            // Pan the target position
            Vector3 right = viewerCamera.transform.right;
            Vector3 up = viewerCamera.transform.up;
            
            Vector3 panMovement = (-right * mouseDelta.x + up * mouseDelta.y) * panSpeed * 0.01f;
            targetPosition += panMovement;
            
            UpdateCameraPosition();
        }
        
        lastMousePosition = Input.mousePosition;
    }
    
    public void OnScroll(PointerEventData eventData)
    {
        if (viewerCamera == null) return;
        
        float scrollDelta = eventData.scrollDelta.y;
        ZoomCamera(-scrollDelta * zoomSpeed * 0.1f);
    }
    
    void ZoomCamera(float zoomDelta)
    {
        currentZoomDistance += zoomDelta;
        currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);
        UpdateCameraPosition();
    }
    
    void UpdateCameraPosition()
    {
        if (viewerCamera == null) return;
        
        // Calculate camera position based on rotation and zoom
        Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
        Vector3 direction = rotation * Vector3.back;
        Vector3 cameraPosition = targetPosition + direction * currentZoomDistance;
        
        viewerCamera.transform.position = cameraPosition;
        viewerCamera.transform.LookAt(targetPosition);
    }
    
    void ResetCameraPosition()
    {
        currentRotationX = 20f;
        currentRotationY = 45f;
        currentZoomDistance = 15.0f;
        targetPosition = Vector3.zero;
        
        if (targetMesh != null)
        {
            Bounds bounds = GetMeshBounds(targetMesh);
            targetPosition = bounds.center;
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            currentZoomDistance = Mathf.Clamp(maxSize * 2.0f, minZoomDistance, maxZoomDistance);
        }
        
        UpdateCameraPosition();
    }
    
    Bounds GetMeshBounds(GameObject meshObject)
    {
        Bounds bounds = new Bounds();
        bool hasBounds = false;
        
        // Get bounds from all renderers in the object
        Renderer[] renderers = meshObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        
        // If no renderers found, create a default bounds
        if (!hasBounds)
        {
            bounds = new Bounds(meshObject.transform.position, Vector3.one * 10f);
        }
        
        return bounds;
    }
    
    // Public methods for UI controls
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
    
    public void SetZoomSpeed(float speed)
    {
        zoomSpeed = speed;
    }
    
    public void SetPanSpeed(float speed)
    {
        panSpeed = speed;
    }
    
    public void ToggleAutoRotation()
    {
        enableAutoRotation = !enableAutoRotation;
    }
    
    public void SetAutoRotationSpeed(float speed)
    {
        autoRotationSpeed = speed;
    }
    
    // Method to focus on the mesh
    public void FocusOnMesh()
    {
        if (targetMesh != null)
        {
            Bounds bounds = GetMeshBounds(targetMesh);
            targetPosition = bounds.center;
            
            // Set appropriate zoom distance
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            currentZoomDistance = Mathf.Clamp(maxSize * 1.5f, minZoomDistance, maxZoomDistance);
            
            UpdateCameraPosition();
        }
    }
} 