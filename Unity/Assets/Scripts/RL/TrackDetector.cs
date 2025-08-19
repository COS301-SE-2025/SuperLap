using UnityEngine;

public static class TrackDetector
{
    public static bool CheckIfPathGoesOffTrack(float steeringInput, Vector3 currentPosition, Vector3 currentForward, 
                                             float currentSpeed, float currentTurnAngle, float throttleInput, 
                                             float trajectoryLength, int recommendationSteps, float offTrackThreshold, 
                                             MotorcyclePhysicsConfig physicsConfig, TrackDetectionConfig trackConfig, 
                                             out float offTrackRatio)
    {
        // Simulation state - start with current state
        Vector3 simPosition = currentPosition;
        Vector3 simForward = currentForward;
        float simSpeed = currentSpeed;
        float simTurnAngle = currentTurnAngle;
        
        float deltaTime = trajectoryLength / recommendationSteps;
        int offTrackPoints = 0;
        int totalCheckPoints = Mathf.Min(recommendationSteps, 8); // Check first 8 points for performance
        
        // Simulate forward and check if any predicted positions are off track
        for (int step = 0; step < totalCheckPoints; step++)
        {
            // Simulate one step forward with current throttle and provided steering
            TrajectoryPredictor.SimulateOneStepWithInputs(ref simPosition, ref simForward, ref simSpeed, ref simTurnAngle, 
                                                        deltaTime, throttleInput, steeringInput, physicsConfig);
            
            // Check if this position is on track using raycast
            if (!IsPositionOnTrack(simPosition, trackConfig))
            {
                offTrackPoints++;
            }
        }
        
        // Consider path as "going off track" if more than the threshold of check points are off track
        offTrackRatio = (float)offTrackPoints / totalCheckPoints;
        bool goingOffTrack = offTrackRatio > offTrackThreshold;
        
        return goingOffTrack;
    }
    
    public static bool IsPositionOnTrack(Vector3 position, TrackDetectionConfig config)
    {
        // Cast a ray downward from the position to check if there's track beneath
        Vector3 rayOrigin = position + Vector3.up * config.raycastStartHeight;
        Vector3 rayDirection = Vector3.down;
        
        // Perform the raycast
        RaycastHit hit;
        bool hitTrack = false;
        
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, config.raycastDistance))
        {
            if (hit.collider.CompareTag("Track"))
            {
                hitTrack = true;
            }
        }
        
        // Debug visualization
        if (config.showTrackDetectionDebug)
        {
            Color rayColor = hitTrack ? config.onTrackRayColor : config.offTrackRayColor;
            Vector3 rayEnd = hit.collider != null ? hit.point : rayOrigin + rayDirection * config.raycastDistance;
            Debug.DrawLine(rayOrigin, rayEnd, rayColor, 0.1f);
        }
        
        return hitTrack;
    }
}

[System.Serializable]
public struct TrackDetectionConfig
{
    public float raycastStartHeight;
    public float raycastDistance;
    public bool showTrackDetectionDebug;
    public Color onTrackRayColor;
    public Color offTrackRayColor;
}
