using UnityEngine;

[System.Serializable]
public class TrackPanelFixer : MonoBehaviour
{
  [Header("Fix Track Panel Issues")]
  [SerializeField] private bool autoFixOnStart = true;

  [Header("Material Assignment")]
  [SerializeField] private Material trackMaterial;

  void Start()
  {
    if (autoFixOnStart)
    {
      FixTrackPanel();
    }
  }

  [ContextMenu("Fix Track Panel")]
  public void FixTrackPanel()
  {
    // Find the TrackPanel component
    TrackPanel trackPanel = FindAnyObjectByType<TrackPanel>();

    if (trackPanel == null)
    {
      Debug.LogError("No TrackPanel found in the scene!");
      return;
    }

    Debug.Log("Found TrackPanel, checking configuration...");

    // Check if track material is assigned
    if (trackPanel.trackMaterial == null)
    {
      if (trackMaterial != null)
      {
        trackPanel.trackMaterial = trackMaterial;
        Debug.Log($"Assigned track material: {trackMaterial.name}");
      }
      else
      {
        // Try to find a track material in the project
        Material foundMaterial = FindTrackMaterial();
        if (foundMaterial != null)
        {
          trackPanel.trackMaterial = foundMaterial;
          Debug.Log($"Auto-assigned track material: {foundMaterial.name}");
        }
        else
        {
          Debug.LogWarning("No track material found. The system will create a default material when generating the mesh.");
        }
      }
    }
    else
    {
      Debug.Log($"Track material already assigned: {trackPanel.trackMaterial.name}");
    }

    // Check mesh generator
    TrackMeshGenerator meshGenerator = trackPanel.GetComponent<TrackMeshGenerator>();
    if (meshGenerator == null)
    {
      Debug.LogWarning("TrackMeshGenerator component not found on TrackPanel!");
    }
    else
    {
      Debug.Log("TrackMeshGenerator found and ready.");
    }

    Debug.Log("Track Panel fix complete!");
  }

  Material FindTrackMaterial()
  {
    // Try to find materials in the Materials folder using Resources.FindObjectsOfTypeAll
    Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();

    foreach (Material material in allMaterials)
    {
      if (material != null && material.name.ToLower().Contains("track"))
      {
        return material;
      }
    }

    return null;
  }

  [ContextMenu("Create Runtime Track Material")]
  public void CreateRuntimeTrackMaterial()
  {
    // Create a new material at runtime
    Material material = new Material(Shader.Find("Standard"));
    material.name = "Runtime Track Material";
    material.color = Color.white;
    material.SetFloat("_Metallic", 0.0f);
    material.SetFloat("_Glossiness", 0.3f);

    Debug.Log($"Created runtime track material: {material.name}");

    // Assign it to this component
    trackMaterial = material;

    // Try to assign it to the track panel
    FixTrackPanel();
  }

  [ContextMenu("Test Texture Application")]
  public void TestTextureApplication()
  {
    TrackPanel trackPanel = FindAnyObjectByType<TrackPanel>();
    if (trackPanel == null)
    {
      Debug.LogError("No TrackPanel found!");
      return;
    }

    // Check if there's an uploaded texture
    if (trackPanel.imagePreview != null && trackPanel.imagePreview.texture != null)
    {
      Debug.Log($"Found uploaded texture: {trackPanel.imagePreview.texture.name}");

      // Try to generate a test mesh
      trackPanel.GenerateTrackMesh();
    }
    else
    {
      Debug.LogWarning("No texture uploaded. Please upload a track image first.");
    }
  }
}