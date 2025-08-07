using UnityEngine;
using TMPro;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class ReadOnlyAttribute : PropertyAttribute
{
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif

public class MotorcyclePlayer : MonoBehaviour
{
    private const float AIR_DENSITY = 1.225f;
    private const float GRAVITY = 9.81f;

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
    
    [Header("Recommendation UI GameObjects")]
    [SerializeField] private float recommendationSensitivity = 0.1f;
    [Tooltip("GameObject to enable when recommending speed up (throttle).")]
    [SerializeField] private GameObject speedUpIndicator;
    [Tooltip("GameObject to enable when recommending slow down (brake).")]
    [SerializeField] private GameObject slowDownIndicator;
    [Tooltip("GameObject to enable when recommending turn left.")]
    [SerializeField] private GameObject turnLeftIndicator;
    [Tooltip("GameObject to enable when recommending turn right.")]
    [SerializeField] private GameObject turnRightIndicator;

    [Header("Reward System Settings")]
    [Tooltip("Reward given for following a recommendation.")]
    [SerializeField] private float recommendationFollowReward = 1.0f;
    [Tooltip("Minimum input threshold to consider an action as 'following' a recommendation.")]
    [SerializeField] private float inputThreshold = 0.3f;
    [Tooltip("Multiplier for speed-based rewards. Higher values give more reward for high speeds.")]
    [SerializeField] private float speedRewardMultiplier = 0.5f;

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

    [Header("Reward System")]
    [ReadOnly] [SerializeField] private float cumulativeReward = 0f;
    [ReadOnly] [SerializeField] private float followingRecommendationsReward = 0f;
    [ReadOnly] [SerializeField] private float speedReward = 0f;

    // Input values
    private float throttleInput = 0f;
    private float steerInput = 0f;
    
    // Internal tracking
    private float previousSpeed = 0f;
    
    // Reward system tracking
    private bool wasFollowingRecommendationLastFrame = false;
    private float timeSinceLastRewardLog = 0f;
    
    // Trajectory prediction
    private LineRenderer trajectoryLineRenderer;

    void Start()
    {
        CalculateTheoreticalTopSpeed();
        SetupTrajectoryLineRenderer();
        ResetRewards();
    }

    void Update()
    {
        HandleInput();
        UpdateMovement(Time.deltaTime);
        UpdateTurning(Time.deltaTime);
        UpdateMotorcycleLeaning(Time.deltaTime);
        UpdateCameraFOV(Time.deltaTime);
        UpdateTrajectoryVisualization();
        UpdateRacelineDeviation();
        UpdateDrivingRecommendations();
        UpdateRewardSystem();

        currentSpeedKmh = currentSpeed * 3.6f;
        if (speedText != null)
        {
            speedText.text = $"{currentSpeedKmh:F1} km/h";
        }
        
        if (racelineDeviationText != null)
        {
            racelineDeviationText.text = $"Raceline Dev: {racelineDeviation:F2}m\nTraj Avg Dev: {averageTrajectoryDeviation:F2}m\n" +
                                       $"Cumulative Reward: {cumulativeReward:F1}\n" +
                                       $"Follow Rec. Reward: {followingRecommendationsReward:F1}\n" +
                                       $"Speed Reward: {speedReward:F1}";
        }
    }

    private void HandleInput()
    {
        throttleInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
    }

    private void UpdateRewardSystem()
    {
        timeSinceLastRewardLog += Time.deltaTime;
        
        // Calculate speed-based reward
        float speedRewardThisFrame = CalculateSpeedReward();
        speedReward += speedRewardThisFrame;
        cumulativeReward += speedRewardThisFrame;
        
        // Check if player is following recommendations
        bool followingRecommendation = false;
        string followedAction = "";
        
        // Check throttle/brake recommendations
        if (recommendSpeedUp && throttleInput > inputThreshold)
        {
            followingRecommendation = true;
            followedAction = "Speed Up";
        }
        else if (recommendSlowDown && throttleInput < -inputThreshold)
        {
            followingRecommendation = true;
            followedAction = "Slow Down";
        }
        
        // Check steering recommendations (can be combined with throttle)
        if (recommendTurnLeft && steerInput < -inputThreshold)
        {
            followingRecommendation = true;
            followedAction += (followedAction != "" ? " + " : "") + "Turn Left";
        }
        else if (recommendTurnRight && steerInput > inputThreshold)
        {
            followingRecommendation = true;
            followedAction += (followedAction != "" ? " + " : "") + "Turn Right";
        }
        
        // Award points for following recommendations
        if (followingRecommendation)
        {
            float reward = recommendationFollowReward * Time.deltaTime;
            followingRecommendationsReward += reward;
            cumulativeReward += reward;
        }
        
        // Debug output in editor (limit frequency to avoid spam)
        #if UNITY_EDITOR
        if (followingRecommendation && (!wasFollowingRecommendationLastFrame || timeSinceLastRewardLog > 2f))
        {
            Debug.Log($"Following recommendation: {followedAction}! " +
                     $"Cumulative reward: {cumulativeReward:F2}");
            timeSinceLastRewardLog = 0f;
        }
        #endif
        
        wasFollowingRecommendationLastFrame = followingRecommendation;
    }

    private float CalculateSpeedReward()
    {
        // Reward is based on the ratio of current speed to theoretical top speed
        // Uses a curved function to give more reward for higher speeds
        float speedRatio = currentSpeed / theoreticalTopSpeed;
        
        // Use a quadratic function to emphasize higher speeds more
        // This gives minimal reward at low speeds and exponentially more at high speeds
        float normalizedReward = speedRatio * speedRatio;
        
        // Apply the multiplier and time delta
        return normalizedReward * speedRewardMultiplier * Time.deltaTime;
    }

    private void UpdateMovement(float dt)
    {
        previousSpeed = currentSpeed;

        float drivingForce = CalculateDrivingForce();
        float resistanceForces = CalculateResistanceForces();

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
        float steeringMultiplier = CalculateSteeringMultiplier();
        
        currentTurnAngle += steerInput * turnRate * steeringMultiplier * dt;
        
        currentTurnAngle *= Mathf.Pow(steeringDecay, dt);

        transform.Rotate(Vector3.up * currentTurnAngle * dt);
    }

    private void UpdateMotorcycleLeaning(float dt)
    {
        if (motorcycleModel == null) return;

        float targetLeanAngle = CalculateTargetLeanAngle();
        
        currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetLeanAngle, leanSpeed * dt);
        
        motorcycleModel.localRotation = Quaternion.Euler(0, 0, -currentLeanAngle);
    }

    private void UpdateCameraFOV(float dt)
    {
        if (dynamicCamera == null) return;

        float targetFOV = CalculateTargetFOV();
        
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovAdjustSpeed * dt);
        
        dynamicCamera.fieldOfView = currentFOV;
    }

    private float CalculateDrivingForce()
    {
        float powerLimitedForce = enginePower / Mathf.Max(currentSpeed, 0.1f);
        
        return Mathf.Min(maxTractionForce, powerLimitedForce);
    }
    
    private float CalculateResistanceForces()
    {
        float dragForce = 0.5f * AIR_DENSITY * dragCoefficient * frontalArea * currentSpeed * currentSpeed;

        float rollingForce = rollingResistanceCoefficient * mass * GRAVITY;

        return dragForce + rollingForce;
    }

    private float CalculateSteeringMultiplier()
    {
        if (currentSpeed < minSteeringSpeed)
        {
            return 0f;
        }

        float normalizedSpeed = currentSpeed / fullSteeringSpeed;
        
        float steeringMultiplier = 1f / (1f + normalizedSpeed * steeringIntensity);
        
        float fadeInMultiplier = Mathf.Clamp01((currentSpeed - minSteeringSpeed) / (fullSteeringSpeed - minSteeringSpeed));
        
        return steeringMultiplier * fadeInMultiplier;
    }

    private float CalculateTargetLeanAngle()
    {
        if (currentSpeed < minSteeringSpeed || Mathf.Abs(currentTurnAngle) < 0.1f)
        {
            return 0f;
        }

        float speedMultiplier = CalculateSpeedLeanMultiplier();
        
        float turnIntensity = Mathf.Abs(currentTurnAngle) / turnRate;
        turnIntensity = Mathf.Clamp01(turnIntensity);
        
        float baseLeanAngle = turnIntensity * maxLeanAngle * speedMultiplier;
        
        return Mathf.Sign(currentTurnAngle) * baseLeanAngle;
    }

    private float CalculateSpeedLeanMultiplier()
    {
        if (currentSpeed <= optimalLeanSpeed)
        {
            return Mathf.Lerp(0.2f, 1f, currentSpeed / optimalLeanSpeed);
        }
        else
        {
            float excessSpeed = currentSpeed - optimalLeanSpeed;
            float reductionFactor = 1f / (1f + excessSpeed * 0.02f);
            return Mathf.Max(0.3f, reductionFactor);
        }
    }

    private float CalculateTargetFOV()
    {
        float speedBasedFOV = Mathf.Lerp(minFOV, maxFOV, currentSpeed / maxFOVSpeed);
        
        float accelerationBoost = Mathf.Max(0f, currentAcceleration) * accelerationFOVBoost;
        
        float targetFOV = speedBasedFOV + accelerationBoost;
        
        return Mathf.Clamp(targetFOV, minFOV, maxFOV + (accelerationFOVBoost * 10f));
    }

    private void CalculateTheoreticalTopSpeed()
    {
        float dragTerm = 0.5f * AIR_DENSITY * dragCoefficient * frontalArea;
        float rollingTerm = rollingResistanceCoefficient * mass * GRAVITY;
        
        theoreticalTopSpeed = Mathf.Pow(enginePower / dragTerm, 1f/3f);
        
        Debug.Log($"Theoretical Top Speed: {theoreticalTopSpeed * 3.6f:F1} km/h");
        Debug.Log($"Cd x A = {dragCoefficient * frontalArea:F3}");
    }

    private void SetupTrajectoryLineRenderer()
    {
        trajectoryLineRenderer = GetComponent<LineRenderer>();
        if (trajectoryLineRenderer == null)
        {
            trajectoryLineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        trajectoryLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trajectoryLineRenderer.material.color = trajectoryColor;
        trajectoryLineRenderer.startWidth = trajectoryWidth;
        trajectoryLineRenderer.endWidth = trajectoryWidth;
        trajectoryLineRenderer.positionCount = trajectoryPoints;
        trajectoryLineRenderer.useWorldSpace = true;
        trajectoryLineRenderer.enabled = showTrajectory;
        trajectoryLineRenderer.sortingOrder = 1;
    }

    private void UpdateTrajectoryVisualization()
    {
        if (trajectoryLineRenderer == null)
        {
            SetupTrajectoryLineRenderer();
            return;
        }

        trajectoryLineRenderer.enabled = showTrajectory;
        trajectoryLineRenderer.material.color = trajectoryColor;
        trajectoryLineRenderer.startWidth = trajectoryWidth;
        trajectoryLineRenderer.endWidth = trajectoryWidth;

        if (!showTrajectory) return;

        CalculateTrajectoryPoints();
    }

    private void CalculateTrajectoryPoints()
    {
        Vector3[] points = new Vector3[trajectoryPoints];
        
        // Simulation state - start with current state
        Vector3 simPosition = transform.position;
        Vector3 simForward = transform.forward;
        float simSpeed = currentSpeed;
        float simTurnAngle = currentTurnAngle;
        
        float deltaTime = trajectoryLength / trajectoryPoints;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            points[i] = simPosition;

            // Simulate one step forward using the same physics as the actual motorcycle
            SimulateOneStep(ref simPosition, ref simForward, ref simSpeed, ref simTurnAngle, deltaTime);
        }

        trajectoryLineRenderer.positionCount = trajectoryPoints;
        trajectoryLineRenderer.SetPositions(points);
    }

    private void SimulateOneStep(ref Vector3 position, ref Vector3 forward, ref float speed, ref float turnAngle, float dt)
    {
        // Use current inputs for prediction (could be modified to use different inputs)
        float simThrottleInput = throttleInput;
        float simSteerInput = steerInput;

        // Simulate movement using same physics as UpdateMovement
        float drivingForce = SimulateDrivingForce(speed);
        float resistanceForces = SimulateResistanceForces(speed);

        float netForce = (drivingForce * simThrottleInput) - resistanceForces;

        if (simThrottleInput < 0)
        {
            netForce = -brakingForce - resistanceForces;
        }

        float acceleration = netForce / mass;
        speed += acceleration * dt;
        speed = Mathf.Max(0, speed);

        // Simulate turning using same physics as UpdateTurning
        float steeringMultiplier = SimulateSteeringMultiplier(speed);
        turnAngle += simSteerInput * turnRate * steeringMultiplier * dt;
        turnAngle *= Mathf.Pow(steeringDecay, dt);

        // Apply rotation to forward vector
        float rotationThisFrame = turnAngle * dt;
        Quaternion rotation = Quaternion.AngleAxis(rotationThisFrame, Vector3.up);
        forward = rotation * forward;

        // Move forward
        position += forward * speed * dt;
    }

    private float SimulateDrivingForce(float speed)
    {
        float powerLimitedForce = enginePower / Mathf.Max(speed, 0.1f);
        return Mathf.Min(maxTractionForce, powerLimitedForce);
    }

    private float SimulateResistanceForces(float speed)
    {
        float dragForce = 0.5f * AIR_DENSITY * dragCoefficient * frontalArea * speed * speed;
        float rollingForce = rollingResistanceCoefficient * mass * GRAVITY;
        return dragForce + rollingForce;
    }

    private float SimulateSteeringMultiplier(float speed)
    {
        if (speed < minSteeringSpeed)
        {
            return 0f;
        }

        float normalizedSpeed = speed / fullSteeringSpeed;
        float steeringMultiplier = 1f / (1f + normalizedSpeed * steeringIntensity);
        float fadeInMultiplier = Mathf.Clamp01((speed - minSteeringSpeed) / (fullSteeringSpeed - minSteeringSpeed));
        
        return steeringMultiplier * fadeInMultiplier;
    }
    
    private void UpdateRacelineDeviation()
    {
        // Get the optimal raceline from TrackMaster
        List<Vector2> optimalRaceline = GetOptimalRaceline();
        if (optimalRaceline == null || optimalRaceline.Count == 0)
        {
            racelineDeviation = 0f;
            averageTrajectoryDeviation = 0f;
            return;
        }
        
        // Calculate current position deviation from raceline
        Vector2 currentPos2D = new Vector2(transform.position.x, transform.position.z);
        racelineDeviation = CalculateDistanceToRaceline(currentPos2D, optimalRaceline);
        
        // Calculate average trajectory deviation if trajectory is being shown
        if (showTrajectory && trajectoryLineRenderer != null && trajectoryLineRenderer.positionCount > 0)
        {
            averageTrajectoryDeviation = CalculateAverageTrajectoryDeviation(optimalRaceline);
        }
        else
        {
            averageTrajectoryDeviation = 0f;
        }
    }
    
    private List<Vector2> GetOptimalRaceline()
    {
        // Access the raceline from TrackMaster using the public method
        return TrackMaster.GetCurrentRaceline();
    }
    
    private float CalculateDistanceToRaceline(Vector2 position, List<Vector2> raceline)
    {
        if (raceline.Count == 0) return 0f;
        
        float minDistance = float.MaxValue;
        
        // Find the closest point on the raceline
        for (int i = 0; i < raceline.Count; i++)
        {
            float distance = Vector2.Distance(position, raceline[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }
        
        // Also check distances to line segments between consecutive points
        for (int i = 0; i < raceline.Count; i++)
        {
            int nextIndex = (i + 1) % raceline.Count;
            Vector2 lineStart = raceline[i];
            Vector2 lineEnd = raceline[nextIndex];
            
            float distanceToSegment = DistanceToLineSegment(position, lineStart, lineEnd);
            if (distanceToSegment < minDistance)
            {
                minDistance = distanceToSegment;
            }
        }
        
        return minDistance;
    }
    
    private float CalculateAverageTrajectoryDeviation(List<Vector2> raceline)
    {
        if (trajectoryLineRenderer.positionCount == 0) return 0f;
        
        Vector3[] trajectoryPoints = new Vector3[trajectoryLineRenderer.positionCount];
        trajectoryLineRenderer.GetPositions(trajectoryPoints);
        
        float totalDeviation = 0f;
        int validPoints = 0;
        
        // Calculate deviation for each trajectory point
        for (int i = 0; i < trajectoryPoints.Length; i++)
        {
            Vector2 trajPoint2D = new Vector2(trajectoryPoints[i].x, trajectoryPoints[i].z);
            float deviation = CalculateDistanceToRaceline(trajPoint2D, raceline);
            totalDeviation += deviation;
            validPoints++;
        }
        
        return validPoints > 0 ? totalDeviation / validPoints : 0f;
    }
    
    private float DistanceToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        float lineLength = line.magnitude;
        
        if (lineLength < 0.0001f) // Line segment is essentially a point
        {
            return Vector2.Distance(point, lineStart);
        }
        
        Vector2 lineDirection = line / lineLength;
        Vector2 pointToStart = point - lineStart;
        
        // Project point onto line
        float projection = Vector2.Dot(pointToStart, lineDirection);
        
        // Clamp projection to line segment
        projection = Mathf.Clamp(projection, 0f, lineLength);
        
        // Find closest point on line segment
        Vector2 closestPoint = lineStart + lineDirection * projection;
        
        return Vector2.Distance(point, closestPoint);
    }
    
    private void UpdateDrivingRecommendations()
    {
        if (!enableRecommendations)
        {
            SetAllRecommendationIndicators(false);
            return;
        }
        
        List<Vector2> optimalRaceline = GetOptimalRaceline();
        if (optimalRaceline == null || optimalRaceline.Count == 0)
        {
            SetAllRecommendationIndicators(false);
            return;
        }
        
        // Calculate recommendations for each input type
        RecommendationAnalysis analysis = AnalyzeRecommendations(optimalRaceline);
        
        // Update recommendation states
        recommendSpeedUp = analysis.shouldSpeedUp;
        recommendSlowDown = analysis.shouldSlowDown;
        recommendTurnLeft = analysis.shouldTurnLeft;
        recommendTurnRight = analysis.shouldTurnRight;
        
        // Update UI GameObjects
        UpdateRecommendationIndicators(analysis);
    }
    
    private RecommendationAnalysis AnalyzeRecommendations(List<Vector2> raceline)
    {
        // Get baseline score (continue straight)
        float baselineScore = SimulateRecommendationScenario(0f, 0f, raceline);
        
        // First, determine the optimal steering direction
        float turnLeftScore = SimulateRecommendationScenario(0f, -testInputStrength, raceline);
        float turnRightScore = SimulateRecommendationScenario(0f, testInputStrength, raceline);
        
        // Calculate steering improvements
        float turnLeftImprovement = baselineScore - turnLeftScore;
        float turnRightImprovement = baselineScore - turnRightScore;
        
        // Determine optimal steering input for speed tests
        float optimalSteeringInput = 0f;
        if (turnLeftImprovement > steeringSensitivity && turnLeftImprovement > turnRightImprovement)
        {
            optimalSteeringInput = -testInputStrength;
        }
        else if (turnRightImprovement > steeringSensitivity && turnRightImprovement > turnLeftImprovement)
        {
            optimalSteeringInput = testInputStrength;
        }
        
        // Test throttle/brake options with optimal steering
        float speedUpScore = SimulateRecommendationScenario(testInputStrength, optimalSteeringInput, raceline);
        float slowDownScore = SimulateRecommendationScenario(-testInputStrength, optimalSteeringInput, raceline);
        
        // If no steering is beneficial, also test speed changes without steering
        if (optimalSteeringInput == 0f)
        {
            float speedUpNoSteerScore = SimulateRecommendationScenario(testInputStrength, 0f, raceline);
            float slowDownNoSteerScore = SimulateRecommendationScenario(-testInputStrength, 0f, raceline);
            
            // Use the better of the two approaches
            speedUpScore = Mathf.Min(speedUpScore, speedUpNoSteerScore);
            slowDownScore = Mathf.Min(slowDownScore, slowDownNoSteerScore);
        }
        
        // Calculate improvements
        float speedUpImprovement = baselineScore - speedUpScore;
        float slowDownImprovement = baselineScore - slowDownScore;
        
        // Ensure only one throttle recommendation is active (prioritize the better improvement)
        bool shouldSpeedUp = false;
        bool shouldSlowDown = false;
        
        if (speedUpImprovement > throttleSensitivity && slowDownImprovement > throttleSensitivity)
        {
            // Both would help, choose the one with greater improvement
            if (speedUpImprovement > slowDownImprovement)
            {
                shouldSpeedUp = true;
            }
            else
            {
                shouldSlowDown = true;
            }
        }
        else if (speedUpImprovement > throttleSensitivity)
        {
            shouldSpeedUp = true;
        }
        else if (slowDownImprovement > throttleSensitivity)
        {
            shouldSlowDown = true;
        }
        
        return new RecommendationAnalysis
        {
            shouldSpeedUp = shouldSpeedUp,
            shouldSlowDown = shouldSlowDown,
            shouldTurnLeft = turnLeftImprovement > steeringSensitivity,
            shouldTurnRight = turnRightImprovement > steeringSensitivity,
            speedUpImprovement = speedUpImprovement,
            slowDownImprovement = slowDownImprovement,
            turnLeftImprovement = turnLeftImprovement,
            turnRightImprovement = turnRightImprovement
        };
    }
    
    private void UpdateRecommendationIndicators(RecommendationAnalysis analysis)
    {
        // Update throttle/brake indicators
        if (speedUpIndicator != null)
            speedUpIndicator.SetActive(analysis.shouldSpeedUp);
            
        if (slowDownIndicator != null)
            slowDownIndicator.SetActive(analysis.shouldSlowDown);
            
        // Update steering indicators
        if (turnLeftIndicator != null)
            turnLeftIndicator.SetActive(analysis.shouldTurnLeft);
            
        if (turnRightIndicator != null)
            turnRightIndicator.SetActive(analysis.shouldTurnRight);
    }
    
    private void SetAllRecommendationIndicators(bool active)
    {
        if (speedUpIndicator != null)
            speedUpIndicator.SetActive(active);
        if (slowDownIndicator != null)
            slowDownIndicator.SetActive(active);
        if (turnLeftIndicator != null)
            turnLeftIndicator.SetActive(active);
        if (turnRightIndicator != null)
            turnRightIndicator.SetActive(active);
            
        recommendSpeedUp = active;
        recommendSlowDown = active;
        recommendTurnLeft = active;
        recommendTurnRight = active;
    }
    
    private float SimulateRecommendationScenario(float testThrottle, float testSteering, List<Vector2> raceline)
    {
        // Simulation state - start with current state
        Vector3 simPosition = transform.position;
        Vector3 simForward = transform.forward;
        float simSpeed = currentSpeed;
        float simTurnAngle = currentTurnAngle;
        
        float deltaTime = trajectoryLength / recommendationSteps;
        float totalDeviation = 0f;
        
        // Simulate forward for the specified number of steps
        for (int step = 0; step < recommendationSteps; step++)
        {
            // Calculate deviation at this position
            Vector2 simPos2D = new Vector2(simPosition.x, simPosition.z);
            float deviation = CalculateDistanceToRaceline(simPos2D, raceline);
            totalDeviation += deviation;
            
            // Simulate one step forward with test inputs
            SimulateOneStepWithInputs(ref simPosition, ref simForward, ref simSpeed, ref simTurnAngle, 
                                    deltaTime, testThrottle, testSteering);
        }
        
        // Return average deviation over the simulation period
        return totalDeviation / recommendationSteps;
    }
    
    private void SimulateOneStepWithInputs(ref Vector3 position, ref Vector3 forward, ref float speed, 
                                         ref float turnAngle, float dt, float simThrottleInput, float simSteerInput)
    {
        // Simulate movement using same physics as UpdateMovement
        float drivingForce = SimulateDrivingForce(speed);
        float resistanceForces = SimulateResistanceForces(speed);

        float netForce = (drivingForce * simThrottleInput) - resistanceForces;

        if (simThrottleInput < 0)
        {
            netForce = -brakingForce - resistanceForces;
        }

        float acceleration = netForce / mass;
        speed += acceleration * dt;
        speed = Mathf.Max(0, speed);

        // Simulate turning using same physics as UpdateTurning
        float steeringMultiplier = SimulateSteeringMultiplier(speed);
        turnAngle += simSteerInput * turnRate * steeringMultiplier * dt;
        turnAngle *= Mathf.Pow(steeringDecay, dt);

        // Apply rotation to forward vector
        float rotationThisFrame = turnAngle * dt;
        Quaternion rotation = Quaternion.AngleAxis(rotationThisFrame, Vector3.up);
        forward = rotation * forward;

        // Move forward
        position += forward * speed * dt;
    }
    
    private RecommendationResult FindBestRecommendation(List<Vector2> raceline)
    {
        // Define test scenarios: throttle input, steering input, name
        var testScenarios = new[]
        {
            (0f, 0f, "Continue Straight"),
            (testInputStrength, 0f, "Speed Up"),
            (-testInputStrength, 0f, "Slow Down"),
            (0f, testInputStrength, "Turn Right"),
            (0f, -testInputStrength, "Turn Left"),
            (testInputStrength, testInputStrength, "Speed Up + Right"),
            (testInputStrength, -testInputStrength, "Speed Up + Left"),
            (-testInputStrength, testInputStrength, "Slow Down + Right"),
            (-testInputStrength, -testInputStrength, "Slow Down + Left")
        };
        
        float bestScore = float.MaxValue;
        string bestRecommendation = "Continue Straight";
        float currentScore = float.MaxValue;
        
        foreach (var (throttle, steering, name) in testScenarios)
        {
            float score = SimulateRecommendationScenario(throttle, steering, raceline);
            
            // Store current scenario score for comparison
            if (name == "Continue Straight")
            {
                currentScore = score;
            }
            
            if (score < bestScore)
            {
                bestScore = score;
                bestRecommendation = name;
            }
        }
        
        // Only recommend change if improvement is significant enough
        float improvement = currentScore - bestScore;
        if (improvement < recommendationSensitivity)
        {
            bestRecommendation = "Continue Straight";
        }
        
        return new RecommendationResult
        {
            recommendation = bestRecommendation,
            score = bestScore,
            improvement = improvement
        };
    }
    
    // Public methods for accessing reward information
    public float GetCumulativeReward() => cumulativeReward;
    public float GetFollowingRecommendationsReward() => followingRecommendationsReward;
    public float GetSpeedReward() => speedReward;

    public void ResetRewards()
    {
        cumulativeReward = 0f;
        followingRecommendationsReward = 0f;
        speedReward = 0f;
        wasFollowingRecommendationLastFrame = false;
        timeSinceLastRewardLog = 0f;
        
        Debug.Log("Reward system reset!");
    }
    
    private struct RecommendationAnalysis
    {
        public bool shouldSpeedUp;
        public bool shouldSlowDown;
        public bool shouldTurnLeft;
        public bool shouldTurnRight;
        public float speedUpImprovement;
        public float slowDownImprovement;
        public float turnLeftImprovement;
        public float turnRightImprovement;
    }
}

internal class RecommendationResult
{
    public string recommendation { get; set; }
    public float score { get; set; }
    public float improvement { get; set; }
}
