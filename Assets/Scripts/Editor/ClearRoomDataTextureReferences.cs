using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// Editor utility to clear direct texture references from RoomData assets.
/// This prevents textures from being included in the build when using addressables.
/// </summary>
public class ClearRoomDataTextureReferences : EditorWindow
{
    private bool clearDayTexture = true;
    private bool clearNightTexture = true;
    private bool clearLegacyTexture = true;
    private bool dryRun = true;
    
    [MenuItem("Tools/Addressables/Clear RoomData Texture References")]
    public static void ShowWindow()
    {
        GetWindow<ClearRoomDataTextureReferences>("Clear RoomData Textures");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Clear Direct Texture References", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This tool clears direct texture references from RoomData assets to prevent " +
            "textures from being included in the build. Only use this if you have set up " +
            "addressable texture addresses (dayTextureAddress, nightTextureAddress).",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        clearDayTexture = EditorGUILayout.Toggle("Clear dayTexture", clearDayTexture);
        clearNightTexture = EditorGUILayout.Toggle("Clear nightTexture", clearNightTexture);
        clearLegacyTexture = EditorGUILayout.Toggle("Clear roomTexture (legacy)", clearLegacyTexture);
        
        GUILayout.Space(10);
        
        dryRun = EditorGUILayout.Toggle("Dry Run (preview only)", dryRun);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Clear References", GUILayout.Height(30)))
        {
            ClearReferences();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("List All RoomData Assets", GUILayout.Height(25)))
        {
            ListRoomDataAssets();
        }
    }
    
    private void ClearReferences()
    {
        string[] guids = AssetDatabase.FindAssets("t:RoomData");
        
        if (guids.Length == 0)
        {
            Debug.LogWarning("[ClearRoomDataTextures] No RoomData assets found");
            return;
        }
        
        int processedCount = 0;
        int clearedCount = 0;
        
        Debug.Log($"[ClearRoomDataTextures] {(dryRun ? "DRY RUN - " : "")}Processing {guids.Length} RoomData assets...");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RoomData roomData = AssetDatabase.LoadAssetAtPath<RoomData>(path);
            
            if (roomData == null) continue;
            
            processedCount++;
            bool needsSave = false;
            
            // Check if addressables are configured
            if (!roomData.UsesAddressables())
            {
                Debug.LogWarning($"[ClearRoomDataTextures] {roomData.name}: No addressable addresses configured, skipping");
                continue;
            }
            
            // Clear day texture
            if (clearDayTexture && roomData.dayTexture != null)
            {
                Debug.Log($"[ClearRoomDataTextures] {roomData.name}: Clearing dayTexture reference");
                if (!dryRun)
                {
                    roomData.dayTexture = null;
                    needsSave = true;
                }
                clearedCount++;
            }
            
            // Clear night texture
            if (clearNightTexture && roomData.nightTexture != null)
            {
                Debug.Log($"[ClearRoomDataTextures] {roomData.name}: Clearing nightTexture reference");
                if (!dryRun)
                {
                    roomData.nightTexture = null;
                    needsSave = true;
                }
                clearedCount++;
            }
            
            // Clear legacy texture
            if (clearLegacyTexture && roomData.roomTexture != null)
            {
                Debug.Log($"[ClearRoomDataTextures] {roomData.name}: Clearing roomTexture (legacy) reference");
                if (!dryRun)
                {
                    roomData.roomTexture = null;
                    needsSave = true;
                }
                clearedCount++;
            }
            
            // Save if changes were made
            if (needsSave && !dryRun)
            {
                EditorUtility.SetDirty(roomData);
            }
        }
        
        if (!dryRun)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ClearRoomDataTextures] Completed! Processed {processedCount} assets, cleared references from {clearedCount} fields");
        }
        else
        {
            Debug.Log($"[ClearRoomDataTextures] DRY RUN completed! Would process {processedCount} assets and clear {clearedCount} references");
            Debug.Log($"[ClearRoomDataTextures] Uncheck 'Dry Run' to apply changes");
        }
    }
    
    private void ListRoomDataAssets()
    {
        string[] guids = AssetDatabase.FindAssets("t:RoomData");
        
        Debug.Log($"[ClearRoomDataTextures] === FOUND {guids.Length} ROOMDATA ASSETS ===");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RoomData roomData = AssetDatabase.LoadAssetAtPath<RoomData>(path);
            
            if (roomData == null) continue;
            
            string hasDay = roomData.dayTexture != null ? "✓" : "✗";
            string hasNight = roomData.nightTexture != null ? "✓" : "✗";
            string hasLegacy = roomData.roomTexture != null ? "✓" : "✗";
            string hasAddress = roomData.UsesAddressables() ? "✓" : "✗";
            
            Debug.Log($"[ClearRoomDataTextures] {roomData.name}: " +
                     $"dayTex={hasDay} nightTex={hasNight} legacyTex={hasLegacy} " +
                     $"addresses={hasAddress} (day:{roomData.dayTextureAddress}, night:{roomData.nightTextureAddress})");
        }
    }
}
