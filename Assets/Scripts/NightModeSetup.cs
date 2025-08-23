using UnityEngine;

/// <summary>
/// Helper component to automatically set up the Night Mode system in the scene
/// Add this to any GameObject in your scene to automatically configure night mode functionality
/// </summary>
public class NightModeSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Manual Setup")]
    [SerializeField] private bool createNightModeManager = true;
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupNightMode();
        }
    }
    
    /// <summary>
    /// Sets up the night mode system automatically
    /// </summary>
    [ContextMenu("Setup Night Mode System")]
    public void SetupNightMode()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[NightModeSetup] Setting up Night Mode system...");
        }
        
        // Check if NightModeManager already exists
        NightModeManager existingManager = FindFirstObjectByType<NightModeManager>();
        if (existingManager == null && createNightModeManager)
        {
            // Create NightModeManager
            GameObject managerGO = new GameObject("NightModeManager");
            managerGO.AddComponent<NightModeManager>();
            
            if (enableDebugLogs)
            {
                Debug.Log("[NightModeSetup] Created NightModeManager");
            }
        }
        else if (existingManager != null)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[NightModeSetup] NightModeManager already exists");
            }
        }
        
        // Verify other required components
        XRUIFlowManager uiFlowManager = FindFirstObjectByType<XRUIFlowManager>();
        if (uiFlowManager == null)
        {
            Debug.LogWarning("[NightModeSetup] XRUIFlowManager not found! Night mode toggle won't work without it.");
        }
        else if (enableDebugLogs)
        {
            Debug.Log("[NightModeSetup] XRUIFlowManager found ✓");
        }
        
        RoomManager roomManager = FindFirstObjectByType<RoomManager>();
        if (roomManager == null)
        {
            Debug.LogWarning("[NightModeSetup] RoomManager not found! Room textures won't change without it.");
        }
        else if (enableDebugLogs)
        {
            Debug.Log("[NightModeSetup] RoomManager found ✓");
        }
        
        if (enableDebugLogs)
        {
            Debug.Log("[NightModeSetup] Night Mode system setup complete!");
            Debug.Log("[NightModeSetup] Usage Instructions:");
            Debug.Log("1. Add night textures to your RoomData assets");
            Debug.Log("2. Use the DarkLightToggle in MainMenu to switch between day/night modes");
            Debug.Log("3. Night mode will automatically update room textures when toggled");
        }
    }
    
    /// <summary>
    /// Validates the current night mode setup
    /// </summary>
    [ContextMenu("Validate Night Mode Setup")]
    public void ValidateSetup()
    {
        Debug.Log("[NightModeSetup] === NIGHT MODE VALIDATION ===");
        
        // Check NightModeManager
        NightModeManager nightModeManager = FindFirstObjectByType<NightModeManager>();
        Debug.Log($"NightModeManager: {(nightModeManager != null ? "✓ Found" : "✗ Missing")}");
        
        // Check XRUIFlowManager
        XRUIFlowManager uiFlowManager = FindFirstObjectByType<XRUIFlowManager>();
        Debug.Log($"XRUIFlowManager: {(uiFlowManager != null ? "✓ Found" : "✗ Missing")}");
        
        // Check RoomManager
        RoomManager roomManager = FindFirstObjectByType<RoomManager>();
        Debug.Log($"RoomManager: {(roomManager != null ? "✓ Found" : "✗ Missing")}");
        
        // Check for RoomData assets with night textures
        RoomData[] roomDataAssets = Resources.FindObjectsOfTypeAll<RoomData>();
        int roomsWithNightTextures = 0;
        int totalRooms = roomDataAssets.Length;
        
        foreach (var roomData in roomDataAssets)
        {
            if (roomData.HasNightMode())
            {
                roomsWithNightTextures++;
            }
        }
        
        Debug.Log($"RoomData Assets: {totalRooms} total, {roomsWithNightTextures} with night textures");
        
        if (roomsWithNightTextures == 0 && totalRooms > 0)
        {
            Debug.LogWarning("[NightModeSetup] No RoomData assets have night textures configured. Night mode won't show different textures.");
        }
        
        // Check for DarkLightToggle in scene
        GameObject darkLightToggle = GameObject.Find("DarkLightToggle");
        Debug.Log($"DarkLightToggle: {(darkLightToggle != null ? "✓ Found" : "✗ Missing")}");
        
        Debug.Log("[NightModeSetup] === VALIDATION COMPLETE ===");
    }
    
    /// <summary>
    /// Lists all RoomData assets and their night mode support
    /// </summary>
    [ContextMenu("List RoomData Night Mode Support")]
    public void ListRoomDataNightModeSupport()
    {
        Debug.Log("[NightModeSetup] === ROOMDATA NIGHT MODE SUPPORT ===");
        
        RoomData[] roomDataAssets = Resources.FindObjectsOfTypeAll<RoomData>();
        
        if (roomDataAssets.Length == 0)
        {
            Debug.Log("No RoomData assets found");
            return;
        }
        
        foreach (var roomData in roomDataAssets)
        {
            string dayTexture = roomData.GetDayTexture() != null ? roomData.GetDayTexture().name : "None";
            string nightTexture = roomData.nightTexture != null ? roomData.nightTexture.name : "None";
            string nightSupport = roomData.HasNightMode() ? "✓" : "✗";
            
            Debug.Log($"{roomData.roomName}: Day='{dayTexture}', Night='{nightTexture}' {nightSupport}");
        }
        
        Debug.Log("[NightModeSetup] === END ROOMDATA LIST ===");
    }
}
