using System;
using System.Collections.Generic;
using UnityEngine;

public class ACOWorkerThreadTest : MonoBehaviour
{
    ACOWorkerThread2 wt;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            InitializeSystem();
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            StartSystem();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            StopSystem();
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
        int totalCheckpoints = 50;
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

            Debug.Log($"Distance-based checkpoint {checkpointIndex} placed at distance {targetDistance:F2} at position {lastRacelinePoint}");
        }

        return checkpointPositions;
    }

    private void InitializeSystem()
    {
        PolygonTrack threadTrack = ACOTrackMaster.GetPolygonTrack();
        List<System.Numerics.Vector2> threadRl = ACOTrackMaster.GetCurrentRaceline();

        var cps = InitializeDistanceBasedCheckpoints();
        
        wt = new ACOWorkerThread2(threadTrack, 0, 5, threadRl, 100, 50.0f);

        Vector3 startPos = ACOTrackMaster.GetTrainingSpawnPosition(0, threadRl);
        Vector3 startDir = ACOTrackMaster.GetTrainingSpawnDirection(0, threadRl);

        System.Numerics.Vector2 pos = new System.Numerics.Vector2(startPos.x, startPos.z);
        float bear = CalculateBearing(new System.Numerics.Vector2(startDir.x, startDir.z));
        wt.SetStartState(pos, bear);
        wt.SetCheckPoints(cps[0], cps[1], cps[2]);
        Debug.Log($"System Initialized with start pos {pos} and bearing {bear}");
        Debug.Log($"CP0: {cps[0]}");
        Debug.Log($"CP0: {cps[1]}");
        Debug.Log($"CP0: {cps[2]}");
    }

    float CalculateBearing(System.Numerics.Vector2 forward)
    {
        float angle = (float)(Math.Atan2(forward.Y, forward.X) * 180.0 / Math.PI);
        return angle + 90.0f;
    }

    private void StartSystem()
    {
        if (wt == null)
        {
            Debug.LogWarning("Cannot start a system that is not initialized.");
            return;
        }

        wt.StartThread();
    }
    
    private void StopSystem()
    {
        if (wt == null)
        {
            Debug.LogWarning("Cannot stop a system that is not initialized.");
            return;
        }

        wt.StopThread();
    }

    private void CleanSystem()
    {

    }
}