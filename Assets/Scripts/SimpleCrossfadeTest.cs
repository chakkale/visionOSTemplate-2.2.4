using UnityEngine;

/// <summary>
/// Simple crossfade test - press SPACE to test the true crossfade effect
/// </summary>
public class SimpleCrossfadeTest : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestCrossfade();
        }
        
        // Auto-correct to day mode on start
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (NightModeManager.Instance != null)
            {
                NightModeManager.Instance.SetNightMode(false);
                Debug.Log("Forced DAY mode");
            }
        }
        
        // Force night mode
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (NightModeManager.Instance != null)
            {
                NightModeManager.Instance.SetNightMode(true);
                Debug.Log("Forced NIGHT mode");
            }
        }
    }
    
    private void TestCrossfade()
    {
        if (RoomManager.Instance?.CurrentRoomData == null)
        {
            Debug.LogWarning("No room loaded! Load a room first.");
            return;
        }
        
        if (NightModeManager.Instance == null)
        {
            Debug.LogWarning("NightModeManager not found!");
            return;
        }
        
        bool currentMode = NightModeManager.Instance.IsNightMode();
        string fromMode = currentMode ? "NIGHT" : "DAY";
        string toMode = !currentMode ? "NIGHT" : "DAY";
        
        Debug.Log($"=== TRUE CROSSFADE: {fromMode} → {toMode} ===");
        NightModeManager.Instance.SetNightMode(!currentMode);
    }
    
    private void OnGUI()
    {
        string roomName = RoomManager.Instance?.CurrentRoomData?.roomName ?? "None";
        bool nightMode = NightModeManager.Instance?.IsNightMode() ?? false;
        
        GUI.Label(new Rect(10, 10, 400, 100), 
            $"True Crossfade Test\n" +
            $"Room: {roomName}\n" +
            $"Mode: {(nightMode ? "NIGHT" : "DAY")}\n" +
            $"SPACE: Toggle crossfade\n" +
            $"D: Force day | N: Force night");
    }
}
