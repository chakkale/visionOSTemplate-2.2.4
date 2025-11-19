using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Automated workflow for updating room textures in addressables.
/// After you update RoomData assets with new texture addresses, run this to:
/// 1. Find all referenced textures
/// 2. Mark them as addressable in Remote_Textures group
/// 3. Build addressables
/// 4. (Optional) Auto-run upload script
/// </summary>
public class AutoUpdateRoomTextures : EditorWindow
{
    private string groupName = "Remote_Textures";
    private bool autoBuildAddressables = true;
    private bool showDetailedLog = true;
    private bool removeUnusedTextures = false;
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Addressables/Auto-Update Room Textures")]
    public static void ShowWindow()
    {
        var window = GetWindow<AutoUpdateRoomTextures>("Auto-Update Textures");
        window.minSize = new Vector2(500, 400);
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("Automated Room Texture Update", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "WORKFLOW:\n" +
            "1. Update your RoomData assets with new dayTextureAddress/nightTextureAddress\n" +
            "2. Click 'Scan & Update' - this will find all textures and make them addressable\n" +
            "3. (Optional) Build addressables automatically\n" +
            "4. Run ./update-release.sh to upload to GitHub",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        groupName = EditorGUILayout.TextField("Target Group", groupName);
        autoBuildAddressables = EditorGUILayout.Toggle("Auto-Build Addressables", autoBuildAddressables);
        showDetailedLog = EditorGUILayout.Toggle("Show Detailed Log", showDetailedLog);
        removeUnusedTextures = EditorGUILayout.Toggle("Remove Unused Textures", removeUnusedTextures);
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            removeUnusedTextures ? 
            "⚠️ Remove Unused: Will remove textures from addressables if not referenced by any RoomData" :
            "Keep Unused: Will keep all existing addressable textures even if not currently used",
            removeUnusedTextures ? MessageType.Warning : MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🔄 Scan & Update All", GUILayout.Height(40)))
        {
            ScanAndUpdateTextures();
        }
        
        GUILayout.Space(10);
        
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📋 List All RoomData"))
        {
            ListAllRoomData();
        }
        if (GUILayout.Button("🖼️ List All Texture Addresses"))
        {
            ListAllTextureAddresses();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("✅ Verify Texture References"))
        {
            VerifyTextureReferences();
        }
        if (GUILayout.Button("🔨 Build Addressables"))
        {
            BuildAddressables();
        }
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "After building, run this in terminal:\n./update-release.sh",
            MessageType.Info
        );
        
        EditorGUILayout.EndScrollView();
    }
    
    private void ScanAndUpdateTextures()
    {
        Debug.Log("=== AUTO-UPDATE ROOM TEXTURES STARTED ===");
        
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Error", "Addressables settings not found!", "OK");
            return;
        }
        
        var group = settings.FindGroup(groupName);
        if (group == null)
        {
            EditorUtility.DisplayDialog("Error", $"Group '{groupName}' not found!", "OK");
            return;
        }
        
        // Step 1: Find all RoomData assets
        string[] roomDataGuids = AssetDatabase.FindAssets("t:RoomData");
        var allTextureAddresses = new HashSet<string>();
        var textureGuidMap = new Dictionary<string, string>(); // address -> GUID
        
        Debug.Log($"[AutoUpdate] Found {roomDataGuids.Length} RoomData assets");
        
        // Step 2: Collect all texture addresses from RoomData
        foreach (string guid in roomDataGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RoomData roomData = AssetDatabase.LoadAssetAtPath<RoomData>(path);
            
            if (roomData == null) continue;
            
            if (!string.IsNullOrEmpty(roomData.dayTextureAddress))
            {
                allTextureAddresses.Add(roomData.dayTextureAddress);
                if (showDetailedLog)
                    Debug.Log($"[AutoUpdate] {roomData.name}: day texture = {roomData.dayTextureAddress}");
            }
            
            if (!string.IsNullOrEmpty(roomData.nightTextureAddress))
            {
                allTextureAddresses.Add(roomData.nightTextureAddress);
                if (showDetailedLog)
                    Debug.Log($"[AutoUpdate] {roomData.name}: night texture = {roomData.nightTextureAddress}");
            }
        }
        
        Debug.Log($"[AutoUpdate] Collected {allTextureAddresses.Count} unique texture addresses");
        
        // Step 3: Find texture assets by searching for matching addresses
        string[] allTextureGuids = AssetDatabase.FindAssets("t:Texture2D");
        
        foreach (string textureGuid in allTextureGuids)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(textureGuid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(texturePath);
            
            // Check if this texture's filename matches any address
            if (allTextureAddresses.Contains(fileName))
            {
                textureGuidMap[fileName] = textureGuid;
            }
        }
        
        Debug.Log($"[AutoUpdate] Mapped {textureGuidMap.Count} textures to addresses");
        
        // Step 4: Make textures addressable
        int addedCount = 0;
        int updatedCount = 0;
        int skippedCount = 0;
        
        foreach (var kvp in textureGuidMap)
        {
            string address = kvp.Key;
            string guid = kvp.Value;
            
            var entry = settings.FindAssetEntry(guid);
            
            if (entry == null)
            {
                // Create new entry
                var newEntry = settings.CreateOrMoveEntry(guid, group, false, false);
                newEntry.address = address;
                newEntry.SetLabel("remote", true, true);
                
                if (showDetailedLog)
                    Debug.Log($"[AutoUpdate] ✓ Added: {address}");
                addedCount++;
            }
            else if (entry.parentGroup != group)
            {
                // Move to correct group
                settings.MoveEntry(entry, group, false, false);
                entry.address = address;
                entry.SetLabel("remote", true, true);
                
                if (showDetailedLog)
                    Debug.Log($"[AutoUpdate] ↻ Moved: {address} from '{entry.parentGroup.Name}' to '{groupName}'");
                updatedCount++;
            }
            else
            {
                // Already in correct group, just update address
                entry.address = address;
                entry.SetLabel("remote", true, true);
                if (showDetailedLog)
                    Debug.Log($"[AutoUpdate] ✓ Already addressable: {address}");
                skippedCount++;
            }
        }
        
        // Step 5: Remove unused textures if requested
        int removedCount = 0;
        if (removeUnusedTextures)
        {
            var allEntries = new List<AddressableAssetEntry>(group.entries);
            foreach (var entry in allEntries)
            {
                if (entry.AssetPath.Contains("Texture") || entry.AssetPath.Contains("Render"))
                {
                    string entryAddress = entry.address;
                    if (!allTextureAddresses.Contains(entryAddress) && 
                        !textureGuidMap.ContainsKey(entryAddress))
                    {
                        settings.RemoveAssetEntry(entry.guid);
                        Debug.Log($"[AutoUpdate] ✗ Removed unused: {entryAddress}");
                        removedCount++;
                    }
                }
            }
        }
        
        // Save changes
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();
        
        // Summary
        Debug.Log("=== AUTO-UPDATE COMPLETED ===");
        Debug.Log($"✓ Added: {addedCount}");
        Debug.Log($"↻ Updated: {updatedCount}");
        Debug.Log($"○ Already correct: {skippedCount}");
        if (removeUnusedTextures)
            Debug.Log($"✗ Removed unused: {removedCount}");
        Debug.Log($"Total textures in addressables: {addedCount + updatedCount + skippedCount}");
        
        EditorUtility.DisplayDialog(
            "Update Complete",
            $"✓ Added: {addedCount}\n" +
            $"↻ Updated: {updatedCount}\n" +
            $"○ Already correct: {skippedCount}\n" +
            (removeUnusedTextures ? $"✗ Removed unused: {removedCount}\n" : "") +
            $"\nTotal: {addedCount + updatedCount + skippedCount} textures",
            "OK"
        );
        
        // Auto-build if requested
        if (autoBuildAddressables)
        {
            Debug.Log("[AutoUpdate] Auto-building addressables...");
            BuildAddressables();
        }
    }
    
    private void ListAllRoomData()
    {
        string[] guids = AssetDatabase.FindAssets("t:RoomData");
        Debug.Log($"=== ALL ROOMDATA ASSETS ({guids.Length}) ===");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RoomData roomData = AssetDatabase.LoadAssetAtPath<RoomData>(path);
            
            if (roomData == null) continue;
            
            Debug.Log($"{roomData.name}:\n" +
                     $"  Day: {roomData.dayTextureAddress}\n" +
                     $"  Night: {roomData.nightTextureAddress}\n" +
                     $"  Path: {path}");
        }
    }
    
    private void ListAllTextureAddresses()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return;
        
        var group = settings.FindGroup(groupName);
        if (group == null) return;
        
        var textureEntries = group.entries
            .Where(e => e.AssetPath.Contains("Texture") || e.AssetPath.Contains("Render"))
            .ToList();
        
        Debug.Log($"=== ADDRESSABLE TEXTURES IN '{groupName}' ({textureEntries.Count}) ===");
        
        foreach (var entry in textureEntries)
        {
            Debug.Log($"{entry.address} -> {entry.AssetPath}");
        }
    }
    
    private void VerifyTextureReferences()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return;
        
        string[] roomDataGuids = AssetDatabase.FindAssets("t:RoomData");
        var missingTextures = new List<string>();
        var foundTextures = new List<string>();
        
        Debug.Log("=== VERIFYING TEXTURE REFERENCES ===");
        
        foreach (string guid in roomDataGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RoomData roomData = AssetDatabase.LoadAssetAtPath<RoomData>(path);
            
            if (roomData == null) continue;
            
            // Check day texture
            if (!string.IsNullOrEmpty(roomData.dayTextureAddress))
            {
                if (FindTextureByAddress(roomData.dayTextureAddress) != null)
                {
                    foundTextures.Add($"{roomData.name} -> {roomData.dayTextureAddress}");
                }
                else
                {
                    missingTextures.Add($"{roomData.name} -> {roomData.dayTextureAddress} (DAY)");
                }
            }
            
            // Check night texture
            if (!string.IsNullOrEmpty(roomData.nightTextureAddress) && 
                roomData.nightTextureAddress != roomData.dayTextureAddress)
            {
                if (FindTextureByAddress(roomData.nightTextureAddress) != null)
                {
                    foundTextures.Add($"{roomData.name} -> {roomData.nightTextureAddress}");
                }
                else
                {
                    missingTextures.Add($"{roomData.name} -> {roomData.nightTextureAddress} (NIGHT)");
                }
            }
        }
        
        Debug.Log($"✓ Found: {foundTextures.Count} texture references");
        Debug.Log($"✗ Missing: {missingTextures.Count} texture references");
        
        if (missingTextures.Count > 0)
        {
            Debug.LogWarning("=== MISSING TEXTURES ===");
            foreach (var missing in missingTextures)
            {
                Debug.LogWarning($"✗ {missing}");
            }
        }
        
        EditorUtility.DisplayDialog(
            "Verification Complete",
            $"✓ Found: {foundTextures.Count}\n" +
            $"✗ Missing: {missingTextures.Count}\n\n" +
            (missingTextures.Count > 0 ? "Check console for details" : "All textures found!"),
            "OK"
        );
    }
    
    private Texture2D FindTextureByAddress(string address)
    {
        string[] guids = AssetDatabase.FindAssets($"{address} t:Texture2D");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (fileName == address)
            {
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }
        return null;
    }
    
    private void BuildAddressables()
    {
        Debug.Log("[AutoUpdate] Building addressables...");
        UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent();
        Debug.Log("[AutoUpdate] Build complete! Run ./update-release.sh to upload");
        
        EditorUtility.DisplayDialog(
            "Build Complete",
            "Addressables built successfully!\n\n" +
            "Next step: Run ./update-release.sh in terminal to upload to GitHub",
            "OK"
        );
    }
}
