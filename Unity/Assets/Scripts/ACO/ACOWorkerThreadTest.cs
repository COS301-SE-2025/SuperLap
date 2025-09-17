using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

public class ACOWorkerThreadTest : MonoBehaviour
{
    List<ACOWorkerThread2> workers;
    private bool running = false;
    private int c = 0;
    [SerializeField] private int checkpointCount = 10;
    List<AgentContainer> bestAgents = new();
    float waitTimer = 0.05f;
    int retryCounter = 5;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (running) return;
            InitializeSystem(c);
            running = true;
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            StartSystem();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            StopSystem();
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Checking for best solutions...");
            workers.ForEach((wt) =>
            {
                AgentContainer best = wt.BestAgent;

                if (best != null)
                {
                    Debug.Log("Found a best solution!");
                }
            });
        }
        else if(Input.GetKeyDown(KeyCode.Z))
        {
            string filePath = Path.Combine(Application.persistentDataPath, "bestAgent.txt");
            GetComponent<ACOAgentReplay>().InitializeTextFile(filePath);
        }
        
        if (running)
        {
            bool allDone = workers.Count((w) => w.IsRunning) == 0;

            if (allDone)
            {
                if(waitTimer > 0.0f)
                {
                    waitTimer -= Time.deltaTime;
                    return;
                }

                running = false;
                waitTimer = 0.05f;

                // Check if we've completed all splits
                if (c >= checkpointCount - 2)
                {
                    Debug.Log("Training completed!");
                    SaveBestAgentToFile();
                    c = 0;
                    return;
                }

                // Collect solutions from current split
                List<AgentContainer> currentSolutions = new();
                workers.ForEach((w) =>
                {
                    if (w.BestAgent != null)
                    {
                        currentSolutions.Add(w.BestAgent);
                    }
                });

                if (currentSolutions.Count == 0)
                {
                    // No solution found for current split
                    if (retryCounter > 0)
                    {
                        // Retry current split
                        Debug.LogWarning($"Could not find solution for split {c}. Retrying. Attempts remaining: {retryCounter}");
                        retryCounter--;
                        InitializeSystem(c);
                        running = true;
                        return;
                    }
                    else
                    {
                        // Backtrack if possible
                        if (c > 0 && bestAgents.Count > 0)
                        {
                            Debug.LogWarning($"Could not find solution for split {c} after all retries. Backing up to split {c - 1}.");
                            
                            // Remove the last successful agent (corresponds to split c-1)
                            bestAgents.RemoveAt(bestAgents.Count - 1);
                            c--;
                            retryCounter = 5;
                            
                            InitializeSystem(c);
                            running = true;
                            return;
                        }
                        else
                        {
                            // Cannot backtrack further - training failed
                            Debug.LogError("Training failed: Could not find solution for initial split and cannot backtrack further.");
                            c = 0;
                            bestAgents.Clear();
                            return;
                        }
                    }
                }
                else
                {
                    // Solution found - select best and move to next split
                    AgentContainer bestAgent = currentSolutions.OrderBy((ac) => ac.TotalSteps).First();
                    bestAgents.Add(bestAgent);
                    
                    retryCounter = 5; // Reset retry counter
                    c++;
                    
                    Debug.Log($"Split {c - 1} completed successfully. Moving to split {c}");
                    InitializeSystem(c);
                    running = true;
                }
            }
        }
    }

    private void SaveBestAgentToFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "bestAgent.txt");

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Write initial state as first line
            List<System.Numerics.Vector2> threadRl = ACOTrackMaster.GetCurrentRaceline();
            Vector3 startPos = ACOTrackMaster.GetTrainingSpawnPosition(0, threadRl);
            Vector3 startDir = ACOTrackMaster.GetTrainingSpawnDirection(0, threadRl);
            float bearing = CalculateBearing(new System.Numerics.Vector2(startDir.x, startDir.z));
            
            writer.WriteLine($"{startPos.x}:{startPos.z}:{bearing}");
            
            // Write all inputs from all segments
            bestAgents.ForEach((agent) =>
            {
                agent.Inputs.ForEach((input) =>
                {
                    writer.WriteLine($"{input.Item1}:{input.Item2}");
                });
            });
        }
        
        Debug.Log($"Best agent path saved to: {filePath}");
    }
    
    List<System.Numerics.Vector2> InitializeDistanceBasedCheckpoints()
    {
        List<System.Numerics.Vector2> checkpointPositions = new List<System.Numerics.Vector2>();
        
        // Get raceline data from ACOTrackMaster
        List<System.Numerics.Vector2> raceline = ACOTrackMaster.GetCurrentRaceline();
        
        // Calculate total raceline distance
        float totalRacelineDistance = 0f;
        Vector3 previousPoint = new Vector3(raceline[0].X, 0, raceline[0].Y);

        for (int i = 1; i < raceline.Count; i++)
        {
            Vector3 currentPoint = new Vector3(raceline[i].X, 0, raceline[i].Y);
            totalRacelineDistance += Vector3.Distance(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
        
        // Distribute checkpoints evenly along the raceline
        int totalCheckpoints = checkpointCount;
        float distancePerCheckpoint = totalRacelineDistance / totalCheckpoints;
        
        float currentDistance = 0f;
        int racelineIndex = 0;
        System.Numerics.Vector2 lastRacelinePoint = raceline[0];

        for (int checkpointIndex = 0; checkpointIndex < totalCheckpoints; checkpointIndex++)
        {
            float targetDistance = checkpointIndex * distancePerCheckpoint;

            // Find the raceline point closest to our target distance
            while (currentDistance < targetDistance && racelineIndex < raceline.Count - 1)
            {
                racelineIndex++;
                System.Numerics.Vector2 nextPoint = raceline[racelineIndex];
                currentDistance += System.Numerics.Vector2.Distance(lastRacelinePoint, nextPoint);
                lastRacelinePoint = nextPoint;
            }

            checkpointPositions.Add(lastRacelinePoint);

            // Debug.Log($"Distance-based checkpoint {checkpointIndex} placed at distance {targetDistance:F2} at position {lastRacelinePoint}");
        }

        return checkpointPositions;
    }

    private void InitializeSystem(int currentSplit)
    {
        // Validate split index
        var checkPoints = InitializeDistanceBasedCheckpoints();
        
        if (currentSplit < 0)
        {
            Debug.LogError($"Invalid split index: {currentSplit}. Cannot be negative.");
            return;
        }
        
        if (currentSplit + 2 >= checkPoints.Count)
        {
            Debug.LogError($"Invalid split index: {currentSplit}. Not enough checkpoints remaining (need {currentSplit + 2}, have {checkPoints.Count}).");
            return;
        }

        PolygonTrack threadTrack = ACOTrackMaster.GetPolygonTrack();
        List<System.Numerics.Vector2> threadRl = ACOTrackMaster.GetCurrentRaceline();

        Vector3 startPos = ACOTrackMaster.GetTrainingSpawnPosition(0, threadRl);
        Vector3 startDir = ACOTrackMaster.GetTrainingSpawnDirection(0, threadRl);

        Debug.Log($"Initializing system for split {currentSplit}");
        Debug.Log($"Start position: {startPos}");
        Debug.Log($"Start bearing: {CalculateBearing(new System.Numerics.Vector2(startDir.x, startDir.z))}");

        workers = new();

        // only 1 thread for now
        for (int i = 0; i < 1; i++)
        {
            System.Numerics.Vector2[] outer = threadTrack.GetOuterData;
            System.Numerics.Vector2[] inner = threadTrack.GetInnerData;

            System.Numerics.Vector2[] newOuter = new System.Numerics.Vector2[outer.Length];
            System.Numerics.Vector2[] newInner = new System.Numerics.Vector2[inner.Length];

            for (int j = 0; j < outer.Length; j++)
            {
                newOuter[j] = new System.Numerics.Vector2(outer[j].X, outer[j].Y);
            }

            for (int j = 0; j < inner.Length; j++)
            {
                newInner[j] = new System.Numerics.Vector2(inner[j].X, inner[j].Y);
            }

            PolygonTrack newTrack = new(newOuter, newInner);

            List<System.Numerics.Vector2> newRl = new();
            threadRl.ForEach((point) => newRl.Add(new System.Numerics.Vector2(point.X, point.Y)));

            workers.Add(new ACOWorkerThread2(newTrack, i, 5, newRl, 100, 40.0f));
        }

        workers.ForEach((wt) =>
        {
            // Determine starting state based on current split and available best agents
            System.Numerics.Vector2 pos;
            float bear;
            float speed;
            float turnAngle;
            
            if (currentSplit > 0 && bestAgents.Count >= currentSplit)
            {
                // Use the end state of the previous successful segment
                AgentContainer previousAgent = bestAgents[currentSplit - 1];
                pos = previousAgent.TargetPassPosition;
                bear = previousAgent.TargetPassBear;
                speed = previousAgent.TargetPassSpeed;
                turnAngle = previousAgent.TargetPassTurnAngle;
            }
            else
            {
                // Use initial spawn state
                pos = new System.Numerics.Vector2(startPos.x, startPos.z);
                bear = CalculateBearing(new System.Numerics.Vector2(startDir.x, startDir.z));
                speed = 0;
                turnAngle = 0;
            }

            wt.SetStartState(pos, bear, speed, turnAngle);

            List<System.Numerics.Vector2> cps = new();
            checkPoints.ForEach((point) => cps.Add(new(point.X, point.Y)));
            
            // Safe checkpoint setting with bounds check (already validated above)
            wt.SetCheckPoints(cps[currentSplit], cps[currentSplit + 1], cps[currentSplit + 2]);

            wt.StartThread();
        });
    }

    float CalculateBearing(System.Numerics.Vector2 forward)
    {
        float angle = (float)(Math.Atan2(forward.Y, forward.X) * 180.0 / Math.PI);
        return angle + 90.0f;
    }

    private void StartSystem()
    {
        workers.ForEach((wt) =>
        {
            if (wt == null)
            {
                Debug.LogWarning("Cannot start a system that is not initialized.");
                return;
            }

            wt.StartThread();
        });
    }

    private void StopSystem()
    {
        workers.ForEach((wt) =>
        {
            if (wt == null)
            {
                Debug.LogWarning("Cannot stop a system that is not initialized.");
                return;
            }

            wt.StopThread();
        });
    }

    private void CleanSystem()
    {

    }
}