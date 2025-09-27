using System;
using System.Collections.Generic;
using System.Numerics;
public class ACOAgent
{
    private static float enginePower = 80000f;
    private static float maxTractionForce = 7000f;
    private static float brakingForce = 8000f;

    private static float mass = 200f;
    private static float dragCoefficient = 0.6f;
    private static float frontalArea = 0.48f;
    private static float rollingResistanceCoefficient = 0.012f;

    private static float turnRate = 100f;
    private float steeringDecay = 0.9f;
    private static float minSteeringSpeed = 0.5f;
    private static float fullSteeringSpeed = 5f;
    private float steeringIntensity = 0.5f;

    private bool enableRecommendations = true;
    private int recommendationSteps = 10;
    private float steeringSensitivity = 0.1f;
    private float throttleSensitivity = 0.15f;
    private float testInputStrength = 0.5f;
    private float inputThreshold = 0.3f;
    
    private float raycastStartHeight = 2f;
    private float raycastDistance = 5f;
    private float offTrackThreshold = 0.25f;
    private float maxSpeedRatio = 0.8f;
    private bool showTrackDetectionDebug = false;

    private float currentSpeed = 5f;
    private float currentSpeedKmh = 0f;
    private float currentAcceleration = 0f;
    private float currentTurnAngle = 0f;
    private float theoreticalTopSpeed = 0f;
    private bool recommendSpeedUp = false;
    private bool recommendSlowDown = false;
    private bool recommendTurnLeft = false;
    private bool recommendTurnRight = false;
    private float trajectoryLength = 2f;

    // Input values
    private float throttleInput = 0f;
    private float steerInput = 0f;
        
    // Physics configuration
    private MotorcyclePhysicsConfig physicsConfig;
    private RecommendationConfig recommendationConfig;
    private PolygonTrack track;
    private Vector2 position;
    private float bearing;
    
    // Thread-local raceline copy to avoid memory contention
    private List<Vector2> threadLocalRaceline;
    private ThreadLocalRacelineAnalyzer threadLocalAnalyzer;

    public Vector2 Position => position;
    private static int globalInstanceCounter = 0;
    private int instanceId;
    public int ID => instanceId;
    Random random;
    public readonly static float Scale = 0.3f;

    public Vector2 Forward
    {
        get
        {
            float rad = (bearing - 90.0f) * (float)Math.PI / 180f;
            return new Vector2((float)Math.Cos(rad), (float)Math.Sin(rad));
        }
    }

    // set static field values
    public static void SetParameters(float enginePower, float maxTractionForce, float brakingForce,
                                       float mass, float dragCoefficient, float frontalArea, float rollingResistanceCoefficient,
                                       float turnRate, float minSteeringSpeed, float fullSteeringSpeed)
    {
        ACOAgent.enginePower = enginePower;
        ACOAgent.maxTractionForce = maxTractionForce;
        ACOAgent.brakingForce = brakingForce;
        ACOAgent.mass = mass;
        ACOAgent.dragCoefficient = dragCoefficient;
        ACOAgent.frontalArea = frontalArea;
        ACOAgent.rollingResistanceCoefficient = rollingResistanceCoefficient;
        ACOAgent.turnRate = turnRate;
        ACOAgent.minSteeringSpeed = minSteeringSpeed;
        ACOAgent.fullSteeringSpeed = fullSteeringSpeed;
    }

    public ACOAgent(PolygonTrack track, Vector2 pos, float bear, List<Vector2> raceline = null, ThreadLocalRacelineAnalyzer analyzer = null, int agentId = -1)
    {
        position = pos;
        bearing = bear;

        // Use provided agentId to avoid atomic contention, fallback to interlocked only if not provided
        if (agentId >= 0)
        {
            instanceId = agentId;
        }
        else
        {
            // Fallback for backward compatibility (but this should be avoided in multithreaded scenarios)
            instanceId = System.Threading.Interlocked.Increment(ref globalInstanceCounter);
        }

        InitializeConfigurations();
        InitializeComponents();
        theoreticalTopSpeed = MotorcyclePhysicsCalculator.CalculateTheoreticalTopSpeed(
            enginePower, dragCoefficient, frontalArea, rollingResistanceCoefficient, mass);
        this.track = track;
        // Create deep copy of raceline to ensure thread isolation
        if (raceline != null)
        {
            this.threadLocalRaceline = new List<Vector2>();
            foreach (var point in raceline)
            {
                this.threadLocalRaceline.Add(new Vector2(point.X, point.Y));
            }
        }
        else
        {
            this.threadLocalRaceline = null; // Fall back to shared raceline if needed
        }

        // Initialize Random with a unique seed to prevent thread contention
        // Use agent ID and current high-resolution timestamp to ensure uniqueness
        int uniqueSeed = instanceId * 31 + (int)(System.DateTime.UtcNow.Ticks & 0x7FFFFFFF);
        random = new Random(uniqueSeed);

        // Use provided analyzer, or create new one if raceline is provided
        if (analyzer != null)
        {
            threadLocalAnalyzer = analyzer;
        }
        else if (raceline != null)
        {
            threadLocalAnalyzer = new ThreadLocalRacelineAnalyzer();
            threadLocalAnalyzer.InitializeWithRaceline(raceline);
        }
    }

    private void InitializeConfigurations()
    {
        physicsConfig = new MotorcyclePhysicsConfig
        {
            enginePower = ACOAgent.enginePower,
            maxTractionForce = ACOAgent.maxTractionForce,
            brakingForce = ACOAgent.brakingForce,
            mass = ACOAgent.mass,
            dragCoefficient = ACOAgent.dragCoefficient,
            frontalArea = ACOAgent.frontalArea,
            rollingResistanceCoefficient = ACOAgent.rollingResistanceCoefficient,
            turnRate = ACOAgent.turnRate,
            steeringDecay = this.steeringDecay,
            minSteeringSpeed = ACOAgent.minSteeringSpeed,
            fullSteeringSpeed = ACOAgent.fullSteeringSpeed,
            steeringIntensity = this.steeringIntensity
        };

        recommendationConfig = new RecommendationConfig
        {
            recommendationSteps = this.recommendationSteps,
            steeringSensitivity = this.steeringSensitivity,
            throttleSensitivity = this.throttleSensitivity,
            testInputStrength = this.testInputStrength,
            inputThreshold = this.inputThreshold,
            offTrackThreshold = this.offTrackThreshold,
            maxSpeedRatio = this.maxSpeedRatio,
            trajectoryLength = this.trajectoryLength
        };
    }

    private void InitializeComponents()
    {

    }

    public void SetInput(int throttle, int steer)
    {
        throttleInput = throttle;
        steerInput = steer;
    }

    public (int, int) Decide()
    {
        // Use thread-local raceline - should NEVER be null to avoid shared memory access
        List<Vector2> raceline = threadLocalRaceline;
        if (raceline == null)
        {
            throw new Exception("Critical Error: threadLocalRaceline is null - this causes memory contention!");
        }
        
        // Use thread-local analyzer when available to avoid shared memory contention
        if (threadLocalAnalyzer != null)
        {
            ACODrivingRecommendationEngine.UpdateDrivingRecommendationsWithAnalyzer(enableRecommendations, position, 
                                                                   Forward, currentSpeed, currentTurnAngle, 
                                                                   throttleInput, theoreticalTopSpeed, recommendationConfig, 
                                                                   physicsConfig, track, raceline, threadLocalAnalyzer,
                                                                   out recommendSpeedUp, out recommendSlowDown, 
                                                                   out recommendTurnLeft, out recommendTurnRight);
        }
        else
        {
            throw new Exception("Thread-local analyzer is not initialized.");
        }

        List<(int, int)> options = new List<(int, int)>();

        if (recommendTurnLeft)
        {
            float offTrackRatio;
            if (ACOTrajectoryPredictor.CheckIfPathGoesOffTrack(-1, position, Forward,
                                                   currentSpeed, currentTurnAngle, throttleInput,
                                                   trajectoryLength, recommendationSteps, offTrackThreshold,
                                                   physicsConfig, out offTrackRatio, track))
            {
                options.Add((-1, -1)); // Brake
            }
            else
            {
                options.Add((0, -1)); // Turn left
                options.Add((1, -1)); // Accelerate and turn left
            }
        }

        if (recommendTurnRight)
        {
            float offTrackRatio;
            if (ACOTrajectoryPredictor.CheckIfPathGoesOffTrack(1, position, Forward,
                                                   currentSpeed, currentTurnAngle, throttleInput,
                                                   trajectoryLength, recommendationSteps, offTrackThreshold,
                                                   physicsConfig, out offTrackRatio, track))
            {
                options.Add((-1, 1)); // Brake
            }
            else
            {
                options.Add((0, 1)); // Turn right
                options.Add((1, 1)); // Accelerate and turn right
            }
        }

        if (recommendSpeedUp)
        {
            float offTrackRatio;
            if (ACOTrajectoryPredictor.CheckIfPathGoesOffTrack(0, position, Forward,
                                                   currentSpeed, currentTurnAngle, throttleInput,
                                                   trajectoryLength, recommendationSteps, offTrackThreshold,
                                                   physicsConfig, out offTrackRatio, track))
            {
                options.Add((-1, 0)); // Brake
            }
            else
            {
                options.Add((1, 0)); // Accelerate
                options.Add((0, 0)); // Idle
            }
        }

        // Fallback: if no options were added, provide default actions
        if (options.Count == 0)
        {
            // If we're stationary, always try to accelerate
            if (currentSpeed < 0.1f)
            {
                options.Add((1, 0)); // Accelerate
            }
            else
            {
                // Add some basic options when no recommendations are active
                options.Add((1, 0));  // Accelerate
                options.Add((0, 0));  // Idle
                options.Add((-1, 0)); // Brake
            }
        }

        // Randomly select one of the available options
        int randomIndex = random.Next(0, options.Count);
        (int, int) selectedAction = options[randomIndex];

        return selectedAction;
    }

    public void Step()
    {
        float stepTime = 1f / 30f;
        UpdateMovement(stepTime);
        UpdateTurning(stepTime);
    }

    private void UpdateMovement(float dt)
    {
        float drivingForce = MotorcyclePhysicsCalculator.CalculateDrivingForce(enginePower, currentSpeed, maxTractionForce);
        float resistanceForces = MotorcyclePhysicsCalculator.CalculateResistanceForces(currentSpeed, dragCoefficient,
                                                                                      frontalArea, rollingResistanceCoefficient, mass);

        float netForce = (drivingForce * throttleInput) - resistanceForces;

        if (throttleInput < 0)
        {
            netForce = -brakingForce - resistanceForces;
        }

        float acceleration = netForce / mass;
        currentAcceleration = acceleration;

        currentSpeed += acceleration * dt;

        currentSpeed = Math.Max(0, currentSpeed);

        // calculate new position from bearing and current position
        float distance = currentSpeed * dt;
        position += Forward * distance * Scale;
    }

    private void UpdateTurning(float dt)
    {
        float steeringMultiplier = MotorcyclePhysicsCalculator.CalculateSteeringMultiplier(currentSpeed, minSteeringSpeed, 
                                                                                         fullSteeringSpeed, steeringIntensity);
        
        currentTurnAngle += steerInput * turnRate * steeringMultiplier * dt;
        
        currentTurnAngle *= (float)Math.Pow(steeringDecay, dt);

        bearing += currentTurnAngle * dt;
    }

    // Public methods for training system
    public bool IsOffTrack()
    {
        // Remove caching entirely - in multi-threaded scenarios with many agents,
        // cache hits are rare and the overhead of checking exceeds the benefits
        return !track.PointInTrack(position);
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    public float GetCurrentTurnAngle()
    {
        return currentTurnAngle;
    }

    public float GetCurrentBearing()
    {
        return bearing;
    }

    public void SetInitialState(float speed, float turnAngle)
    {
        currentSpeed = speed;
        currentTurnAngle = turnAngle;
        currentSpeedKmh = currentSpeed * 3.6f;
    }
}