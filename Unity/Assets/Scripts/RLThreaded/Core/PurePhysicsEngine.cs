using System;

/// <summary>
/// Pure C# physics engine for motorcycle simulation
/// Replaces Unity's physics system with mathematical calculations
/// Thread-safe and optimized for concurrent agent simulation
/// </summary>
public static class PurePhysicsEngine
{
    private const float AIR_DENSITY = 1.225f;
    private const float GRAVITY = 9.81f;

    /// <summary>
    /// Simulate a single physics step for an agent
    /// Updates position, direction, speed, and turn angle based on input
    /// </summary>
    public static void SimulateStep(ref Vector2D position, ref Vector2D direction, ref float speed, 
                                  ref float turnAngle, float deltaTime, AgentInput input, MotorcycleConfig config)
    {
        // Update movement physics
        UpdateMovement(ref speed, deltaTime, input.throttle, config);
        
        // Update turning physics
        UpdateTurning(ref direction, ref turnAngle, deltaTime, input.steering, speed, config);
        
        // Update position based on current speed and direction
        Vector2D velocity = direction * speed;
        position = position + velocity * deltaTime;
    }

    /// <summary>
    /// Calculate theoretical top speed for a motorcycle configuration
    /// </summary>
    public static float CalculateTheoreticalTopSpeed(MotorcycleConfig config)
    {
        float dragTerm = 0.5f * AIR_DENSITY * config.dragCoefficient * config.frontalArea;
        float theoreticalTopSpeed = (float)Math.Pow(config.enginePower / dragTerm, 1f / 3f);
        return theoreticalTopSpeed;
    }

    /// <summary>
    /// Calculate driving force based on engine power and current speed
    /// </summary>
    public static float CalculateDrivingForce(float enginePower, float currentSpeed, float maxTractionForce)
    {
        float powerLimitedForce = enginePower / Math.Max(currentSpeed, 0.1f);
        return Math.Min(maxTractionForce, powerLimitedForce);
    }

    /// <summary>
    /// Calculate total resistance forces acting on the motorcycle
    /// </summary>
    public static float CalculateResistanceForces(float currentSpeed, MotorcycleConfig config)
    {
        float dragForce = 0.5f * AIR_DENSITY * config.dragCoefficient * config.frontalArea * currentSpeed * currentSpeed;
        float rollingForce = config.rollingResistanceCoefficient * config.mass * GRAVITY;
        return dragForce + rollingForce;
    }

    /// <summary>
    /// Calculate steering effectiveness based on speed
    /// </summary>
    public static float CalculateSteeringMultiplier(float currentSpeed, MotorcycleConfig config)
    {
        if (currentSpeed < config.minSteeringSpeed)
        {
            return 0f;
        }

        float normalizedSpeed = currentSpeed / config.fullSteeringSpeed;
        float steeringMultiplier = 1f / (1f + normalizedSpeed * config.steeringIntensity);
        float fadeInMultiplier = MathUtils.Clamp01((currentSpeed - config.minSteeringSpeed) / 
                                                  (config.fullSteeringSpeed - config.minSteeringSpeed));

        return steeringMultiplier * fadeInMultiplier;
    }

    /// <summary>
    /// Predict agent trajectory for the given number of steps
    /// Used for decision making and collision avoidance
    /// </summary>
    public static Vector2D[] PredictTrajectory(Vector2D startPosition, Vector2D startDirection, float startSpeed, 
                                             float startTurnAngle, AgentInput input, MotorcycleConfig config, 
                                             float trajectoryLength, int steps)
    {
        Vector2D[] trajectory = new Vector2D[steps];
        float deltaTime = trajectoryLength / steps;

        Vector2D simPosition = startPosition;
        Vector2D simDirection = startDirection;
        float simSpeed = startSpeed;
        float simTurnAngle = startTurnAngle;

        for (int i = 0; i < steps; i++)
        {
            SimulateStep(ref simPosition, ref simDirection, ref simSpeed, ref simTurnAngle, 
                        deltaTime, input, config);
            trajectory[i] = simPosition;
        }

        return trajectory;
    }

    /// <summary>
    /// Calculate lean angle for motorcycle based on speed and turn rate
    /// Used for visual effects and physics accuracy
    /// </summary>
    public static float CalculateLeanAngle(float speed, float turnRate, float maxLeanAngle, float optimalLeanSpeed)
    {
        if (speed < 0.1f || Math.Abs(turnRate) < 0.01f)
        {
            return 0f;
        }

        float speedFactor = Math.Min(speed / optimalLeanSpeed, 1f);
        float baseLeanAngle = Math.Abs(turnRate) * maxLeanAngle * speedFactor;
        
        return Math.Sign(turnRate) * Math.Min(baseLeanAngle, maxLeanAngle);
    }

    #region Private Methods

    private static void UpdateMovement(ref float speed, float deltaTime, int throttleInput, MotorcycleConfig config)
    {
        float force;
        
        if (throttleInput > 0)
        {
            // Accelerating
            force = CalculateDrivingForce(config.enginePower, speed, config.maxTractionForce);
        }
        else if (throttleInput < 0)
        {
            // Braking
            force = -config.brakingForce;
        }
        else
        {
            // Coasting - only resistance forces
            force = 0f;
        }

        // Apply resistance forces
        float resistanceForce = CalculateResistanceForces(speed, config);
        float netForce = force - resistanceForce;

        // Calculate acceleration and update speed
        float acceleration = netForce / config.mass;
        speed += acceleration * deltaTime;

        // Ensure speed doesn't go negative
        speed = Math.Max(0f, speed);
    }

    private static void UpdateTurning(ref Vector2D direction, ref float turnAngle, float deltaTime, 
                                    int steerInput, float currentSpeed, MotorcycleConfig config)
    {
        // Apply steering decay
        turnAngle *= config.steeringDecay;

        // Apply new steering input
        if (steerInput != 0)
        {
            float steeringMultiplier = CalculateSteeringMultiplier(currentSpeed, config);
            float steeringRate = config.turnRate * MathUtils.DegToRad * steeringMultiplier;
            turnAngle += steerInput * steeringRate * deltaTime;
        }

        // Apply turn angle to direction
        if (Math.Abs(turnAngle) > MathUtils.Epsilon)
        {
            float angleChange = turnAngle * deltaTime;
            direction = MathUtils.RotateVector(direction, angleChange);
            direction.Normalize();
        }
    }

    #endregion

    /// <summary>
    /// Advanced physics simulation with more realistic motorcycle dynamics
    /// Includes weight transfer, tire grip, and other advanced effects
    /// </summary>
    public static class Advanced
    {
        /// <summary>
        /// Simulate physics with tire grip and weight transfer effects
        /// </summary>
        public static void SimulateStepAdvanced(ref Vector2D position, ref Vector2D direction, ref float speed, 
                                              ref float turnAngle, float deltaTime, AgentInput input, 
                                              MotorcycleConfig config, float trackGrip = 1.0f)
        {
            // Calculate weight transfer effects
            float longitudinalAcceleration = CalculateLongitudinalAcceleration(speed, input.throttle, config);
            float weightTransferFactor = CalculateWeightTransfer(longitudinalAcceleration, config);
            
            // Adjust grip based on weight transfer and track conditions
            float effectiveGrip = trackGrip * weightTransferFactor;
            
            // Apply modified physics with grip effects
            var modifiedConfig = config;
            modifiedConfig.maxTractionForce *= effectiveGrip;
            
            SimulateStep(ref position, ref direction, ref speed, ref turnAngle, deltaTime, input, modifiedConfig);
        }

        private static float CalculateLongitudinalAcceleration(float speed, int throttleInput, MotorcycleConfig config)
        {
            float force = throttleInput > 0 ? 
                CalculateDrivingForce(config.enginePower, speed, config.maxTractionForce) :
                (throttleInput < 0 ? -config.brakingForce : 0f);
            
            float resistanceForce = CalculateResistanceForces(speed, config);
            float netForce = force - resistanceForce;
            
            return netForce / config.mass;
        }

        private static float CalculateWeightTransfer(float longitudinalAcceleration, MotorcycleConfig config)
        {
            // Simplified weight transfer calculation
            float accelerationFactor = Math.Abs(longitudinalAcceleration) / 10f; // Normalize to reasonable range
            return MathUtils.Clamp(1f - accelerationFactor * 0.2f, 0.5f, 1.2f);
        }
    }
}