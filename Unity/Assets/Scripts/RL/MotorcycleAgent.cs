using System;
using UnityEngine;
using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class MotorcycleAgent : Agent
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

    [Header("GUI")]
    [Tooltip("Displays the current speed of the motorcycle.")]
    [SerializeField] private TextMeshProUGUI speedText;

    [Header("ML-Agents Training")]
    [Tooltip("Starting position for episode reset.")]
    [SerializeField] private Vector3 startingPosition;
    [Tooltip("Starting rotation for episode reset.")]
    [SerializeField] private Vector3 startingRotation;

    [Header("Ray Visualization")]
    [Tooltip("Number of rays to cast for visualization/sensing.")]
    [SerializeField] private int rayCount = 5;
    [Tooltip("Angle spread of rays in degrees (0-180). 0 = straight forward, 180 = full hemisphere.")]
    [Range(0f, 180f)]
    [SerializeField] private float rayAngle = 100f;
    [Tooltip("Maximum length of rays in meters.")]
    [SerializeField] private float maxRayLength = 10f;

    [Header("Current State (for debugging)")]
    [ReadOnly] [SerializeField] private float currentSpeed = 0f;
    [ReadOnly] [SerializeField] private float currentSpeedKmh = 0f;
    [ReadOnly] [SerializeField] private float currentAcceleration = 0f;
    [ReadOnly] [SerializeField] private float currentTurnAngle = 0f;
    [ReadOnly] [SerializeField] private float currentLeanAngle = 0f;
    [ReadOnly] [SerializeField] private float currentFOV = 0f;
    [ReadOnly] [SerializeField] private float theoreticalTopSpeed = 0f;

    // Input values (now controlled by ML-Agents or heuristic)
    private float throttleInput = 0f;
    private float steerInput = 0f;
    
    // Internal tracking
    private float previousSpeed = 0f;

    // Store initial values for episode reset
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public override void Initialize()
    {
        // Store initial transform values for episode resets
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        // Use custom starting position/rotation if provided
        if (startingPosition != Vector3.zero)
            initialPosition = startingPosition;
        if (startingRotation != Vector3.zero)
            initialRotation = Quaternion.Euler(startingRotation);

        CalculateTheoreticalTopSpeed();
    }

    public override void OnEpisodeBegin()
    {
        // Reset motorcycle state for new episode
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        
        // Reset physics state
        currentSpeed = 0f;
        currentAcceleration = 0f;
        currentTurnAngle = 0f;
        currentLeanAngle = 0f;
        previousSpeed = 0f;
        
        // Reset inputs
        throttleInput = 0f;
        steerInput = 0f;
        
        // Reset motorcycle model rotation if available
        if (motorcycleModel != null)
        {
            motorcycleModel.localRotation = Quaternion.identity;
        }
        
        // Reset camera FOV if available
        if (dynamicCamera != null)
        {
            currentFOV = minFOV;
            dynamicCamera.fieldOfView = currentFOV;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Add current motorcycle state as observations
        // These observations will be fed to the neural network
        
        // Speed and acceleration (2 observations)
        sensor.AddObservation(currentSpeed / 50f); // Normalized by approximate max speed
        sensor.AddObservation(currentAcceleration / 10f); // Normalized by reasonable acceleration range
        
        // Turn angle and lean angle (2 observations)
        sensor.AddObservation(currentTurnAngle / turnRate); // Normalized by max turn rate
        sensor.AddObservation(currentLeanAngle / maxLeanAngle); // Normalized by max lean angle
        
        // Position and rotation (6 observations)
        sensor.AddObservation(transform.position); // x, y, z position
        sensor.AddObservation(transform.rotation); // x, y, z rotation (quaternion converted to 3 values)
        
        // Relative position to starting point (3 observations)
        Vector3 relativePosition = transform.position - initialPosition;
        sensor.AddObservation(relativePosition);
        
        // Forward direction vector (3 observations)
        sensor.AddObservation(transform.forward);
        
        // Previous inputs (2 observations) - helps with temporal understanding
        sensor.AddObservation(throttleInput);
        sensor.AddObservation(steerInput);
        
        // Total: 21 observations
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Extract actions from the neural network
        // Continuous actions: [throttle, steering]
        throttleInput = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        steerInput = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);
        
        // Update motorcycle physics using the ML-Agent actions
        UpdateMovement(Time.fixedDeltaTime);
        UpdateTurning(Time.fixedDeltaTime);
        UpdateMotorcycleLeaning(Time.fixedDeltaTime);
        UpdateCameraFOV(Time.fixedDeltaTime);

        // Update display
        currentSpeedKmh = currentSpeed * 3.6f;
        if (speedText != null)
        {
            speedText.text = $"{currentSpeedKmh:F1} km/h";
        }
        
        // Add rewards based on performance
        // This is where you would implement your reward function
        // For example:
        // - Reward for maintaining speed
        // - Reward for smooth turning
        // - Penalty for going too slow or too fast
        // - Penalty for excessive lean angles
        
        // Example reward (you should customize this based on your training goals):
        float speedReward = currentSpeed > 5f ? 0.1f : -0.1f; // Encourage movement
        AddReward(speedReward * Time.fixedDeltaTime);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // This method provides manual control for testing and demonstration
        // It should map human input to the same action space as the neural network
        
        // Get human input from keyboard/controller
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical");   // Throttle (W/S or Up/Down arrows)
        continuousActionsOut[1] = Input.GetAxis("Horizontal"); // Steering (A/D or Left/Right arrows)
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

    private void OnDrawGizmos()
    {
        DrawRays();
    }

    private void DrawRays()
    {
        if (rayCount <= 0 || maxRayLength <= 0f) return;

        Vector3 startPosition = transform.position;
        Vector3 forward = transform.forward;
        Vector3 up = transform.up;

        // Set gizmo color
        Gizmos.color = Color.green;

        if (rayCount == 1)
        {
            // Single ray straight forward
            Vector3 rayDirection = forward;
            Gizmos.DrawRay(startPosition, rayDirection * maxRayLength);
        }
        else
        {
            // Multiple rays spread across the angle
            float halfAngle = rayAngle * 0.5f;
            
            for (int i = 0; i < rayCount; i++)
            {
                float angle;
                
                if (rayCount == 1)
                {
                    angle = 0f; // Straight forward
                }
                else
                {
                    // Distribute rays evenly across the angle range
                    float t = (float)i / (rayCount - 1); // 0 to 1
                    angle = Mathf.Lerp(-halfAngle, halfAngle, t);
                }

                // Create rotation around the Y-axis (horizontal rotation)
                Quaternion rotation = Quaternion.AngleAxis(angle, up);
                Vector3 rayDirection = rotation * forward;
                
                // Draw the ray
                Gizmos.DrawRay(startPosition, rayDirection * maxRayLength);
            }
        }
    }
}
