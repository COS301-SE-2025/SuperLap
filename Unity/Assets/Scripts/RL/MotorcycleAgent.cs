using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Profiling;
public class MotorcycleAgent : MonoBehaviour
{
    [Header("Engine & Power")]
    [Tooltip("Engine power in Watts. ~750W = 1 horsepower.")]
    [SerializeField] private float enginePower = 150000f;
    [Tooltip("Maximum force the wheel can apply before slipping (in Newtons).")]
    [SerializeField] private float maxTractionForce = 7000f;
    [Tooltip("Force applied when braking (in Newtons).")]
    [SerializeField] private float brakingForce = 8000f;

    [Header("Physical Properties")]
    [Tooltip("Total mass of the bike and rider in KG.")]
    [SerializeField] private float mass = 200f;
    [Tooltip("Coefficient of drag. Lower is more aerodynamic.")]
    [SerializeField] private float dragCoefficient = 0.6f;
    [Tooltip("Frontal area of the bike/rider in square meters.")]
    [SerializeField] private float frontalArea = 0.48f;
    [Tooltip("Coefficient of rolling resistance.")]
    [SerializeField] private float rollingResistanceCoefficient = 0.012f;

    [Header("Turning")]
    [Tooltip("How fast the bike turns in degrees/sec.")]
    [SerializeField] private float turnRate = 100f;
    [Tooltip("How quickly the steering returns to center (0-1 range).")]
    [SerializeField] private float steeringDecay = 0.9f;
    [Tooltip("Minimum speed (m/s) required to steer.")]
    [SerializeField] private float minSteeringSpeed = 0.5f;
    [Tooltip("Speed (m/s) at which steering becomes fully responsive.")]
    [SerializeField] private float fullSteeringSpeed = 5f;
    [Tooltip("Controls how aggressively steering reduces with speed. Higher = more reduction. (0.1-2.0 recommended)")]
    [SerializeField] private float steeringIntensity = 0.5f;

    [Header("Motorcycle Banking/Leaning")]
    [Tooltip("Reference to the motorcycle model that will tilt. Should be a child transform.")]
    [SerializeField] private Transform motorcycleModel;
    [Tooltip("Maximum lean angle in degrees.")]
    [SerializeField] private float maxLeanAngle = 45f;
    [Tooltip("Speed (m/s) at which maximum lean is achieved.")]
    [SerializeField] private float optimalLeanSpeed = 15f;
    [Tooltip("How quickly the bike leans into turns (0-1 range).")]
    [SerializeField] private float leanSpeed = 5f;

    [Header("Dynamic Camera")]
    [Tooltip("Optional camera to adjust FOV based on speed and acceleration.")]
    [SerializeField] private Camera dynamicCamera;
    [Tooltip("Minimum field of view when stationary.")]
    [SerializeField] private float minFOV = 60f;
    [Tooltip("Maximum field of view at top speed.")]
    [SerializeField] private float maxFOV = 90f;
    [Tooltip("Speed (m/s) at which maximum FOV is reached.")]
    [SerializeField] private float maxFOVSpeed = 50f;
    [Tooltip("Additional FOV boost per m/s² of acceleration.")]
    [SerializeField] private float accelerationFOVBoost = 2f;
    [Tooltip("How quickly the FOV adjusts to changes.")]
    [SerializeField] private float fovAdjustSpeed = 3f;

    [Header("Trajectory Prediction")]
    [Tooltip("Enable/disable trajectory line visualization.")]
    [SerializeField] private bool showTrajectory = false;
    [Tooltip("Length of the predicted trajectory in seconds.")]
    [SerializeField] private float trajectoryLength = 3f;
    [Tooltip("Number of points to calculate for the trajectory line.")]
    [SerializeField] private int trajectoryPoints = 50;
    [Tooltip("Color of the trajectory line.")]
    [SerializeField] private Color trajectoryColor = Color.green;
    [Tooltip("Width of the trajectory line.")]
    [SerializeField] private float trajectoryWidth = 0.1f;

    [Header("Driving Recommendations")]
    [Tooltip("Enable/disable driving recommendations based on raceline analysis.")]
    [SerializeField] private bool enableRecommendations = true;
    [Tooltip("Number of simulation steps to test each recommendation.")]
    [SerializeField] private int recommendationSteps = 10;
    [Tooltip("Sensitivity threshold for steering recommendations (lower = more sensitive).")]
    [SerializeField] private float steeringSensitivity = 0.1f;
    [Tooltip("Sensitivity threshold for throttle/brake recommendations (lower = more sensitive).")]
    [SerializeField] private float throttleSensitivity = 0.15f;
    [Tooltip("Input strength for testing recommendations (0-1).")]
    [SerializeField] private float testInputStrength = 0.5f;
    [Tooltip("Minimum input threshold to consider an action as 'following' a recommendation.")]
    [SerializeField] private float inputThreshold = 0.3f;
    
    [Header("Track Detection")]
    [Tooltip("Height above ground to start raycast for track detection (in meters).")]
    [SerializeField] private float raycastStartHeight = 2f;
    [Tooltip("Maximum distance to check for track surface (in meters).")]
    [SerializeField] private float raycastDistance = 5f;
    [Tooltip("Ratio of off-track points that triggers braking recommendation (0-1).")]
    [SerializeField] private float offTrackThreshold = 0.25f;
    [Tooltip("Maximum speed ratio (of theoretical top speed) before stopping speed-up recommendations.")]
    [SerializeField] private float maxSpeedRatio = 0.8f;
    [Tooltip("Enable debug visualization of track detection raycasts.")]
    [SerializeField] private bool showTrackDetectionDebug = false;
    [Tooltip("Color for raycasts that hit track surface.")]
    [SerializeField] private Color onTrackRayColor = Color.green;
    [Tooltip("Color for raycasts that miss track surface.")]
    [SerializeField] private Color offTrackRayColor = Color.red;

    [Header("GUI")]
    [Tooltip("Displays the current speed of the motorcycle.")]
    [SerializeField] private TextMeshProUGUI speedText;
    [Tooltip("Displays the distance deviation from optimal raceline.")]
    [SerializeField] private TextMeshProUGUI racelineDeviationText;

    [Header("Current State (for debugging)")]
    [ReadOnly] [SerializeField] private float currentSpeed = 0f;
    [ReadOnly] [SerializeField] private float currentSpeedKmh = 0f;
    [ReadOnly] [SerializeField] private float currentAcceleration = 0f;
    [ReadOnly] [SerializeField] private float currentTurnAngle = 0f;
    [ReadOnly] [SerializeField] private float currentLeanAngle = 0f;
    [ReadOnly] [SerializeField] private float currentFOV = 0f;
    [ReadOnly] [SerializeField] private float theoreticalTopSpeed = 0f;
    [ReadOnly] [SerializeField] private float racelineDeviation = 0f;
    [ReadOnly] [SerializeField] private float averageTrajectoryDeviation = 0f;
    [ReadOnly] [SerializeField] private bool recommendSpeedUp = false;
    [ReadOnly] [SerializeField] private bool recommendSlowDown = false;
    [ReadOnly] [SerializeField] private bool recommendTurnLeft = false;
    [ReadOnly] [SerializeField] private bool recommendTurnRight = false;
    [ReadOnly] [SerializeField] private bool isCurrentlyOffTrack = false;

    // Input values
    private float throttleInput = 0f;
    private float steerInput = 0f;
    
    // Helper components
    private TrajectoryPredictor trajectoryPredictor;
    private MotorcycleVisualEffects visualEffects;
    
    // Physics configuration
    private MotorcyclePhysicsConfig physicsConfig;
    private RecommendationConfig recommendationConfig;
    private TrackDetectionConfig trackDetectionConfig;

    void Start()
    {
        InitializeConfigurations();
        InitializeComponents();
        theoreticalTopSpeed = MotorcyclePhysicsCalculator.CalculateTheoreticalTopSpeed(
            enginePower, dragCoefficient, frontalArea, rollingResistanceCoefficient, mass);
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
        trajectoryPredictor = GetComponent<TrajectoryPredictor>();
        if (trajectoryPredictor == null)
        {
            trajectoryPredictor = gameObject.AddComponent<TrajectoryPredictor>();
        }

        visualEffects = GetComponent<MotorcycleVisualEffects>();
        if (visualEffects == null)
        {
            visualEffects = gameObject.AddComponent<MotorcycleVisualEffects>();
        }

        trajectoryPredictor.SetupTrajectoryLineRenderer(trajectoryPoints, trajectoryColor, trajectoryWidth, showTrajectory);
    }

    public void SetInput(int throttle, int steer)
    {
        throttleInput = throttle;
        steerInput = steer;
    }

    public (int, int) Decide()
    {
        Profiler.BeginSample("MotorcycleAgent.UpdateRacelineDeviation");
        RacelineAnalyzer.UpdateRacelineDeviation(transform.position, showTrajectory, 
                                               trajectoryPredictor.GetComponent<LineRenderer>(), 
                                               out racelineDeviation, out averageTrajectoryDeviation);
        Profiler.EndSample();
        
        Profiler.BeginSample("MotorcycleAgent.UpdateDrivingRecommendations");
        DrivingRecommendationEngine.UpdateDrivingRecommendations(enableRecommendations, transform.position, 
                                                               transform.forward, currentSpeed, currentTurnAngle, 
                                                               throttleInput, theoreticalTopSpeed, recommendationConfig, 
                                                               physicsConfig, trackDetectionConfig, out recommendSpeedUp, 
                                                               out recommendSlowDown, out recommendTurnLeft, out recommendTurnRight);
        Profiler.EndSample();

        List<(int, int)> options = new List<(int, int)>();

        Profiler.BeginSample("MotorcycleAgent.GenerateActionOptions");

        if (recommendTurnLeft)
        {
            float offTrackRatio;
            if(TrackDetector.CheckIfPathGoesOffTrack(-1, transform.position, transform.forward, 
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
            if(TrackDetector.CheckIfPathGoesOffTrack(1, transform.position, transform.forward, 
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
            if(TrackDetector.CheckIfPathGoesOffTrack(0, transform.position, transform.forward, 
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

        Profiler.EndSample();

        return selectedAction;
    }

    public void Step()
    {
        Profiler.BeginSample("MotorcycleAgent.Step");
        UpdateMovement(Time.fixedDeltaTime);
        UpdateTurning(Time.fixedDeltaTime);
        visualEffects.UpdateMotorcycleLeaning(motorcycleModel, currentSpeed, currentTurnAngle, 
                                            maxLeanAngle, optimalLeanSpeed, leanSpeed, 
                                            minSteeringSpeed, turnRate, Time.fixedDeltaTime, ref currentLeanAngle);
        visualEffects.UpdateCameraFOV(dynamicCamera, currentSpeed, currentAcceleration, 
                                    minFOV, maxFOV, maxFOVSpeed, accelerationFOVBoost, 
                                    fovAdjustSpeed, Time.fixedDeltaTime, ref currentFOV);
        trajectoryPredictor.UpdateTrajectoryVisualization(showTrajectory, trajectoryColor, trajectoryWidth, 
                                                         trajectoryPoints, trajectoryLength, transform.position, 
                                                         transform.forward, currentSpeed, currentTurnAngle, 
                                                         throttleInput, steerInput, physicsConfig);

        // Update current track status
        isCurrentlyOffTrack = IsOffTrack();

        currentSpeedKmh = currentSpeed * 3.6f;
        if (speedText != null)
        {
            speedText.text = $"{currentSpeedKmh:F1} km/h";
        }

        if (racelineDeviationText != null)
        {
            racelineDeviationText.text = $"Raceline Dev: {racelineDeviation:F2}m\nTraj Avg Dev: {averageTrajectoryDeviation:F2}m\n" +
                                       $"Currently Off Track: {isCurrentlyOffTrack}";
        }
        Profiler.EndSample();
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

        currentSpeed = Mathf.Max(0, currentSpeed);

        transform.Translate(Vector3.forward * currentSpeed * dt, Space.Self);
    }

    private void UpdateTurning(float dt)
    {
        float steeringMultiplier = MotorcyclePhysicsCalculator.CalculateSteeringMultiplier(currentSpeed, minSteeringSpeed, 
                                                                                         fullSteeringSpeed, steeringIntensity);
        
        currentTurnAngle += steerInput * turnRate * steeringMultiplier * dt;
        
        currentTurnAngle *= Mathf.Pow(steeringDecay, dt);

        transform.Rotate(Vector3.up * currentTurnAngle * dt);
    }

    // Public methods for training system
    public bool IsOffTrack()
    {
        // Check if the motorcycle is currently off track using a simple raycast
        // This is used by the training system to determine if an agent should be recycled
        // Note: The Decide() method still uses predictive track detection for better decision making
        return !TrackDetector.IsPositionOnTrack(transform.position, trackDetectionConfig);
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

// Simple component to mark track surfaces for raycast detection
public class TrackSurface : MonoBehaviour
{
    // This component can be attached to track pieces to identify them in raycasts
    // IMPORTANT: The GameObject must also have the "Track" tag for the raycast detection to work
    // No additional functionality needed - just acts as a marker
}
