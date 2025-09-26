[System.Serializable]
public struct RecommendationConfig
{
    public int recommendationSteps;
    public float steeringSensitivity;
    public float throttleSensitivity;
    public float testInputStrength;
    public float inputThreshold;
    public float offTrackThreshold;
    public float maxSpeedRatio;
    public float trajectoryLength;
}
