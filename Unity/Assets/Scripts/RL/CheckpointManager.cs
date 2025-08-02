using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [Header("Checkpoint Materials")]
    [SerializeField] private Material[] checkpointMaterials = new Material[3];

    [Header("Checkpoint Settings")]
    [SerializeField] private int maxVisibleCheckpoints = 3;
    [SerializeField] private int currentTargetCheckpoint = 0;

    private List<Checkpoint> allCheckpoints = new List<Checkpoint>();
    private bool systemInitialized = false;

    void Awake()
    {
        // Ensure only one CheckpointManager exists
        CheckpointManager[] managers = FindObjectsByType<CheckpointManager>(FindObjectsSortMode.None);
        if (managers.Length > 1)
        {
            Debug.LogWarning("Multiple CheckpointManagers found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    public void RegisterCheckpoint(Checkpoint checkpoint)
    {
        if (!allCheckpoints.Contains(checkpoint))
        {
            allCheckpoints.Add(checkpoint);

            // Sort checkpoints by their IDs
            allCheckpoints = allCheckpoints.OrderBy(c => c.GetCheckpointId()).ToList();

            // Update the display after registration
            if (!systemInitialized)
            {
                // Wait for all checkpoints to register, then initialize
                Invoke(nameof(InitializeCheckpointSystem), 0.1f);
            }
        }
    }

    private void InitializeCheckpointSystem()
    {
        if (systemInitialized) return;

        systemInitialized = true;
        UpdateCheckpointVisibility();

        Debug.Log($"Checkpoint system initialized with {allCheckpoints.Count} checkpoints");
    }

    public void CheckpointTriggered(int checkpointId)
    {
        // Validate that this is the correct next checkpoint
        if (checkpointId != currentTargetCheckpoint)
        {
            Debug.LogWarning($"Player attempted to skip checkpoint! Expected: {currentTargetCheckpoint}, Triggered: {checkpointId}");
            return; // Don't allow checkpoint skipping
        }

        // Mark the checkpoint as completed
        Checkpoint triggeredCheckpoint = allCheckpoints.FirstOrDefault(c => c.GetCheckpointId() == checkpointId);
        if (triggeredCheckpoint != null)
        {
            triggeredCheckpoint.MarkCompleted();
        }

        // Move to next checkpoint
        currentTargetCheckpoint++;

        // Handle wrap-around for circular tracks
        if (currentTargetCheckpoint >= allCheckpoints.Count)
        {
            currentTargetCheckpoint = 0;
            Debug.Log("Lap completed! Starting new lap.");
        }

        // Update visibility
        UpdateCheckpointVisibility();

        Debug.Log($"Checkpoint {checkpointId} triggered. Next target: {currentTargetCheckpoint}");
    }

    private void UpdateCheckpointVisibility()
    {
        if (allCheckpoints.Count == 0) return;

        // First, make all checkpoints inactive
        foreach (var checkpoint in allCheckpoints)
        {
            checkpoint.SetActive(false);
        }

        // Then activate and set materials for the next few checkpoints
        for (int i = 0; i < maxVisibleCheckpoints && i < allCheckpoints.Count; i++)
        {
            int checkpointIndex = (currentTargetCheckpoint + i) % allCheckpoints.Count;
            Checkpoint checkpoint = allCheckpoints[checkpointIndex];

            checkpoint.SetActive(true);

            // Set material if available
            if (i < checkpointMaterials.Length && checkpointMaterials[i] != null)
            {
                checkpoint.SetMaterial(checkpointMaterials[i]);
            }
        }
    }

    public void ResetCheckpoints()
    {
        currentTargetCheckpoint = 0;
        UpdateCheckpointVisibility();
        Debug.Log("Checkpoints reset to beginning");
    }

    public void SetTargetCheckpoint(int checkpointId)
    {
        if (checkpointId >= 0 && checkpointId < allCheckpoints.Count)
        {
            currentTargetCheckpoint = checkpointId;
            UpdateCheckpointVisibility();
            Debug.Log($"Target checkpoint set to: {checkpointId}");
        }
        else
        {
            Debug.LogWarning($"Invalid checkpoint ID: {checkpointId}");
        }
    }

    public int GetCurrentTargetCheckpoint()
    {
        return currentTargetCheckpoint;
    }

    public int GetTotalCheckpoints()
    {
        return allCheckpoints.Count;
    }

    public void SetCheckpointMaterials(Material[] materials)
    {
        checkpointMaterials = materials;
        UpdateCheckpointVisibility();
    }

    public void SetMaxVisibleCheckpoints(int count)
    {
        maxVisibleCheckpoints = Mathf.Max(1, count);
        UpdateCheckpointVisibility();
    }

    // Method to manually trigger checkpoint (for testing)
    public void TriggerCheckpoint(int checkpointId)
    {
        CheckpointTriggered(checkpointId);
    }
}
