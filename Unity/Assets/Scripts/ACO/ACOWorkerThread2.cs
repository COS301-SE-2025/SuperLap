using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

public class AgentContainer
{
    private ACOAgent agent;
    private List<(int, int)> inputs;
    private int lastThrottle, lastTurn;
    private bool isValid = true;
    private bool isDone = false;

    public bool IsValid => isValid;
    public bool ShouldRun => isValid && !isDone;
    public bool IsDone => isDone;
    public int TotalSteps => inputs.Count();
    public Vector2 Position => agent.Position;
    private Vector2[] checkPoints;
    private float checkpointDistance;
    private bool passedTarget = false;
    private Vector2 targetPassPos;
    private float targetPassSpeed;
    private float targetPassBear;

    public Vector2 TargetPassPosition => targetPassPos;
    public float TargetPassSpeed => targetPassSpeed;
    public float TargetPassBear => targetPassBear;
    public float TargetPassTurnAngle => agent.GetCurrentTurnAngle();
    public List<(int, int)> Inputs => inputs;
    List<ReplayState> replayStates;
    public List<ReplayState> ReplayStates => replayStates;

    public AgentContainer(PolygonTrack track, Vector2 pos, float bear, float ss, float ta, List<Vector2> rl, ThreadLocalRacelineAnalyzer al, Vector2[] cps, float cpd)
    {
        agent = new ACOAgent(track, pos, bear, rl, al);
        inputs = new();
        checkPoints = cps;
        checkpointDistance = cpd;
        agent.SetInitialState(ss, ta);
        replayStates = new()
        {
            new ReplayState
            {
                position = agent.Position,
                bear = agent.GetCurrentBearing()
            }
        };
    }

    public void Step(bool decide = false)
    {
        if (!isValid || isDone)
        {
            return;
        }

        if (decide)
        {
            (lastThrottle, lastTurn) = agent.Decide();
            agent.SetInput(lastThrottle, lastTurn);
        }

        inputs.Add((lastThrottle, lastTurn));
        agent.Step();

        replayStates.Add(new ReplayState
        {
            position = agent.Position,
            bear = agent.GetCurrentBearing()
        });

        isValid = !agent.IsOffTrack();

        if (!passedTarget && InTargetRange())
        {
            passedTarget = true;
            targetPassSpeed = agent.GetCurrentSpeed();
            targetPassPos = Position;
            targetPassBear = agent.GetCurrentBearing();
        }

        if(passedTarget && InValidateRange())
        {
            isDone = true;
        }
    }

    public bool InTargetRange()
    {
        return Vector2.Distance(Position, checkPoints[1]) < checkpointDistance;
    }
    
    public bool InValidateRange()
    {
        return Vector2.Distance(Position, checkPoints[2]) < checkpointDistance;
    }
}

public class ACOWorkerThread2
{
    private PolygonTrack track;
    private int workerId;
    private int decisionInterval;
    private Thread workerThread;
    private bool running = false;

    // Public properties for thread state monitoring
    public bool IsRunning => running && workerThread != null && workerThread.IsAlive;
    public bool IsInitialized => workerThread != null;
    public ThreadState ThreadState => workerThread?.ThreadState ?? ThreadState.Unstarted;
    public int WorkerId => workerId;


    // Properties for agent spawning
    private int agentCount;
    private static readonly Vector2 NULL_VEC = Vector2.One * Int32.MaxValue;
    private Vector2 startPos = NULL_VEC;
    private float startBear = float.NaN;
    private float startSpeed = float.NaN;
    private float turnAngle = float.NaN;
    private ThreadLocalRacelineAnalyzer racelineAnalyzer;
    private List<Vector2> raceline;
    private Vector2[] checkPoints;
    private float checkpointDistance;
    private AgentContainer bestAgent;
    public AgentContainer BestAgent => bestAgent;

    public ACOWorkerThread2(
        PolygonTrack pt,
        int id,
        int decisionInt,
        List<Vector2> rl,
        int agentCounts,
        float cpd
    )
    {
        track = pt;

        workerId = id;
        decisionInterval = decisionInt;

        raceline = rl;

        // create raceline
        racelineAnalyzer = new ThreadLocalRacelineAnalyzer();
        racelineAnalyzer.InitializeWithRaceline(raceline);

        agentCount = agentCounts;

        checkPoints = new Vector2[0];
        checkpointDistance = cpd;
    }

    public void StartThread()
    {
        if (workerThread == null || !workerThread.IsAlive)
        {
            workerThread = new Thread(ThreadMain) { IsBackground = true };
            running = true;
            workerThread.Start();
            Debug.Log($"Worker thread {workerId} started successfully. State: {workerThread.ThreadState}");
        }
        else
        {
            Debug.LogWarning($"Worker thread {workerId} is already running. State: {workerThread.ThreadState}");
        }
    }

    public void StopThread()
    {
        if (running)
        {
            Debug.Log($"Stopping worker thread {workerId}...");
            running = false;

            // Give the thread a moment to stop gracefully
            if (workerThread != null && workerThread.IsAlive)
            {
                if (!workerThread.Join(1000)) // Wait up to 1 second
                {
                    Debug.LogWarning($"Worker thread {workerId} did not stop gracefully within timeout");
                }
                else
                {
                    Debug.Log($"Worker thread {workerId} stopped successfully");
                }
            }
        }
        else
        {
            Debug.LogWarning($"Worker thread {workerId} is not running");
        }
    }

    public void SetStartState(Vector2 position, float bearing, float ss, float ta)
    {
        startPos = position;
        startBear = bearing;
        startSpeed = ss;
        turnAngle = ta;
    }

    public void SetCheckPoints(Vector2 start, Vector2 goal, Vector2 validate)
    {
        checkPoints = new Vector2[]{ start, goal, validate };
    }

    public void ResetStartState()
    {
        startPos = NULL_VEC;
        startBear = float.NaN;
        startSpeed = float.NaN;
        turnAngle = float.NaN;
    }

    public void ResetCheckPoints()
    {
        checkPoints = new Vector2[0];
    }

    public void Join()
    {
        workerThread.Join(1000);
    }

    public void ThreadMain()
    {
        Debug.Log($"Worker thread {workerId} main loop started");

        List<AgentContainer> containers = new List<AgentContainer>();

        if (track == null)
        {
            Debug.LogError("Track is null.");
            return;
        }

        if (startPos == NULL_VEC)
        {
            Debug.LogError("Start pos not set.");
            return;
        }

        if (startBear == float.NaN)
        {
            Debug.LogError("Start bear not set.");
            return;
        }
        
        if(checkPoints.Length < 3)
        {
            Debug.LogError("Checkpoints not set.");
            return;
        }

        for (int i = 0; i < agentCount; i++)
        {
            containers.Add(new AgentContainer(track, startPos, startBear, startSpeed, turnAngle, raceline, racelineAnalyzer, checkPoints, checkpointDistance));
        }

        int iteration = 0;

        while (running)
        {
            bool update = iteration % decisionInterval == 0;
            List<AgentContainer> busy = containers.FindAll((ct) => ct.ShouldRun);
            busy.ForEach((ct) =>
            {
                ct.Step(update);
                if (ct.IsDone)
                {
                    running = false;
                }
            });

            if (busy.Count == 0)
                break;

            iteration++;
        }
        List<AgentContainer> valids = containers.FindAll((ct) => ct.IsDone && ct.IsValid);

        if(valids.Count == 0)
        {
            Debug.LogWarning($"Worker {workerId} could not obtain a solution!");
            bestAgent = null;
            return;
        }

        AgentContainer best = valids.OrderBy((ct1) => ct1.TotalSteps).First();

        // Debug.Log($"Worker thread {workerId} exited after doing {iteration} iterations.");

        if (best != null)
        {
            // Debug.Log($"Best: {best.TotalSteps}: {best.IsDone} and {best.IsValid}");
            bestAgent = best;
        } else
        {
            Debug.Log($"Could not find a best solution.");
            bestAgent = null;
        }
    }
}