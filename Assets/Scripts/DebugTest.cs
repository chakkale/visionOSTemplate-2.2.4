using UnityEngine;

public class DebugTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== DEBUG TEST START ===");
        Debug.LogWarning("=== DEBUG TEST WARNING ===");
        Debug.LogError("=== DEBUG TEST ERROR ===");
        
        for (int i = 0; i < 3; i++)
        {
            Debug.Log($"Debug test iteration: {i}");
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("=== SPACE KEY PRESSED ===");
        }
    }
}
