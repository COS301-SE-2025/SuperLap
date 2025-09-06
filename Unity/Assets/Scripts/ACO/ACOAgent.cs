using System;
using System.Collections.Generic;
using System.Numerics;
public class ACOAgent
{
    private float enginePower = 150000f;
    private float maxTractionForce = 7000f;
    private float brakingForce = 8000f;

    private float mass = 200f;
    private float dragCoefficient = 0.6f;
    private float frontalArea = 0.48f;
    private float rollingResistanceCoefficient = 0.012f;

    private float turnRate = 100f;
    private float steeringDecay = 0.9f;
    private float minSteeringSpeed = 0.5f;
    private float fullSteeringSpeed = 5f;
    private float steeringIntensity = 0.5f;

    private float maxLeanAngle = 45f;
    private float optimalLeanSpeed = 15f;
    private float leanSpeed = 5f;

    private float minFOV = 60f;
    private float maxFOV = 90f;
    private float maxFOVSpeed = 50f;
    private float accelerationFOVBoost = 2f;
    private float fovAdjustSpeed = 3f;

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

    private float currentSpeed = 0f;
    private float currentSpeedKmh = 0f;
    private float currentAcceleration = 0f;
    private float currentTurnAngle = 0f;
    private float currentLeanAngle = 0f;
    private float currentFOV = 0f;
    private float theoreticalTopSpeed = 0f;
    private float racelineDeviation = 0f;
    private float averageTrajectoryDeviation = 0f;
    private bool recommendSpeedUp = false;
    private bool recommendSlowDown = false;
    private bool recommendTurnLeft = false;
    private bool recommendTurnRight = false;
    private bool isCurrentlyOffTrack = false;

    // Input values
    private float throttleInput = 0f;
    private float steerInput = 0f;
        
    // Physics configuration
    private MotorcyclePhysicsConfig physicsConfig;
    private RecommendationConfig recommendationConfig;
    private TrackDetectionConfig trackDetectionConfig;
    private PolygonTrack track;
    private Vector2 position;
    private float bearing;

    public ACOAgent(PolygonTrack track, Vector2 pos, float bear)
    {
        position = pos;
        bearing = bear;
        InitializeConfigurations();
        InitializeComponents();
        theoreticalTopSpeed = MotorcyclePhysicsCalculator.CalculateTheoreticalTopSpeed(
            enginePower, dragCoefficient, frontalArea, rollingResistanceCoefficient, mass);
        this.track = track
    }

    private void InitializeConfigurations()
    {
        physicsConfig = new MotorcyclePhysicsConfig
        {
            enginePower = this.enginePower,
            maxTractionForce = this.maxTractionForce,
            brakingForce = this.brakingForce,
            mass = this.mass,
            dragCoefficient = this.dragCoefficient,
            frontalArea = this.frontalArea,
            rollingResistanceCoefficient = this.rollingResistanceCoefficient,
            turnRate = this.turnRate,
            steeringDecay = this.steeringDecay,
            minSteeringSpeed = this.minSteeringSpeed,
            fullSteeringSpeed = this.fullSteeringSpeed,
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

        trackDetectionConfig = new TrackDetectionConfig
        {
            raycastStartHeight = this.raycastStartHeight,
            raycastDistance = this.raycastDistance,
            showTrackDetectionDebug = this.showTrackDetectionDebug,
            onTrackRayColor = this.onTrackRayColor,
            offTrackRayColor = this.offTrackRayColor
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
        RacelineAnalyzer.UpdateRacelineDeviation(position, showTrajectory, 
                                               trajectoryPredictor.GetComponent<LineRenderer>(), 
                                               out racelineDeviation, out averageTrajectoryDeviation);
        
        DrivingRecommendationEngine.UpdateDrivingRecommendations(enableRecommendations, position, 
                                                               bearing, currentSpeed, currentTurnAngle, 
                                                               throttleInput, theoreticalTopSpeed, recommendationConfig, 
                                                               physicsConfig, trackDetectionConfig, out recommendSpeedUp, 
                                                               out recommendSlowDown, out recommendTurnLeft, out recommendTurnRight);

        List<(int, int)> options = new List<(int, int)>();

        if (recommendTurnLeft)
        {
            float offTrackRatio;
            if(TrackDetector.CheckIfPathGoesOffTrack(-1, position, bearing, 
                                                   currentSpeed, currentTurnAngle, throttleInput, 
                                                   trajectoryLength, recommendationSteps, offTrackThreshold, 
                                                   physicsConfig, trackDetectionConfig, out offTrackRatio))
            {
                options.Add((-1, -1)); // Brake
            }
            else
            {
                options.Add((0, -1)); // Turn left
                options.Add((1, -1)); // Accelerate and turn left
            }
        }

        if(recommendTurnRight)
        {
            float offTrackRatio;
            if(TrackDetector.CheckIfPathGoesOffTrack(1, position, bearing, 
                                                   currentSpeed, currentTurnAngle, throttleInput, 
                                                   trajectoryLength, recommendationSteps, offTrackThreshold, 
                                                   physicsConfig, trackDetectionConfig, out offTrackRatio))
            {
                options.Add((-1, 1)); // Brake
            }
            else
            {
                options.Add((0, 1)); // Turn right
                options.Add((1, 1)); // Accelerate and turn right
            }
        }

        if(recommendSpeedUp)
        {
            float offTrackRatio;
            if(TrackDetector.CheckIfPathGoesOffTrack(0, position, bearing, 
                                                   currentSpeed, currentTurnAngle, throttleInput, 
                                                   trajectoryLength, recommendationSteps, offTrackThreshold, 
                                                   physicsConfig, trackDetectionConfig, out offTrackRatio))
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
        int randomIndex = UnityEngine.Random.Range(0, options.Count);
        (int, int) selectedAction = options[randomIndex];

        return selectedAction;
    }

    public void SimulateStep()
    {
        float stepTime = 1f / 30f;
        UpdateMovement(stepTime);
        UpdateTurning(stepTime);

        // Update current track status
        isCurrentlyOffTrack = IsOffTrack();
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
        float bearingRadians = bearing * (float)Math.PI / 180f;
        position.X += distance * (float)Math.Sin(bearingRadians);
        position.Y += distance * (float)Math.Cos(bearingRadians);
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

    public void SetInitialState(float speed, float turnAngle)
    {
        currentSpeed = speed;
        currentTurnAngle = turnAngle;
        currentSpeedKmh = currentSpeed * 3.6f;
    }
}