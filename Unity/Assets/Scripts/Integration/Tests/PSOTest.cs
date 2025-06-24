using UnityEngine;

public class PSOTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Starting PSO Test...");
        ImageProcessing.TrackBoundaries boundaries = ImageProcessing.ProcessImage(Application.dataPath.Replace("Unity/Assets", "Backend/ImageProcessing/trackImages/test.png"));
        Debug.Log($"Processing result: Success = {boundaries.success}, Error = {boundaries.errorMessage}");
        PSOIntegrator.RunPSO(boundaries.innerBoundary, boundaries.outerBoundary, 100, 60000);
        Debug.Log("PSO Test completed.");
    }
}
