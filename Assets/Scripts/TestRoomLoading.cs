using UnityEngine;

public class TestRoomLoading : MonoBehaviour
{
    [ContextMenu("Load Patio Room")]
    public void LoadPatioRoom()
    {
        Debug.Log("[TestRoomLoading] Loading Patio room...");
        
        // Load the Patio RoomData asset
        RoomData patioData = UnityEngine.Resources.Load<RoomData>("Patio");
        if (patioData == null)
        {
            // Try direct path loading
#if UNITY_EDITOR
            patioData = UnityEditor.AssetDatabase.LoadAssetAtPath<RoomData>("Assets/Wizio/Rooms/Patio/Patio.asset");
#endif
        }
        
        if (patioData != null)
        {
            Debug.Log($"[TestRoomLoading] Loaded Patio data: {patioData.roomName}");
            
            // Teleport to the room via RoomManager
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.TeleportToRoom(patioData);
                Debug.Log("[TestRoomLoading] Called TeleportToRoom");
            }
            else
            {
                Debug.LogError("[TestRoomLoading] RoomManager.Instance is null!");
            }
        }
        else
        {
            Debug.LogError("[TestRoomLoading] Failed to load Patio data!");
        }
    }
    
    [ContextMenu("Test Night Mode")]
    public void TestNightMode()
    {
        Debug.Log("[TestRoomLoading] Testing night mode...");
        
        var nightModeManager = FindFirstObjectByType<NightModeManager>();
        if (nightModeManager != null)
        {
            // Toggle night mode
            bool currentMode = nightModeManager.IsNightMode();
            nightModeManager.SetNightMode(!currentMode);
            Debug.Log($"[TestRoomLoading] Toggled night mode from {currentMode} to {!currentMode}");
        }
        else
        {
            Debug.LogError("[TestRoomLoading] NightModeManager not found!");
        }
    }
    
    [ContextMenu("Load Room Then Test Night Mode")]
    public void LoadRoomThenTestNightMode()
    {
        LoadPatioRoom();
        
        // Wait a moment for room to load then test night mode
        Invoke(nameof(TestNightMode), 2f);
    }
}
