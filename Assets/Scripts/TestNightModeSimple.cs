using UnityEngine;

/// <summary>
/// Simple test script to verify night mode functionality
/// Add this to any GameObject in the scene and press T to toggle night mode
/// </summary>
public class TestNightModeSimple : MonoBehaviour
{
    [Header("Test Controls")]
    [SerializeField] private KeyCode toggleKey = KeyCode.T;
    
    private void Update()
    {
        // Press T to toggle night mode
        if (Input.GetKeyDown(toggleKey))
        {
            if (NightModeManager.Instance != null)
            {
                bool currentMode = NightModeManager.Instance.IsNightMode();
                NightModeManager.Instance.SetNightMode(!currentMode);
                Debug.Log($"[TestNightMode] Toggled to {(!currentMode ? "NIGHT" : "DAY")} mode");
            }
            else
            {
                Debug.LogError("[TestNightMode] NightModeManager.Instance is null!");
            }
        }
    }
    
    private void OnGUI()
    {
        // Display test instructions
        GUI.Label(new Rect(10, 10, 300, 60), 
            $"Night Mode Test\nPress '{toggleKey}' to toggle\n" +
            $"Current: {(NightModeManager.Instance?.IsNightMode() == true ? "NIGHT" : "DAY")}");
    }
}
