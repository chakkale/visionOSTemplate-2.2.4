#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor utility to automatically populate addressable addresses in RoomData assets
/// based on their current texture references
/// </summary>
public class RoomDataAddressableHelper
{
    [MenuItem("Tools/Addressables/Auto-Populate RoomData Addresses")]
    public static void AutoPopulateRoomDataAddresses()
    {
        // Find all RoomData assets
        string[] guids = AssetDatabase.FindAssets("t:RoomData");
        
        int updatedCount = 0;
        int skippedCount = 0;
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            RoomData roomData = AssetDatabase.LoadAssetAtPath<RoomData>(assetPath);
            
            if (roomData == null)
                continue;
            
            bool updated = false;
            
            // Auto-populate day texture address
            if (roomData.dayTexture != null && string.IsNullOrEmpty(roomData.dayTextureAddress))
            {
                roomData.dayTextureAddress = roomData.dayTexture.name;
                updated = true;
                Debug.Log($"Set dayTextureAddress for {roomData.roomName}: {roomData.dayTextureAddress}");
            }
            // Fallback to legacy roomTexture
            else if (roomData.roomTexture != null && string.IsNullOrEmpty(roomData.dayTextureAddress))
            {
                roomData.dayTextureAddress = roomData.roomTexture.name;
                updated = true;
                Debug.Log($"Set dayTextureAddress from roomTexture for {roomData.roomName}: {roomData.dayTextureAddress}");
            }
            
            // Auto-populate night texture address
            if (roomData.nightTexture != null && string.IsNullOrEmpty(roomData.nightTextureAddress))
            {
                roomData.nightTextureAddress = roomData.nightTexture.name;
                updated = true;
                Debug.Log($"Set nightTextureAddress for {roomData.roomName}: {roomData.nightTextureAddress}");
            }
            
            if (updated)
            {
                EditorUtility.SetDirty(roomData);
                updatedCount++;
            }
            else
            {
                skippedCount++;
            }
        }
        
        AssetDatabase.SaveAssets();
        
        Debug.Log($"=== RoomData Address Population Complete ===");
        Debug.Log($"Updated: {updatedCount} RoomData assets");
        Debug.Log($"Skipped: {skippedCount} RoomData assets (already populated or no textures)");
        
        EditorUtility.DisplayDialog("Address Population Complete", 
            $"Updated {updatedCount} RoomData assets\nSkipped {skippedCount} assets", "OK");
    }
    
    [MenuItem("Tools/Addressables/Validate RoomData Addresses")]
    public static void ValidateRoomDataAddresses()
    {
        // Find all RoomData assets
        string[] guids = AssetDatabase.FindAssets("t:RoomData");
        
        List<string> missingAddresses = new List<string>();
        List<string> validRooms = new List<string>();
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            RoomData roomData = AssetDatabase.LoadAssetAtPath<RoomData>(assetPath);
            
            if (roomData == null)
                continue;
            
            bool hasIssues = false;
            
            // Check day texture
            if (string.IsNullOrEmpty(roomData.dayTextureAddress))
            {
                missingAddresses.Add($"{roomData.roomName}: Missing dayTextureAddress");
                hasIssues = true;
            }
            
            // Check night texture (only warn if night texture exists but no address)
            if (roomData.nightTexture != null && string.IsNullOrEmpty(roomData.nightTextureAddress))
            {
                missingAddresses.Add($"{roomData.roomName}: Has night texture but missing nightTextureAddress");
                hasIssues = true;
            }
            
            if (!hasIssues)
            {
                validRooms.Add(roomData.roomName);
            }
        }
        
        Debug.Log("=== RoomData Address Validation ===");
        Debug.Log($"Valid rooms: {validRooms.Count}");
        Debug.Log($"Rooms with issues: {missingAddresses.Count}");
        
        if (missingAddresses.Count > 0)
        {
            Debug.LogWarning("Rooms with missing addresses:");
            foreach (string issue in missingAddresses)
            {
                Debug.LogWarning($"  - {issue}");
            }
            
            EditorUtility.DisplayDialog("Validation Issues Found", 
                $"{missingAddresses.Count} rooms have missing addressable addresses.\n\n" +
                "Run 'Auto-Populate RoomData Addresses' to fix automatically, or check the Console for details.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Validation Successful", 
                $"All {validRooms.Count} RoomData assets have valid addressable addresses!", "OK");
        }
    }
    
    [MenuItem("Tools/Addressables/Clear RoomData Addresses")]
    public static void ClearRoomDataAddresses()
    {
        bool confirm = EditorUtility.DisplayDialog("Clear All Addresses?", 
            "This will clear all addressable addresses from RoomData assets. This action cannot be undone.\n\n" +
            "Are you sure?", "Yes, Clear All", "Cancel");
        
        if (!confirm)
            return;
        
        // Find all RoomData assets
        string[] guids = AssetDatabase.FindAssets("t:RoomData");
        
        int clearedCount = 0;
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            RoomData roomData = AssetDatabase.LoadAssetAtPath<RoomData>(assetPath);
            
            if (roomData == null)
                continue;
            
            bool hadAddresses = !string.IsNullOrEmpty(roomData.dayTextureAddress) || 
                               !string.IsNullOrEmpty(roomData.nightTextureAddress);
            
            roomData.dayTextureAddress = "";
            roomData.nightTextureAddress = "";
            
            if (hadAddresses)
            {
                EditorUtility.SetDirty(roomData);
                clearedCount++;
            }
        }
        
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Cleared addressable addresses from {clearedCount} RoomData assets");
        EditorUtility.DisplayDialog("Addresses Cleared", 
            $"Cleared addresses from {clearedCount} RoomData assets", "OK");
    }
}
#endif
