using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FrostTextureGenerator : MonoBehaviour
{
    [Header("Frost Pattern Settings")]
    [SerializeField] private int textureSize = 512;
    [SerializeField] private float noiseScale = 10f;
    [SerializeField] private int octaves = 4;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private float lacunarity = 2f;
    [SerializeField] private AnimationCurve frostCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Output")]
    [SerializeField] private string textureFileName = "FrostPattern";
    
    public Texture2D GenerateFrostTexture()
    {
        Texture2D frostTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        
        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                float noiseValue = GeneratePerlinNoise(x, y);
                
                // Apply curve to create more interesting frost patterns
                noiseValue = frostCurve.Evaluate(noiseValue);
                
                // Create different patterns in R, G, B channels for distortion
                float r = noiseValue;
                float g = GeneratePerlinNoise(x + 100, y + 100);
                float b = GeneratePerlinNoise(x + 200, y + 200);
                
                Color pixelColor = new Color(r, g, b, 1f);
                frostTexture.SetPixel(x, y, pixelColor);
            }
        }
        
        frostTexture.Apply();
        return frostTexture;
    }
    
    private float GeneratePerlinNoise(int x, int y)
    {
        float value = 0f;
        float amplitude = 1f;
        float frequency = noiseScale / textureSize;
        
        for (int i = 0; i < octaves; i++)
        {
            value += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        
        return Mathf.Clamp01(value);
    }
    
#if UNITY_EDITOR
    [ContextMenu("Generate Frost Texture")]
    public void GenerateAndSaveTexture()
    {
        Texture2D frostTexture = GenerateFrostTexture();
        
        // Save as PNG
        byte[] pngData = frostTexture.EncodeToPNG();
        string path = $"Assets/Textures/{textureFileName}.png";
        
        // Create directory if it doesn't exist
        System.IO.Directory.CreateDirectory("Assets/Textures");
        
        System.IO.File.WriteAllBytes(path, pngData);
        
        // Refresh the asset database
        AssetDatabase.Refresh();
        
        // Import the texture with proper settings
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
        
        Debug.Log($"Frost texture generated and saved to: {path}");
        
        // Clean up
        DestroyImmediate(frostTexture);
    }
#endif
} 