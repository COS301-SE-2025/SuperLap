using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

/// <summary>
/// Distance-based checkpoint system for multithreaded training
/// Replaces Unity's trigger colliders with geometric distance calculations
/// Thread-safe and optimized for concurrent agent processing
/// </summary>
public class DistanceCheckpointSystem
{
    private readonly List<CheckpointData> checkpoints;
    private readonly ConcurrentDictionary<int, AgentCheckpointState> agentStates;
    private readonly TrackMeshSystem trackMesh;
    
    public int TotalCheckpoints => checkpoints.Count;
    
    public DistanceCheckpointSystem(List<Vector2D> racelinePoints, TrackMeshSystem trackMesh, int checkpointCount = 50)
    {
        this.trackMesh = trackMesh ?? throw new ArgumentNullException(nameof(trackMesh));
        this.agentStates = new ConcurrentDictionary<int, AgentCheckpointState>();
        
        // Create checkpoints along the raceline
        checkpoints = CreateCheckpointsFromRaceline(racelinePoints, checkpointCount);
    }
    
    /// <summary>
    /// Check if an agent has triggered any checkpoints and update their progress
    /// Returns the checkpoint ID if triggered, -1 if none
    /// </summary>
    public int UpdateAgentProgress(int agentId, Vector2D agentPosition)
    {
        // Get or create agent state
        var agentState = agentStates.GetOrAdd(agentId, _ => new AgentCheckpointState());
        
        int targetCheckpoint = agentState.NextTargetCheckpoint;
        
        // Check if agent is within range of target checkpoint
        if (IsAgentAtCheckpoint(agentPosition, targetCheckpoint))
        {
            // Validate that this is the correct next checkpoint (prevent skipping)
            if (targetCheckpoint == agentState.NextTargetCheckpoint)
            {
                agentState.CheckpointTriggered(targetCheckpoint, checkpoints.Count);
                agentStates.TryUpdate(agentId, agentState, agentStates[agentId]);
                return targetCheckpoint;
            }
        }
        
        return -1; // No checkpoint triggered
    }
    
    /// <summary>
    /// Reset an agent's checkpoint progress
    /// </summary>
    public void ResetAgent(int agentId)
    {
        agentStates.AddOrUpdate(agentId, new AgentCheckpointState(), (key, oldValue) => new AgentCheckpointState());
    }
    
    /// <summary>
    /// Get the current target checkpoint for an agent
    /// </summary>
    public int GetNextTargetCheckpoint(int agentId)
    {
        return agentStates.GetOrAdd(agentId, _ => new AgentCheckpointState()).NextTargetCheckpoint;
    }
    
    /// <summary>
    /// Get the total number of checkpoints completed by an agent
    /// </summary>
    public int GetCheckpointsCompleted(int agentId)
    {
        return agentStates.GetOrAdd(agentId, _ => new AgentCheckpointState()).CheckpointsCompleted;
    }
    
    /// <summary>
    /// Check if an agent has completed a full lap
    /// </summary>
    public bool HasCompletedLap(int agentId)
    {
        var state = agentStates.GetOrAdd(agentId, _ => new AgentCheckpointState());
        return state.CheckpointsCompleted >= checkpoints.Count;
    }
    
    /// <summary>
    /// Get checkpoint position for visualization
    /// </summary>
    public Vector2D GetCheckpointPosition(int checkpointId)
    {
        if (checkpointId >= 0 && checkpointId < checkpoints.Count)
        {
            return checkpoints[checkpointId].position;
        }
        return Vector2D.Zero;
    }
    
    /// <summary>
    /// Get checkpoint detection radius
    /// </summary>
    public float GetCheckpointRadius(int checkpointId)
    {
        if (checkpointId >= 0 && checkpointId < checkpoints.Count)
        {
            return checkpoints[checkpointId].detectionRadius;
        }
        return 0f;
    }
    
    /// <summary>
    /// Get all checkpoint positions for visualization
    /// </summary>
    public List<Vector2D> GetAllCheckpointPositions()
    {
        var positions = new List<Vector2D>();
        foreach (var checkpoint in checkpoints)
        {
            positions.Add(checkpoint.position);
        }
        return positions;
    }
    
    /// <summary>
    /// Get progress percentage for an agent (0-1)
    /// </summary>
    public float GetProgressPercentage(int agentId)
    {
        var state = agentStates.GetOrAdd(agentId, _ => new AgentCheckpointState());
        return (float)state.CheckpointsCompleted / checkpoints.Count;
    }
    
    #region Private Methods
    
    private bool IsAgentAtCheckpoint(Vector2D agentPosition, int checkpointId)
    {
        if (checkpointId < 0 || checkpointId >= checkpoints.Count)
        {
            return false;
        }
        
        var checkpoint = checkpoints[checkpointId];
        float distance = Vector2D.Distance(agentPosition, checkpoint.position);
        return distance <= checkpoint.detectionRadius;
    }
    
    private List<CheckpointData> CreateCheckpointsFromRaceline(List<Vector2D> racelinePoints, int checkpointCount)
    {
        var checkpointList = new List<CheckpointData>();
        
        if (racelinePoints == null || racelinePoints.Count == 0)
        {
            return checkpointList;
        }
        
        // Calculate spacing between checkpoints
        int spacing = Math.Max(1, racelinePoints.Count / checkpointCount);
        
        for (int i = 0; i < checkpointCount && i * spacing < racelinePoints.Count; i++)
        {
            int racelineIndex = (i * spacing) % racelinePoints.Count;
            Vector2D position = racelinePoints[racelineIndex];
            
            // Calculate detection radius as half the track width at this position
            float trackWidth = trackMesh.GetTrackWidthAtPosition(position);
            float detectionRadius = Math.Max(trackWidth * 0.5f, 3.0f); // Minimum 3 meters
            
            checkpointList.Add(new CheckpointData
            {
                checkpointId = i,
                position = position,
                detectionRadius = detectionRadius,
                racelineIndex = racelineIndex
            });
        }
        
        return checkpointList;
    }
    
    #endregion
}

/// <summary>
/// Data structure representing a single checkpoint
/// </summary>
[System.Serializable]
public struct CheckpointData
{
    public int checkpointId;
    public Vector2D position;
    public float detectionRadius;
    public int racelineIndex; // Index in the original raceline for reference
    
    public CheckpointData(int id, Vector2D pos, float radius, int racelineIdx = -1)
    {
        checkpointId = id;
        position = pos;
        detectionRadius = radius;
        racelineIndex = racelineIdx;
    }
}

/// <summary>
/// Thread-safe agent checkpoint state tracking
/// </summary>
public class AgentCheckpointState
{
    private int nextTargetCheckpoint;
    private int checkpointsCompleted;
    private float lastTriggerTime;
    private readonly object lockObject = new object();
    
    public int NextTargetCheckpoint
    {
        get
        {
            lock (lockObject)
            {
                return nextTargetCheckpoint;
            }
        }
    }
    
    public int CheckpointsCompleted
    {
        get
        {
            lock (lockObject)
            {
                return checkpointsCompleted;
            }
        }
    }
    
    public float LastTriggerTime
    {
        get
        {
            lock (lockObject)
            {
                return lastTriggerTime;
            }
        }
    }
    
    public AgentCheckpointState()
    {
        nextTargetCheckpoint = 0;
        checkpointsCompleted = 0;
        lastTriggerTime = 0f;
    }
    
    /// <summary>
    /// Record that a checkpoint was triggered
    /// </summary>
    public void CheckpointTriggered(int checkpointId, int totalCheckpoints)
    {
        lock (lockObject)
        {
            if (checkpointId == nextTargetCheckpoint)
            {
                checkpointsCompleted++;
                nextTargetCheckpoint = (nextTargetCheckpoint + 1) % totalCheckpoints;
                lastTriggerTime = GetCurrentTime();
            }
        }
    }
    
    /// <summary>
    /// Reset checkpoint progress
    /// </summary>
    public void Reset()
    {
        lock (lockObject)
        {
            nextTargetCheckpoint = 0;
            checkpointsCompleted = 0;
            lastTriggerTime = 0f;
        }
    }
    
    /// <summary>
    /// Set target checkpoint manually (for training scenarios)
    /// </summary>
    public void SetTargetCheckpoint(int checkpointId)
    {
        lock (lockObject)
        {
            nextTargetCheckpoint = checkpointId;
        }
    }
    
    private float GetCurrentTime()
    {
        // Use high-resolution timer for better precision
        return (float)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
    }
}

/// <summary>
/// Advanced checkpoint system with sector timing and performance analysis
/// </summary>
public class AdvancedCheckpointSystem : DistanceCheckpointSystem
{
    private readonly ConcurrentDictionary<int, List<float>> agentSectorTimes;
    private readonly int sectorsPerLap;
    
    public AdvancedCheckpointSystem(List<Vector2D> racelinePoints, TrackMeshSystem trackMesh, 
                                  int checkpointCount = 50, int sectorsPerLap = 3) 
        : base(racelinePoints, trackMesh, checkpointCount)
    {
        this.sectorsPerLap = sectorsPerLap;
        this.agentSectorTimes = new ConcurrentDictionary<int, List<float>>();
    }
    
    /// <summary>
    /// Get sector times for an agent
    /// </summary>
    public List<float> GetSectorTimes(int agentId)
    {
        return agentSectorTimes.GetOrAdd(agentId, _ => new List<float>());
    }
    
    /// <summary>
    /// Calculate current sector for a checkpoint
    /// </summary>
    public int GetSectorForCheckpoint(int checkpointId)
    {
        int checkpointsPerSector = TotalCheckpoints / sectorsPerLap;
        return checkpointId / checkpointsPerSector;
    }
    
    /// <summary>
    /// Record sector time when crossing sector boundaries
    /// </summary>
    public void RecordSectorTime(int agentId, int sector, float time)
    {
        var sectorTimes = agentSectorTimes.GetOrAdd(agentId, _ => new List<float>());
        
        // Ensure list is large enough
        while (sectorTimes.Count <= sector)
        {
            sectorTimes.Add(0f);
        }
        
        sectorTimes[sector] = time;
    }
}