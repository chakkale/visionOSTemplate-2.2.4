using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.IO;
using System.Linq;

/// <summary>
/// Editor utility to make all RoomData assets addressable
/// </summary>
public class MakeRoomDataAddressable : EditorWindow
{
    private string groupName = "Remote_Textures";
    private bool dryRun = true;
    
    [MenuItem("Tools/Addressables/Make RoomData Assets Addressable")]
    public static void ShowWindow()
    {
        GetWindow<MakeRoomDataAddressable>("Make RoomData Addressable");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Make RoomData Assets Addressable", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This tool adds all RoomData ScriptableObject assets to an addressables group. " +
            "This ensures they are downloaded remotely and not included in the build.",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        groupName = EditorGUILayout.TextField("Target Group Name", groupName);
        dryRun = EditorGUILayout.Toggle("Dry Run (preview only)", dryRun);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Make RoomData Addressable", GUILayout.Height(30)))
        {
            MakeAddressable();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("List Current Status", GUILayout.Height(25)))
        {
            ListCurrentStatus();
        }
    }
    
    private void MakeAddressable()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[MakeRoomDataAddressable] Addressables settings not found");
            return;
        }
        
        // Find or create the group
        var group = settings.FindGroup(groupName);
        if (group == null)
        {
            Debug.LogError($"[MakeRoomDataAddressable] Group '{groupName}' not found");
            return;
        }
        
        // Find all RoomData assets
        string[] guids = AssetDatabase.FindAssets("t:RoomData");
        
        if (guids.Length == 0)
        {
            Debug.LogWarning("[MakeRoomDataAddressable] No RoomData assets found");
            return;
        }
        
        int addedCount = 0;
        int skippedCount = 0;
        
        Debug.Log($"[MakeRoomDataAddressable] {(dryRun ? "DRY RUN - " : "")}Processing {guids.Length} RoomData assets...");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RoomData roomData = AssetDatabase.LoadAssetAtPath<RoomData>(path);
            
            if (roomData == null) continue;
            
            // Check if already addressable
            var entry = settings.FindAssetEntry(guid);
            if (entry != null)
            {
                Debug.Log($"[MakeRoomDataAddressable] {roomData.name}: Already addressable in group '{entry.parentGroup.Name}'");
                skippedCount++;
                continue;
            }
            
            if (!dryRun)
            {
                // Create addressable entry
                var newEntry = settings.CreateOrMoveEntry(guid, group, false, false);
                newEntry.address = roomData.name; // Use the RoomData name as the address
                newEntry.SetLabel("remote", true, true);
                
                Debug.Log($"[MakeRoomDataAddressable] {roomData.name}: Added to group '{groupName}' with address '{roomData.name}'");
                addedCount++;
            }
            else
            {
                Debug.Log($"[MakeRoomDataAddressable] {roomData.name}: Would add to group '{groupName}' with address '{roomData.name}'");
                addedCount++;
            }
        }
        
        if (!dryRun)
        {
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"[MakeRoomDataAddressable] Completed! Added {addedCount} assets, skipped {skippedCount} (already addressable)");
        }
        else
        {
            Debug.Log($"[MakeRoomDataAddressable] DRY RUN completed! Would add {addedCount} assets, skip {skippedCount}");
            Debug.Log($"[MakeRoomDataAddressable] Uncheck 'Dry Run' to apply changes");
        }
    }
    
    private void ListCurrentStatus()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[MakeRoomDataAddressable] Addressables settings not found");
            return;
        }
        
        string[] guids = AssetDatabase.FindAssets("t:RoomData");
        
        Debug.Log($"[MakeRoomDataAddressable] === ROOMDATA ADDRESSABLE STATUS ({guids.Length} assets) ===");
        
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
                Debug.Log($"[MakeRoomDataAddressable] ✓ {roomData.name}: Addressable in '{entry.parentGroup.Name}' with address '{entry.address}'");
                addressableCount++;
            }
            else
            {
                Debug.Log($"[MakeRoomDataAddressable] ✗ {roomData.name}: NOT addressable (path: {path})");
                nonAddressableCount++;
            }
        }
        
        Debug.Log($"[MakeRoomDataAddressable] Summary: {addressableCount} addressable, {nonAddressableCount} not addressable");
    }
}
