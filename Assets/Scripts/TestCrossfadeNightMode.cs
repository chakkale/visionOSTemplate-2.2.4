using UnityEngine;
using System.Collections;

/// <summary>
/// Advanced test script for the crossfade night mode system
/// This script will automatically test the crossfade functionality when a room is loaded
/// </summary>
public class TestCrossfadeNightMode : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private float testDelay = 3f;
    [SerializeField] private bool autoTest = true;
    [SerializeField] private KeyCode manualTestKey = KeyCode.F;
    
    private bool hasTestedOnce = false;
    
    private void Start()
    {
        if (autoTest)
        {
            StartCoroutine(AutoTestCrossfade());
        }
    }
    
    private void Update()
    {
        // Manual test trigger
        if (Input.GetKeyDown(manualTestKey))
        {
            StartCoroutine(TestCrossfadeSequence());
        }
    }
    
    private IEnumerator AutoTestCrossfade()
    {
        // Wait for managers to be ready
        while (RoomManager.Instance == null || NightModeManager.Instance == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.Log("[TestCrossfade] Managers ready, waiting for room to load...");
        
        // Wait for a room to be loaded
        while (RoomManager.Instance.CurrentRoomData == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.Log($"[TestCrossfade] Room loaded: {RoomManager.Instance.CurrentRoomData.roomName}");
        
        // Wait a bit more for everything to settle
        yield return new WaitForSeconds(testDelay);
        
        if (!hasTestedOnce)
        {
            yield return StartCoroutine(TestCrossfadeSequence());
            hasTestedOnce = true;
        }
    }
    
    private IEnumerator TestCrossfadeSequence()
    {
        Debug.Log("=== STARTING CROSSFADE TEST SEQUENCE ===");
        
        if (RoomManager.Instance?.CurrentRoomData == null)
        {
            Debug.LogError("[TestCrossfade] No room loaded!");
            yield break;
        }
        
        var currentRoom = RoomManager.Instance.CurrentRoomData;
        Debug.Log($"[TestCrossfade] Testing with room: {currentRoom.roomName}");
        Debug.Log($"[TestCrossfade] Day texture: {currentRoom.GetDayTexture()?.name}");
        Debug.Log($"[TestCrossfade] Night texture: {currentRoom.nightTexture?.name}");
        Debug.Log($"[TestCrossfade] Has night mode: {currentRoom.HasNightMode()}");
        
        if (!currentRoom.HasNightMode())
        {
            Debug.LogWarning("[TestCrossfade] Room does not have night mode textures!");
            yield break;
        }
        
        // Test sequence: Day -> Night -> Day
        Debug.Log("=== CROSSFADE TO DAY MODE ===");
        NightModeManager.Instance.SetNightMode(false);
        
        yield return new WaitForSeconds(3f); // Wait for crossfade to complete
        
        Debug.Log("=== CROSSFADE TO NIGHT MODE ===");
        NightModeManager.Instance.SetNightMode(true);
        
        yield return new WaitForSeconds(3f); // Wait for crossfade to complete
        
        Debug.Log("=== CROSSFADE BACK TO DAY MODE ===");
        NightModeManager.Instance.SetNightMode(false);
        
        Debug.Log("=== CROSSFADE TEST SEQUENCE COMPLETE ===");
    }
    
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 120), 
            $"Crossfade Night Mode Test\n" +
            $"Auto Test: {autoTest}\n" +
            $"Manual Test Key: {manualTestKey}\n" +
            $"Room: {RoomManager.Instance?.CurrentRoomData?.roomName ?? "None"}\n" +
            $"Night Mode: {NightModeManager.Instance?.IsNightMode() ?? false}\n" +
            $"Has Tested: {hasTestedOnce}");
    }
}
