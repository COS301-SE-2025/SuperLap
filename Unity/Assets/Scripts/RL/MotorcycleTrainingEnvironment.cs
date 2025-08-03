using UnityEngine;
using RLMatrix;
using System.Collections.Generic;
using System.Reflection;
using RLMatrix.Toolkit;

[RLMatrixEnvironment]
public partial class MotorcycleTrainingEnvironment : MonoBehaviour
{
    [Header("Training Environment Settings")]
    [SerializeField] private int maxAgents = 4;
    [SerializeField] private GameObject motorcycleAgentPrefab;
    [SerializeField] private bool useRandomSpawnPositions = true;
    [SerializeField] private float agentSpawnHeight = 1.0f;
    [SerializeField] private float agentSpacing = 2f; // Distance between agents when spawning multiple
    
    [Header("Episode Management")]
    [SerializeField] private float maxEpisodeTime = 120f;
    [SerializeField] private bool resetAllAgentsSimultaneously = true;
    [SerializeField] private bool visualizeTraining = true;
    
    [Header("Training Progress Tracking")]
    [SerializeField] private int episodeCount = 0;
    [SerializeField] private float averageReward = 0f;
    [SerializeField] private float bestEpisodeReward = float.MinValue;
    [SerializeField] private float worstEpisodeReward = float.MaxValue;
    
    private List<MotorcycleRLMatrixAgent> activeAgents = new List<MotorcycleRLMatrixAgent>();
    private List<float> recentRewards = new List<float>();
    private const int REWARD_HISTORY_SIZE = 100;
    private bool trackLoaded = false;
    
    void Start()
    {
        // Don't initialize immediately - wait for track to be loaded
        CheckForTrackMaster();
    }
    
    void Update()
    {
        // Continuously check if track has been loaded and we haven't initialized yet
        if (!trackLoaded)
        {
            CheckForTrackMaster();
        }
    }
    
    private void CheckForTrackMaster()
    {
        // Check if TrackMaster is available and has a raceline
        if (TrackMaster.instance != null)
        {
            List<Vector2> raceline = TrackMaster.GetCurrentRaceline();
            if (raceline != null && raceline.Count > 0)
            {
                Debug.Log("Track loaded! Initializing training environment...");
                trackLoaded = true;
                InitializeTrainingEnvironment();
            }
        }
    }
    
    private void InitializeTrainingEnvironment()
    {
        Debug.Log("Initializing Motorcycle Training Environment...");
        
        // Verify we have a valid raceline from TrackMaster
        List<Vector2> raceline = TrackMaster.GetCurrentRaceline();
        if (raceline == null || raceline.Count == 0)
        {
            Debug.LogError("Cannot initialize training environment: No raceline available from TrackMaster!");
            return;
        }
        
        // Create agents at track positions
        SpawnAgents();
        
        Debug.Log($"Training environment initialized with {activeAgents.Count} agents");
    }
    
    private void SpawnAgents()
    {
        if (motorcycleAgentPrefab == null)
        {
            Debug.LogError("Motorcycle Agent Prefab is not assigned!");
            return;
        }
        
        List<Vector2> raceline = TrackMaster.GetCurrentRaceline();
        if (raceline == null || raceline.Count == 0)
        {
            Debug.LogError("No raceline available for agent spawning!");
            return;
        }
        
        for (int i = 0; i < maxAgents; i++)
        {
            Vector3 spawnPosition = GetTrackSpawnPosition(i, raceline);
            Vector3 spawnDirection = GetTrackSpawnDirection(i, raceline);
            
            GameObject agentObj = Instantiate(motorcycleAgentPrefab, spawnPosition, Quaternion.LookRotation(spawnDirection));
            agentObj.name = $"MotorcycleAgent_{i}";
            agentObj.transform.SetParent(transform);
            
            MotorcycleRLMatrixAgent agent = agentObj.GetComponent<MotorcycleRLMatrixAgent>();
            if (agent != null)
            {
                activeAgents.Add(agent);
                
                // Configure the agent's starting position
                ConfigureAgentStartingPosition(agent, spawnPosition, spawnDirection);
                
                // Subscribe to agent events for tracking
                agent.OnEpisodeEndEvent += OnAgentEpisodeEnd;
                
                Debug.Log($"Spawned agent {i} at track position: {spawnPosition}");
            }
            else
            {
                Debug.LogError($"Agent {i} does not have MotorcycleRLMatrixAgent component!");
            }
        }
    }
    
    private Vector3 GetTrackSpawnPosition(int agentIndex, List<Vector2> raceline)
    {
        int positionIndex;
        
        if (useRandomSpawnPositions)
        {
            // Random position along the track
            positionIndex = Random.Range(0, raceline.Count);
        }
        else
        {
            // Spread agents evenly along the track
            float spacing = (float)raceline.Count / maxAgents;
            positionIndex = Mathf.RoundToInt(agentIndex * spacing) % raceline.Count;
        }
        
        Vector2 racelinePoint = raceline[positionIndex];
        
        // Add some lateral offset if multiple agents to prevent collisions
        Vector3 basePosition = new Vector3(racelinePoint.x, agentSpawnHeight, racelinePoint.y);
        
        if (maxAgents > 1)
        {
            // Get track direction for lateral offset
            Vector3 trackDirection = GetTrackSpawnDirection(agentIndex, raceline);
            Vector3 lateralOffset = Vector3.Cross(trackDirection, Vector3.up) * (agentIndex - (maxAgents - 1) / 2f) * agentSpacing;
            basePosition += lateralOffset;
        }
        
        return basePosition;
    }
    
    private Vector3 GetTrackSpawnDirection(int agentIndex, List<Vector2> raceline)
    {
        int positionIndex;
        
        if (useRandomSpawnPositions)
        {
            positionIndex = Random.Range(0, raceline.Count);
        }
        else
        {
            float spacing = (float)raceline.Count / maxAgents;
            positionIndex = Mathf.RoundToInt(agentIndex * spacing) % raceline.Count;
        }
        
        // Look ahead several points for smoother direction calculation
        int lookAheadDistance = Mathf.Max(1, raceline.Count / 20); // ~5% of track length
        int endIndex = (positionIndex + lookAheadDistance) % raceline.Count;
        
        Vector2 point1 = raceline[positionIndex];
        Vector2 point2 = raceline[endIndex];
        
        Vector2 direction2D = point2 - point1;
        
        // Handle case where points are too close (circular track wrap-around)
        if (direction2D.magnitude < 0.1f)
        {
            endIndex = (positionIndex + 1) % raceline.Count;
            point2 = raceline[endIndex];
            direction2D = point2 - point1;
        }
        
        // Convert to 3D direction
        Vector3 direction3D = new Vector3(direction2D.x, 0, direction2D.y).normalized;
        
        return direction3D != Vector3.zero ? direction3D : Vector3.forward;
    }
    
    private void ConfigureAgentStartingPosition(MotorcycleRLMatrixAgent agent, Vector3 position, Vector3 direction)
    {
        // Use reflection to set the starting position and rotation fields
        var startingPositionField = typeof(MotorcycleRLMatrixAgent).GetField("startingPosition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var startingRotationField = typeof(MotorcycleRLMatrixAgent).GetField("startingRotation", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (startingPositionField != null)
            startingPositionField.SetValue(agent, position);
        if (startingRotationField != null)
            startingRotationField.SetValue(agent, direction);
    }
    
    private void OnAgentEpisodeEnd(float episodeReward)
    {
        episodeCount++;
        
        // Track reward statistics
        recentRewards.Add(episodeReward);
        if (recentRewards.Count > REWARD_HISTORY_SIZE)
        {
            recentRewards.RemoveAt(0);
        }
        
        // Update best/worst rewards
        if (episodeReward > bestEpisodeReward)
        {
            bestEpisodeReward = episodeReward;
        }
        if (episodeReward < worstEpisodeReward)
        {
            worstEpisodeReward = episodeReward;
        }
        
        // Calculate average reward
        float totalReward = 0f;
        foreach (float reward in recentRewards)
        {
            totalReward += reward;
        }
        averageReward = totalReward / recentRewards.Count;
        
        // Log progress periodically
        if (episodeCount % 50 == 0)
        {
            Debug.Log($"Episode {episodeCount} - Avg Reward: {averageReward:F2}, Best: {bestEpisodeReward:F2}, Worst: {worstEpisodeReward:F2}");
        }
    }
    
    public void ResetEnvironment()
    {
        Debug.Log("Resetting training environment...");
        
        foreach (var agent in activeAgents)
        {
            if (agent != null)
            {
                agent.ResetEpisode();
            }
        }
    }
    
    public void ResetSingleAgent(MotorcycleRLMatrixAgent agent)
    {
        if (agent != null && activeAgents.Contains(agent))
        {
            agent.ResetEpisode();
        }
    }
    
    // Public getters for training statistics
    public int EpisodeCount => episodeCount;
    public float AverageReward => averageReward;
    public float BestEpisodeReward => bestEpisodeReward;
    public float WorstEpisodeReward => worstEpisodeReward;
    public int ActiveAgentCount => activeAgents.Count;
    
    private float CalculateAverageReward()
    {
        if (recentRewards.Count == 0) return 0f;
        
        float sum = 0f;
        foreach (float reward in recentRewards)
        {
            sum += reward;
        }
        return sum / recentRewards.Count;
    }
    
    void OnDrawGizmos()
    {
        // Draw agent spawn visualization
        if (TrackMaster.instance != null)
        {
            List<Vector2> raceline = TrackMaster.GetCurrentRaceline();
            if (raceline != null && raceline.Count > 0)
            {
                Gizmos.color = Color.cyan;
                
                // Draw potential spawn positions
                for (int i = 0; i < maxAgents; i++)
                {
                    Vector3 spawnPos = GetTrackSpawnPosition(i, raceline);
                    Vector3 spawnDir = GetTrackSpawnDirection(i, raceline);
                    
                    Gizmos.DrawWireSphere(spawnPos, 1f);
                    Gizmos.DrawRay(spawnPos, spawnDir * 3f);
                }
            }
        }
        
        // Draw training environment bounds
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 5f);
    }
}
