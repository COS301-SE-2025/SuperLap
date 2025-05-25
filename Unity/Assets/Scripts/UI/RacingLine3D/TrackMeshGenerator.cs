using UnityEngine;

public class TrackMeshGenerator : MonoBehaviour
{
    [Header("Texture Settings")]
    private bool useImageAsTexture = true;
    private bool useVertexColors = true;
    private bool separateTextures = false;
    private Texture2D colorTexture;
    
    public GameObject GenerateTrackMesh(Texture2D trackImage, int resolution, float baseHeight, float maxHeight, Material material)
    {
        if (trackImage == null)
        {
            Debug.LogError("Track image is null");
            return null;
        }
        
        // Create new GameObject for the mesh
        GameObject meshObject = new GameObject("Generated Track Mesh");
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
        
        // Generate the mesh
        Mesh mesh = CreateMeshFromImage(trackImage, resolution, baseHeight, maxHeight);
        
        if (mesh != null)
        {
            meshFilter.mesh = mesh;
            
            // Apply material with proper texture
            Material finalMaterial = CreateMaterialWithTexture(material, trackImage);
            meshRenderer.material = finalMaterial;
                
            // Add collider for interaction
            MeshCollider collider = meshObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            
            Debug.Log($"Generated track mesh with {mesh.vertexCount} vertices and {mesh.triangles.Length / 3} triangles");
            return meshObject;
        }
        
        DestroyImmediate(meshObject);
        return null;
    }
    
    // Texture control methods
    public void SetUseImageAsTexture(bool value)
    {
        useImageAsTexture = value;
    }
    
    public void SetUseVertexColors(bool value)
    {
        useVertexColors = value;
    }
    
    public void SetSeparateTextures(bool value)
    {
        separateTextures = value;
    }
    
    public void SetColorTexture(Texture2D texture)
    {
        colorTexture = texture;
    }
    
    Material CreateMaterialWithTexture(Material baseMaterial, Texture2D trackTexture)
    {
        Material material;
        
        if (baseMaterial != null)
        {
            // Create a new instance of the base material
            material = new Material(baseMaterial);
            Debug.Log($"Using base material: {baseMaterial.name} with shader: {baseMaterial.shader.name}");
        }
        else
        {
            // Create default material
            material = CreateDefaultTrackMaterial(trackTexture);
            Debug.Log($"Created default material with shader: {material.shader.name}");
        }
        
        // Apply texture based on settings
        if (useImageAsTexture && trackTexture != null)
        {
            // Set the main texture
            material.mainTexture = trackTexture;
            
            // For URP/HDRP compatibility, also set _BaseMap
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", trackTexture);
                Debug.Log("Applied texture to _BaseMap property");
            }
            
            // Also set _Color/_BaseColor to white to ensure texture is visible
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }
            
            // If using separate textures and we have a color texture
            if (separateTextures && colorTexture != null)
            {
                material.mainTexture = colorTexture;
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", colorTexture);
                }
                Debug.Log("Applied separate color texture");
            }
            
            Debug.Log($"Applied texture: {trackTexture.name} ({trackTexture.width}x{trackTexture.height}) to material: {material.name}");
        }
        else
        {
            Debug.LogWarning($"Texture not applied - useImageAsTexture: {useImageAsTexture}, trackTexture: {trackTexture != null}");
        }
        
        return material;
    }
    
    Mesh CreateMeshFromImage(Texture2D image, int resolution, float baseHeight, float maxHeight)
    {
        // Clamp resolution to reasonable bounds
        resolution = Mathf.Clamp(resolution, 10, 500);
        
        int width = resolution;
        int height = resolution;
        
        // Create vertices
        Vector3[] vertices = new Vector3[(width + 1) * (height + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        
        // Sample the image and create height map
        for (int y = 0; y <= height; y++)
        {
            for (int x = 0; x <= width; x++)
            {
                int index = y * (width + 1) + x;
                
                // Calculate UV coordinates
                float u = (float)x / width;
                float v = (float)y / height;
                uvs[index] = new Vector2(u, v);
                
                // Sample the image at this UV coordinate
                Color pixelColor = SampleImageBilinear(image, u, v);
                colors[index] = pixelColor;
                
                // Calculate height based on pixel brightness
                float brightness = pixelColor.grayscale;
                float heightValue = Mathf.Lerp(baseHeight, maxHeight, brightness);
                
                // Create vertex position
                // Scale X and Z to create a reasonable sized track
                float scaleX = 10.0f; // Adjust this to change track width
                float scaleZ = 10.0f; // Adjust this to change track length
                
                vertices[index] = new Vector3(
                    (u - 0.5f) * scaleX,
                    heightValue,
                    (v - 0.5f) * scaleZ
                );
            }
        }
        
        // Create triangles
        int[] triangles = new int[width * height * 6];
        int triangleIndex = 0;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int bottomLeft = y * (width + 1) + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (y + 1) * (width + 1) + x;
                int topRight = topLeft + 1;
                
                // First triangle (bottom-left, top-left, top-right)
                triangles[triangleIndex] = bottomLeft;
                triangles[triangleIndex + 1] = topLeft;
                triangles[triangleIndex + 2] = topRight;
                
                // Second triangle (bottom-left, top-right, bottom-right)
                triangles[triangleIndex + 3] = bottomLeft;
                triangles[triangleIndex + 4] = topRight;
                triangles[triangleIndex + 5] = bottomRight;
                
                triangleIndex += 6;
            }
        }
        
        // Create the mesh
        Mesh mesh = new Mesh();
        mesh.name = "Track Mesh";
        
        // Check if we need to use 32-bit indices
        if (vertices.Length > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;
        
        // Calculate normals and tangents
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    Color SampleImageBilinear(Texture2D image, float u, float v)
    {
        // Clamp UV coordinates
        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);
        
        // Convert to pixel coordinates
        float x = u * (image.width - 1);
        float y = v * (image.height - 1);
        
        // Get integer coordinates
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = Mathf.Min(x0 + 1, image.width - 1);
        int y1 = Mathf.Min(y0 + 1, image.height - 1);
        
        // Get fractional parts
        float fx = x - x0;
        float fy = y - y0;
        
        // Sample four pixels
        Color c00 = image.GetPixel(x0, y0);
        Color c10 = image.GetPixel(x1, y0);
        Color c01 = image.GetPixel(x0, y1);
        Color c11 = image.GetPixel(x1, y1);
        
        // Bilinear interpolation
        Color c0 = Color.Lerp(c00, c10, fx);
        Color c1 = Color.Lerp(c01, c11, fx);
        return Color.Lerp(c0, c1, fy);
    }
    
    Material CreateDefaultTrackMaterial(Texture2D trackTexture)
    {
        // Try to find URP Lit shader first, then Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        if (shader == null)
        {
            // Fallback to built-in diffuse
            shader = Shader.Find("Legacy Shaders/Diffuse");
        }
        
        Material material = new Material(shader);
        material.name = "Generated Track Material";
        
        // Apply texture
        if (trackTexture != null)
        {
            material.mainTexture = trackTexture;
            
            // For URP compatibility
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", trackTexture);
            }
        }
        
        // Set material properties
        material.color = Color.white;
        
        // Set properties based on shader type
        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0.0f);
        }
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.3f);
        }
        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0.3f);
        }
        
        return material;
    }
} 