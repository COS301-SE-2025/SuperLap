using UnityEngine;
using UnityEngine.UI;

public class ImageProcessingTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ImageProcessing.TrackBoundaries boundaries = ImageProcessing.ProcessImage(Application.dataPath.Replace("Unity/Assets", "Backend/ImageProcessing/trackImages/test.png"));
        if (boundaries.success)
        {
            Debug.Log("Track boundaries processed successfully.");
        }
        else
        {
            Debug.LogError("Failed to process track boundaries: " + boundaries.errorMessage);
        }
    }
}
