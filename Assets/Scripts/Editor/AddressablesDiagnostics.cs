using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

/// <summary>
/// Quick diagnostic tool to check addressables status
/// </summary>
public class AddressablesDiagnostics : EditorWindow
{
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Addressables/Diagnostics")]
    public static void ShowWindow()
    {
        GetWindow<AddressablesDiagnostics>("Addressables Diagnostics");
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("Addressables Diagnostics", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Check All Texture Addresses", GUILayout.Height(30)))
        {
            CheckTextureAddresses();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Check RoomData Status", GUILayout.Height(30)))
        {
            CheckRoomDataStatus();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Check Scene Addressable Status", GUILayout.Height(30)))
        {
            CheckSceneStatus();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void CheckTextureAddresses()
    {
        Debug.Log("=== TEXTURE ADDRESSABLES DIAGNOSTIC ===");
        
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("Addressables settings not found!");
            return;
        }
        
        // Get all RoomData and collect texture addresses
        string[] roomDataGuids = AssetDatabase.FindAssets("t:RoomData");
        var requiredTextures = new System.Collections.Generic.HashSet<string>();
        
        foreach (string guid in roomDataGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RoomData roomData = AssetDatabase.LoadAssetAtPath<RoomData>(path);
            
            if (roomData != null)
            {
                if (!string.IsNullOrEmpty(roomData.dayTextureAddress))
                    requiredTextures.Add(roomData.dayTextureAddress);
                if (!string.IsNullOrEmpty(roomData.nightTextureAddress))
                    requiredTextures.Add(roomData.nightTextureAddress);
            }
        }
        
        Debug.Log($"Found {requiredTextures.Count} unique texture addresses required by RoomData");
        
        // Check which ones are addressable
        int addressableCount = 0;
        int missingCount = 0;
        
        foreach (string address in requiredTextures)
        {
            // Try to find this address in addressables
            bool found = false;
            foreach (var group in settings.groups)
            {
                foreach (var entry in group.entries)
                {
                    if (entry.address == address)
                    {
                        found = true;
                        Debug.Log($"✓ FOUND: {address} in group '{group.Name}'");
                        addressableCount++;
                        break;
                    }
                }
                if (found) break;
            }
            
            if (!found)
            {
                Debug.LogWarning($"✗ MISSING: {address} is NOT addressable!");
                missingCount++;
            }
        }
        
        Debug.Log($"\n=== SUMMARY ===");
        Debug.Log($"✓ Addressable: {addressableCount}");
        Debug.LogWarning($"✗ Missing: {missingCount}");
        
        if (missingCount > 0)
        {
            Debug.LogError($"\n⚠️ {missingCount} textures are NOT addressable!");
            Debug.LogError("Run: Tools > Addressables > Auto-Update Room Textures");
        }
    }
    
    private void CheckRoomDataStatus()
    {
        Debug.Log("=== ROOMDATA ADDRESSABLES STATUS ===");
        
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("Addressables settings not found!");
            return;
        }
        
        string[] guids = AssetDatabase.FindAssets("t:RoomData");
        
        int addressableCount = 0;
        int nonAddressableCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RoomData roomData = AssetDatabase.LoadAssetAtPath<RoomData>(path);
            
            if (roomData == null) continue;
            
            var entry = settings.FindAssetEntry(guid);
            if (entry != null)
            {
                Debug.Log($"✓ {roomData.name} is addressable");
                addressableCount++;
            }
            else
            {
                Debug.LogWarning($"✗ {roomData.name} is NOT addressable");
                nonAddressableCount++;
            }
        }
        
        Debug.Log($"\n=== SUMMARY ===");
        Debug.Log($"✓ Addressable: {addressableCount}");
        Debug.LogWarning($"✗ Not Addressable: {nonAddressableCount}");
        
        if (nonAddressableCount > 0)
        {
            Debug.LogError($"\n⚠️ {nonAddressableCount} RoomData assets are NOT addressable!");
            Debug.LogError("Run: Tools > Addressables > Make RoomData Assets Addressable");
        }
    }
    
    private void CheckSceneStatus()
    {
        Debug.Log("=== SCENE ADDRESSABLES STATUS ===");
        
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("Addressables settings not found!");
            return;
        }
        
        // Check MainFinal scene
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene MainFinal");
        
        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var entry = settings.FindAssetEntry(guid);
            
            if (entry != null)
            {
                Debug.Log($"✓ {path} is addressable in group '{entry.parentGroup.Name}'");
            }
            else
            {
                Debug.LogWarning($"✗ {path} is NOT addressable");
            }
        }
    }
}
