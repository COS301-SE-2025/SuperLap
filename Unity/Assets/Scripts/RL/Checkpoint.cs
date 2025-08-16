using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [SerializeField] private int checkpointId;
    [SerializeField] private bool isActive = false;
    
    private Renderer checkpointRenderer;
    private Collider checkpointCollider;
    
    // Static checkpoint management
    private static CheckpointManager manager;
    
    void Awake()
    {
        checkpointRenderer = GetComponent<Renderer>();
        checkpointCollider = GetComponent<Collider>();
        
        // Ensure the collider is a trigger
        if (checkpointCollider != null)
        {
            checkpointCollider.isTrigger = true;
        }
        
        // Find or create the checkpoint manager
        if (manager == null)
        {
            manager = FindAnyObjectByType<CheckpointManager>();
            if (manager == null)
            {
                GameObject managerObject = new GameObject("CheckpointManager");
                manager = managerObject.AddComponent<CheckpointManager>();
            }
        }
    }
    
    void Start()
    {
        // Register this checkpoint with the manager
        manager.RegisterCheckpoint(this);
        
        // Initially set visibility based on whether this should be visible
        UpdateVisibility();
    }
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Checkpoint {checkpointId} triggered by {other.gameObject.name}");
        // Check if the object has the Player tag
        if (other.CompareTag("Player") && isActive)
        {
            // Notify manager but don't auto-complete for training
            manager.CheckpointTriggered(checkpointId, other.gameObject);
        }
    }
    
    public void SetCheckpointId(int id)
    {
        checkpointId = id;
    }
    
    public int GetCheckpointId()
    {
        return checkpointId;
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        UpdateVisibility();
    }
    
    public bool IsActive()
    {
        return isActive;
    }
    
    public void SetMaterial(Material material)
    {
        if (checkpointRenderer != null)
        {
            checkpointRenderer.material = material;
        }
    }
    
    private void UpdateVisibility()
    {
        if (checkpointRenderer != null)
        {
            checkpointRenderer.enabled = isActive;
        }
        
        if (checkpointCollider != null)
        {
            checkpointCollider.enabled = isActive;
        }
    }
    
    public void MarkCompleted()
    {
        // Make the checkpoint disappear
        SetActive(false);
        
        // Optional: Add effects here (particle system, sound, etc.)
        Debug.Log($"Checkpoint {checkpointId} completed!");
    }
}
