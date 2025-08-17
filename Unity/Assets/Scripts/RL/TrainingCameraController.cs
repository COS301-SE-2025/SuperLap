using UnityEngine;
using System.Collections.Generic;

public class TrainingCameraController : MonoBehaviour
{
    [Header("Camera Setup")]
    [Tooltip("The Unity Camera that will be controlled")]
    [SerializeField] private Camera trainingCamera;
    
    [Header("Movement Settings")]
    [Tooltip("Speed of WASD movement")]
    [SerializeField] private float moveSpeed = 40f;
    
    [Tooltip("Speed of camera movement (multiplier)")]
    [SerializeField] private float moveSpeedMultiplier = 3f;
    
    [Tooltip("Hold this key to move faster")]
    [SerializeField] private KeyCode fastMoveKey = KeyCode.LeftShift;
    
    [Header("Zoom Settings")]
    [Tooltip("Zoom speed with scroll wheel")]
    [SerializeField] private float zoomSpeed = 10f;
    
    [Tooltip("Minimum distance from ground")]
    [SerializeField] private float minZoomDistance = 10f;
    
    [Tooltip("Maximum distance from ground")]
    [SerializeField] private float maxZoomDistance = 150f;
    
    [Header("Rotation Settings")]
    [Tooltip("Rotation angle for Q/E keys (degrees)")]
    [SerializeField] private float rotationAngle = 45f;
    
    [Tooltip("Speed of rotation animation")]
    // defaults to a very large number since the animation is a bit wonky
    [SerializeField] private float rotationSpeed = 9999999999f;
    
    [Header("Camera Angle")]
    [Tooltip("Fixed viewing angle (degrees from horizontal)")]
    [SerializeField] private float viewingAngle = 45f;
    
    [Tooltip("Camera field of view")]
    [SerializeField] private float fieldOfView = 60f;
    
    [Header("Controls")]
    [Tooltip("Keys for movement and rotation")]
    [SerializeField] private KeyCode moveForward = KeyCode.W;
    [SerializeField] private KeyCode moveBackward = KeyCode.S;
    [SerializeField] private KeyCode moveLeft = KeyCode.A;
    [SerializeField] private KeyCode moveRight = KeyCode.D;
    [SerializeField] private KeyCode rotateLeft = KeyCode.Q;
    [SerializeField] private KeyCode rotateRight = KeyCode.E;
    [SerializeField] private KeyCode repositionToSpawn = KeyCode.R;
    
    [Header("Debug")]
    [Tooltip("Show debug information")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Internal state
    private Vector3 targetPosition;
    private float targetYRotation;
    private bool isRotating = false;
    private float currentHeight;
    
    // Components
    private CheckpointManager checkpointManager;
    private Trainer trainer;
    
    void Start()
    {
        // Find required components
        checkpointManager = FindAnyObjectByType<CheckpointManager>();
        trainer = FindAnyObjectByType<Trainer>();
        
        // Create camera if not assigned
        if (trainingCamera == null)
        {
            CreateTrainingCamera();
        }
        
        // Set up the camera configuration
        SetupCameraConfiguration();
        
        // Subscribe to track loaded event
        TrackMaster.OnTrackLoaded += OnTrackLoaded;
        
        // Try initial positioning in case track is already loaded
        if (TrackMaster.GetCurrentRaceline() != null)
        {
            InitializeCameraPosition();
        }
        
        Debug.Log("TrainingCameraController initialized - waiting for track to be loaded");
    }
    
    void OnTrackLoaded()
    {
        Debug.Log("Track loaded! Positioning camera relative to spawn points");
        InitializeCameraPosition();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (TrackMaster.OnTrackLoaded != null)
        {
            TrackMaster.OnTrackLoaded -= OnTrackLoaded;
        }
    }
    
    void CreateTrainingCamera()
    {
        // Create a new GameObject for the camera
        GameObject cameraObject = new GameObject("RTS Training Camera");
        cameraObject.transform.parent = transform;
        
        // Add the Camera component
        trainingCamera = cameraObject.AddComponent<Camera>();
        
        // Set basic properties
        trainingCamera.depth = 1; // Higher than main camera
        trainingCamera.clearFlags = CameraClearFlags.Skybox;
        trainingCamera.cullingMask = -1; // Render everything
        
        Debug.Log("Created RTS Training Camera");
    }
    
    void SetupCameraConfiguration()
    {
        if (trainingCamera == null) return;
        
        // Configure camera settings
        trainingCamera.fieldOfView = fieldOfView;
        trainingCamera.nearClipPlane = 0.1f;
        trainingCamera.farClipPlane = 1000f;
        
        // Set fixed viewing angle
        SetCameraAngle();
    }
    
    void InitializeCameraPosition()
    {
        // Check if track is loaded
        var raceline = TrackMaster.GetCurrentRaceline();
        if (raceline == null || raceline.Count == 0)
        {
            Debug.LogWarning("Track not loaded yet - cannot position camera relative to spawn");
            // Set a default position
            targetPosition = new Vector3(0f, 0f, -100f);
            currentHeight = 50f;
            targetYRotation = 0f;
            UpdateCameraPosition();
            return;
        }
        
        // Position camera relative to motorcycle spawn point
        Vector3 spawnPosition = TrackMaster.GetTrainingSpawnPosition(0, raceline);
        PositionRelativeToSpawn(spawnPosition);
        
        Debug.Log($"Camera positioned relative to motorcycle spawn point: {spawnPosition}");
    }
    
    void Update()
    {
        HandleMovementInput();
        HandleZoomInput();
        HandleRotationInput();
        UpdateCameraPosition();
        
        if (showDebugInfo)
        {
            ShowDebugInfo();
        }
    }
    
    void HandleMovementInput()
    {
        Vector3 movement = Vector3.zero;
        
        // Get movement input
        if (Input.GetKey(moveForward)) movement += Vector3.forward;
        if (Input.GetKey(moveBackward)) movement += Vector3.back;
        if (Input.GetKey(moveLeft)) movement += Vector3.left;
        if (Input.GetKey(moveRight)) movement += Vector3.right;
        
        // Apply movement if any input detected
        if (movement != Vector3.zero)
        {
            // Calculate speed
            float currentMoveSpeed = moveSpeed;
            if (Input.GetKey(fastMoveKey))
            {
                currentMoveSpeed *= moveSpeedMultiplier;
            }
            
            // Transform movement relative to camera's Y rotation
            movement = Quaternion.Euler(0, targetYRotation, 0) * movement;
            
            // Apply movement
            targetPosition += movement * currentMoveSpeed * Time.deltaTime;
        }
        
        // R to reposition to spawn
        if (Input.GetKeyDown(repositionToSpawn))
        {
            RepositionToCurrentSpawn();
        }
    }
    
    void HandleZoomInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // Zoom in/out by changing height
            currentHeight -= scroll * zoomSpeed;
            currentHeight = Mathf.Clamp(currentHeight, minZoomDistance, maxZoomDistance);
        }
    }
    
    void HandleRotationInput()
    {
        if (isRotating) return; // Don't allow new rotations while rotating
        
        if (Input.GetKeyDown(rotateLeft))
        {
            StartRotation(-rotationAngle);
        }
        else if (Input.GetKeyDown(rotateRight))
        {
            StartRotation(rotationAngle);
        }
    }
    
    void StartRotation(float angle)
    {
        targetYRotation += angle;
        // Normalize to 0-360 range
        while (targetYRotation < 0) targetYRotation += 360f;
        while (targetYRotation >= 360) targetYRotation -= 360f;
        
        isRotating = true;
    }
    
    void UpdateCameraPosition()
    {
        if (trainingCamera == null) return;
        
        // Calculate camera position based on target position and height
        Vector3 cameraPosition = targetPosition + Vector3.up * currentHeight;
        
        // Apply viewing angle offset
        float angleRad = viewingAngle * Mathf.Deg2Rad;
        Vector3 angleOffset = new Vector3(0, 0, -currentHeight * Mathf.Cos(angleRad));
        angleOffset = Quaternion.Euler(0, targetYRotation, 0) * angleOffset;
        cameraPosition += angleOffset;
        
        // Set camera position
        trainingCamera.transform.position = cameraPosition;
        
        // Handle rotation
        float currentYRotation = trainingCamera.transform.eulerAngles.y;
        if (isRotating)
        {
            // Smooth rotation
            float newYRotation = Mathf.MoveTowardsAngle(currentYRotation, targetYRotation, rotationSpeed * Time.deltaTime);
            trainingCamera.transform.rotation = Quaternion.Euler(viewingAngle, newYRotation, 0);
            
            // Check if rotation is complete
            if (Mathf.Abs(Mathf.DeltaAngle(newYRotation, targetYRotation)) < 1f)
            {
                isRotating = false;
                trainingCamera.transform.rotation = Quaternion.Euler(viewingAngle, targetYRotation, 0);
            }
        }
        else
        {
            // Set fixed angle
            trainingCamera.transform.rotation = Quaternion.Euler(viewingAngle, targetYRotation, 0);
        }
    }
    
    void SetCameraAngle()
    {
        if (trainingCamera != null)
        {
            trainingCamera.transform.rotation = Quaternion.Euler(viewingAngle, 0f, 0f);
        }
    }
    
    void ShowDebugInfo()
    {
        if (trainingCamera == null) return;
        
        Vector3 cameraPos = trainingCamera.transform.position;
        
        // Draw camera direction
        Debug.DrawRay(cameraPos, trainingCamera.transform.forward * 20f, Color.blue);
        
        // Draw target position
        Debug.DrawLine(targetPosition + Vector3.up * 2f, targetPosition - Vector3.up * 2f, Color.red);
        Debug.DrawLine(targetPosition + Vector3.left * 2f, targetPosition + Vector3.right * 2f, Color.red);
        Debug.DrawLine(targetPosition + Vector3.forward * 2f, targetPosition + Vector3.back * 2f, Color.red);
        
        // Show current checkpoints if available
        var checkpoints = GetCurrentTrainingCheckpoints();
        for (int i = 0; i < checkpoints.Count; i++)
        {
            Color color = i == 0 ? Color.green : (i == 1 ? Color.yellow : Color.cyan);
            Vector3 pos = checkpoints[i];
            Debug.DrawLine(pos + Vector3.up * 5f, pos - Vector3.up * 5f, color);
        }
    }
    
    // Helper methods for checkpoint detection
    List<Vector3> GetCurrentTrainingCheckpoints()
    {
        List<Vector3> positions = new List<Vector3>();
        
        if (trainer == null || trainer.TrainingSessions == null) return positions;
        
        int sessionIndex = trainer.CurrentSessionIndex;
        if (sessionIndex >= 0 && sessionIndex < trainer.TrainingSessions.Count)
        {
            var session = trainer.TrainingSessions[sessionIndex];
            
            Vector3 startPos = GetCheckpointPosition(session.startCheckpoint);
            Vector3 goalPos = GetCheckpointPosition(session.goalCheckpoint);
            Vector3 validatePos = GetCheckpointPosition(session.validateCheckpoint);
            
            if (startPos != Vector3.zero) positions.Add(startPos);
            if (goalPos != Vector3.zero) positions.Add(goalPos);
            if (validatePos != Vector3.zero) positions.Add(validatePos);
        }
        
        return positions;
    }
    
    Vector3 GetCheckpointPosition(int checkpointId)
    {
        try
        {
            Vector3 pos = TrackMaster.GetTrainingSpawnPosition(checkpointId, TrackMaster.GetCurrentRaceline());
            if (pos != Vector3.zero) return pos;
        }
        catch { }
        
        GameObject[] checkpoints = GameObject.FindGameObjectsWithTag("Checkpoint");
        foreach (var checkpoint in checkpoints)
        {
            var checkpointComponent = checkpoint.GetComponent<Checkpoint>();
            if (checkpointComponent != null && checkpointComponent.GetCheckpointId() == checkpointId)
            {
                return checkpoint.transform.position;
            }
        }
        
        Checkpoint[] allCheckpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
        foreach (var checkpointComponent in allCheckpoints)
        {
            if (checkpointComponent.GetCheckpointId() == checkpointId)
            {
                return checkpointComponent.transform.position;
            }
        }
        
        return Vector3.zero;
    }
    
    Vector3 CalculateCenter(List<Vector3> positions)
    {
        if (positions.Count == 0) return Vector3.zero;
        
        Vector3 sum = Vector3.zero;
        foreach (var pos in positions)
        {
            sum += pos;
        }
        return sum / positions.Count;
    }
    
    // Public methods for external control
    public void SetCameraActive(bool active)
    {
        if (trainingCamera != null)
        {
            trainingCamera.gameObject.SetActive(active);
        }
    }
    
    public void FocusOnPosition(Vector3 position)
    {
        targetPosition = position;
    }
    
    public void PositionRelativeToSpawn(Vector3 spawnPosition)
    {
        // Position camera relative to motorcycle spawn: same X, calculated Y, Z minus 100
        targetPosition = new Vector3(
            spawnPosition.x,           // Same X as spawn
            20f,                        // Y will be set by currentHeight  
            spawnPosition.z - 30f     // Z minus 100 from spawn
        );
        
        Debug.Log($"Camera repositioned relative to spawn: Spawn={spawnPosition}, Camera Target={targetPosition}");
    }
    
    public void FocusOnCheckpoints(int startId, int goalId, int validateId)
    {
        List<Vector3> positions = new List<Vector3>();
        positions.Add(GetCheckpointPosition(startId));
        positions.Add(GetCheckpointPosition(goalId));
        positions.Add(GetCheckpointPosition(validateId));
        
        if (positions.Count > 0)
        {
            targetPosition = CalculateCenter(positions);
        }
    }
    
    public void ResetCamera()
    {
        targetPosition = Vector3.zero;
        targetYRotation = 0f;
        currentHeight = 50f;
        isRotating = false;
    }
    
    public void RepositionToCurrentSpawn()
    {
        // Reposition camera to current training spawn position
        var raceline = TrackMaster.GetCurrentRaceline();
        if (raceline != null && raceline.Count > 0)
        {
            Vector3 spawnPosition = TrackMaster.GetTrainingSpawnPosition(0, raceline);
            PositionRelativeToSpawn(spawnPosition);
            Debug.Log($"Camera repositioned to spawn: {spawnPosition}");
        }
        else
        {
            Debug.LogWarning("Cannot reposition camera - track not loaded");
        }
    }
}
