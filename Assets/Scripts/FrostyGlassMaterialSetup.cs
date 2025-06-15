using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class FrostyGlassPreset
{
    [Header("Glass Properties")]
    public Color tintColor = new Color(0.8f, 0.9f, 1f, 0.3f);
    public float smoothness = 0.9f;
    public float fresnelPower = 1.5f;
    public Color fresnelColor = Color.white;
    
    [Header("Frost Effect")]
    public float blurAmount = 3f;
    public float backgroundDarken = 0.4f;
    public float distortionStrength = 0.02f;
    public float frostIntensity = 0.8f;
    public float frostScale = 2f;
}

public class FrostyGlassMaterialSetup : MonoBehaviour
{
    [Header("Material Settings")]
    [SerializeField] private Material targetMaterial;
    [SerializeField] private Texture2D frostPattern;
    
    [Header("Presets")]
    [SerializeField] private FrostyGlassPreset lightFrost = new FrostyGlassPreset
    {
        tintColor = new Color(0.9f, 0.95f, 1f, 0.2f),
        blurAmount = 2f,
        backgroundDarken = 0.3f,
        frostIntensity = 0.5f,
        frostScale = 1.5f
    };
    
    [SerializeField] private FrostyGlassPreset mediumFrost = new FrostyGlassPreset
    {
        tintColor = new Color(0.8f, 0.9f, 1f, 0.4f),
        blurAmount = 4f,
        backgroundDarken = 0.5f,
        frostIntensity = 1f,
        frostScale = 2f
    };
    
    [SerializeField] private FrostyGlassPreset heavyFrost = new FrostyGlassPreset
    {
        tintColor = new Color(0.7f, 0.85f, 1f, 0.6f),
        blurAmount = 6f,
        backgroundDarken = 0.7f,
        frostIntensity = 1.5f,
        frostScale = 3f
    };

    private void Awake()
    {
        // Auto-setup if material is assigned
        if (targetMaterial == null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
                targetMaterial = renderer.material;
        }
    }

    public void ApplyPreset(FrostyGlassPreset preset)
    {
        if (targetMaterial == null)
        {
            Debug.LogWarning("No target material assigned!");
            return;
        }

        // Apply glass properties
        targetMaterial.SetColor("_Color", preset.tintColor);
        targetMaterial.SetFloat("_Smoothness", preset.smoothness);
        targetMaterial.SetFloat("_FresnelPower", preset.fresnelPower);
        targetMaterial.SetColor("_FresnelColor", preset.fresnelColor);

        // Apply frost effects
        targetMaterial.SetFloat("_BlurAmount", preset.blurAmount);
        targetMaterial.SetFloat("_BackgroundDarken", preset.backgroundDarken);
        targetMaterial.SetFloat("_DistortionStrength", preset.distortionStrength);
        targetMaterial.SetFloat("_FrostIntensity", preset.frostIntensity);
        targetMaterial.SetFloat("_FrostScale", preset.frostScale);

        // Apply frost pattern if available
        if (frostPattern != null)
            targetMaterial.SetTexture("_FrostPattern", frostPattern);
    }

    [ContextMenu("Apply Light Frost")]
    public void ApplyLightFrost() => ApplyPreset(lightFrost);

    [ContextMenu("Apply Medium Frost")]
    public void ApplyMediumFrost() => ApplyPreset(mediumFrost);

    [ContextMenu("Apply Heavy Frost")]
    public void ApplyHeavyFrost() => ApplyPreset(heavyFrost);

#if UNITY_EDITOR
    [ContextMenu("Create Frosty Glass Material")]
    public void CreateFrostyGlassMaterial()
    {
        // Find the shader
        Shader frostyGlassShader = Shader.Find("Custom/URP/FrostyGlass");
        if (frostyGlassShader == null)
        {
            Debug.LogError("FrostyGlass shader not found! Make sure the shader is properly imported.");
            return;
        }

        // Create material
        Material newMaterial = new Material(frostyGlassShader);
        
        // Create Materials directory if it doesn't exist
        System.IO.Directory.CreateDirectory("Assets/Materials");
        
        // Save the material
        string path = "Assets/Materials/FrostyGlassMaterial.mat";
        AssetDatabase.CreateAsset(newMaterial, path);
        AssetDatabase.SaveAssets();
        
        // Assign to target
        targetMaterial = newMaterial;
        
        // Apply medium frost by default
        ApplyMediumFrost();
        
        Debug.Log($"Frosty Glass material created at: {path}");
        
        // Assign to renderer if available
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = newMaterial;
            Debug.Log("Material assigned to renderer.");
        }
    }
#endif
} 