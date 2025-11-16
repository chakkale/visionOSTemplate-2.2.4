using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.AddressableAssets;
#endif

[CreateAssetMenu(fileName = "RoomData", menuName = "Archviz/Room Data", order = 1)]
public class RoomData : ScriptableObject
{
    [Header("Room Settings")]
    public string roomName;
    
    [Header("Day/Night Textures - Addressables")]
    [Tooltip("Addressable reference for day mode texture (preferred for remote loading)")]
    public string dayTextureAddress;
    
    [Tooltip("Addressable reference for night mode texture (preferred for remote loading)")]
    public string nightTextureAddress;
    
    [Header("Day/Night Textures - Direct References")]
    [Tooltip("Direct texture used during day mode (legacy/fallback)")]
    public Texture2D dayTexture;
    
    [Tooltip("Direct texture used during night mode (legacy/fallback)")]
    public Texture2D nightTexture;
    
    [Header("Room Prefab")]
    public GameObject roomPrefab;
    
    [Header("Backwards Compatibility")]
    [Tooltip("Legacy room texture field - will be used as day texture if dayTexture is not set")]
    public Texture2D roomTexture;
    
    /// <summary>
    /// Gets the appropriate addressable address based on the current lighting mode
    /// </summary>
    /// <param name="isNightMode">True for night mode, false for day mode</param>
    /// <returns>The addressable address to use for the current lighting mode</returns>
    public string GetTextureAddressForMode(bool isNightMode)
    {
        if (isNightMode)
        {
            // Use night texture address if available, otherwise fall back to day texture address
            return !string.IsNullOrEmpty(nightTextureAddress) ? nightTextureAddress : dayTextureAddress;
        }
        else
        {
            return dayTextureAddress;
        }
    }
    
    /// <summary>
    /// Checks if this room uses addressables for texture loading
    /// </summary>
    /// <returns>True if addressable addresses are configured</returns>
    public bool UsesAddressables()
    {
        return !string.IsNullOrEmpty(dayTextureAddress);
    }
    
    /// <summary>
    /// Gets the appropriate texture based on the current lighting mode (legacy direct reference)
    /// </summary>
    /// <param name="isNightMode">True for night mode, false for day mode</param>
    /// <returns>The texture to use for the current lighting mode</returns>
    public Texture2D GetTextureForMode(bool isNightMode)
    {
        if (isNightMode)
        {
            // Use night texture if available, otherwise fall back to day texture
            return nightTexture != null ? nightTexture : GetDayTexture();
        }
        else
        {
            return GetDayTexture();
        }
    }
    
    /// <summary>
    /// Gets the day texture, with backwards compatibility support
    /// </summary>
    /// <returns>The day texture</returns>
    public Texture2D GetDayTexture()
    {
        // Use dayTexture if set, otherwise fall back to legacy roomTexture field
        return dayTexture != null ? dayTexture : roomTexture;
    }
    
    /// <summary>
    /// Checks if this room has night mode support
    /// </summary>
    /// <returns>True if night texture is available</returns>
    public bool HasNightMode()
    {
        return nightTexture != null;
    }
} 