using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ACOWorkerThreadTest : MonoBehaviour
{
    List<ACOWorkerThread2> workers;

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

        var checkPoints = InitializeDistanceBasedCheckpoints();

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

            workers.Add(new ACOWorkerThread2(newTrack, i, 5, newRl, 1000, 50.0f));
        }

        Vector3 startPos = ACOTrackMaster.GetTrainingSpawnPosition(0, threadRl);
        Vector3 startDir = ACOTrackMaster.GetTrainingSpawnDirection(0, threadRl);

        workers.ForEach((wt) =>
        {
            System.Numerics.Vector2 pos = new System.Numerics.Vector2(startPos.x, startPos.z);
            float bear = CalculateBearing(new System.Numerics.Vector2(startDir.x, startDir.z));

            wt.SetStartState(pos, bear);

            List<System.Numerics.Vector2> cps = new();
            checkPoints.ForEach((point) => cps.Add(new(point.X, point.Y)));
            wt.SetCheckPoints(cps[0], cps[1], cps[2]);
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