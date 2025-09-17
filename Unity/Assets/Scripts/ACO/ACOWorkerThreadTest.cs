using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        if (running)
        {
            bool allDone = workers.Count((w) => w.IsRunning) == 0;

            if (allDone)
            {
                if(waitTimer>0.0f)
                {
                    waitTimer -= Time.deltaTime;
                    return;
                }

                running = false;
                waitTimer = 0.05f;
                c++;

                if (c < checkpointCount - 2)
                {
                    List<AgentContainer> temp = new();
                    workers.ForEach((w) =>
                    {
                        if (w.BestAgent != null)
                        {
                            temp.Add(w.BestAgent);
                        }
                    });

                    if (temp.Count == 0)
                    {
                        if (retryCounter <= 0)
                        {
                            Debug.LogWarning($"Could not find solution for split no. {c}. Falling back one step.");
                            c -= 2;
                            InitializeSystem(c);
                            running = true;
                            retryCounter = 5;
                            bestAgents.Remove(bestAgents.Last());
                            return;
                        }
                        c--;
                        InitializeSystem(c);
                        running = true;
                        Debug.LogWarning($"Could not find solution for split no. {c}. Retrying.");
                        retryCounter--;
                        return;
                    }
                    retryCounter = 5;

                    AgentContainer ba = temp.OrderBy((ac) => ac.TotalSteps).First();

                    bestAgents.Add(ba);
                    InitializeSystem(c);
                    running = true;
                    Debug.Log($"Starting  with split no. {c}");
                }
                else
                {
                    Debug.Log("Training completed!");
                    c = 0;
                }
            }
        }
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

    private void InitializeSystem(int c)
    {
        PolygonTrack threadTrack = ACOTrackMaster.GetPolygonTrack();
        List<System.Numerics.Vector2> threadRl = ACOTrackMaster.GetCurrentRaceline();

        var checkPoints = InitializeDistanceBasedCheckpoints();

        Vector3 startPos = ACOTrackMaster.GetTrainingSpawnPosition(0, threadRl);
        Vector3 startDir = ACOTrackMaster.GetTrainingSpawnDirection(0, threadRl);

        // for (int c = 0; c < checkPoints.Count - 2; c++)
        {
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
                System.Numerics.Vector2 pos = c > 0 ? bestAgents.Last().TargetPassPosition : new System.Numerics.Vector2(startPos.x, startPos.z);
                float bear = c > 0 ? bestAgents.Last().TargetPassBear : CalculateBearing(new System.Numerics.Vector2(startDir.x, startDir.z));

                wt.SetStartState(pos, bear, c > 0 ? bestAgents.Last().TargetPassSpeed : 0, c > 0 ? bestAgents.Last().TargetPassTurnAngle : 0);

                List<System.Numerics.Vector2> cps = new();
                checkPoints.ForEach((point) => cps.Add(new(point.X, point.Y)));
                wt.SetCheckPoints(cps[c], cps[c + 1], cps[c + 2]);

                wt.StartThread();
            });
        }
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