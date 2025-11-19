using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.IO;
using System.Linq;

/// <summary>
/// Automatically configures Addressables for all textures and room prefabs
/// Window -> Addressables -> Auto Setup All Content
/// </summary>
public class AddressablesAutoSetup : EditorWindow
{
    private static string texturesPath = "Assets/Wizio/Renders"; // 3.4GB of 8192x8192 textures
    private static string roomsPath = "Assets/Wizio/Rooms";
    
    [MenuItem("Window/Addressables/Auto Setup All Content")]
    public static void ShowWindow()
    {
        GetWindow<AddressablesAutoSetup>("Addressables Auto Setup");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Addressables Auto Configuration", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This will automatically mark all room textures and prefabs as addressable.\n\n" +
            "Scans:\n" +
            "- Assets/Wizio/Renders (3.4GB of 8192x8192 textures)\n" +
            "- Assets/Wizio/Rooms (36 room prefabs)\n" +
            "- Assets/Wizio/MainFinal.unity (main scene)\n\n" +
            "Creates groups:\n" +
            "- Remote_Textures_All (all 360° textures)\n" +
            "- Remote_Rooms_All (all room prefabs)\n" +
            "- Remote_MainScene (MainFinal.unity)\n\n" +
            "Expected result: ~3.5GB bundle",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Setup All Addressables", GUILayout.Height(40)))
        {
            SetupAddressables();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Clear All Addressables", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear Addressables?", 
                "This will remove addressable marks from all assets. Continue?", "Yes", "No"))
            {
                ClearAddressables();
            }
        }
    }
    
    private static void SetupAddressables()
    {
        Debug.Log("[AddressablesAutoSetup] Starting automatic addressables setup...");
        
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[AddressablesAutoSetup] Addressables settings not found! Initialize addressables first.");
            return;
        }
        
        // Find or create groups
        var texturesGroup = FindOrCreateGroup(settings, "Remote_Textures_All", true);
        var roomsGroup = FindOrCreateGroup(settings, "Remote_Rooms_All", true);
        var mainSceneGroup = FindOrCreateGroup(settings, "Remote_MainScene", true);
        
        int textureCount = 0;
        int prefabCount = 0;
        
        // 1. Add all textures from Assets/Textures
        if (Directory.Exists(texturesPath))
        {
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { texturesPath });
            Debug.Log($"[AddressablesAutoSetup] Found {textureGuids.Length} textures in {texturesPath}");
            
            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                
                // Use filename as address (e.g., "SG_Int_E-4D_360_C02")
                var entry = settings.CreateOrMoveEntry(guid, texturesGroup, false, false);
                entry.address = fileName;
                textureCount++;
                
                if (textureCount % 10 == 0)
                    Debug.Log($"[AddressablesAutoSetup] Processed {textureCount} textures...");
            }
            
            Debug.Log($"[AddressablesAutoSetup] ✓ Added {textureCount} textures to Remote_Textures_All");
        }
        else
        {
            Debug.LogWarning($"[AddressablesAutoSetup] Textures path not found: {texturesPath}");
        }
        
        // 2. Add all room prefabs from Assets/Wizio/Rooms
        if (Directory.Exists(roomsPath))
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { roomsPath });
            Debug.Log($"[AddressablesAutoSetup] Found {prefabGuids.Length} prefabs in {roomsPath}");
            
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                
                // Use filename as address (e.g., "E-2A-C02")
                var entry = settings.CreateOrMoveEntry(guid, roomsGroup, false, false);
                entry.address = fileName;
                prefabCount++;
            }
            
            Debug.Log($"[AddressablesAutoSetup] ✓ Added {prefabCount} room prefabs to Remote_Rooms_All");
        }
        else
        {
            Debug.LogWarning($"[AddressablesAutoSetup] Rooms path not found: {roomsPath}");
        }
        
        // 3. Ensure MainFinal scene is addressable
        string mainScenePath = "Assets/Wizio/MainFinal.unity";
        string mainSceneGuid = AssetDatabase.AssetPathToGUID(mainScenePath);
        
        if (!string.IsNullOrEmpty(mainSceneGuid))
        {
            var sceneEntry = settings.CreateOrMoveEntry(mainSceneGuid, mainSceneGroup, false, false);
            sceneEntry.address = "MainFinal";
            Debug.Log($"[AddressablesAutoSetup] ✓ Added MainFinal.unity to Remote_MainScene");
        }
        else
        {
            Debug.LogWarning($"[AddressablesAutoSetup] MainFinal.unity not found at {mainScenePath}");
        }
        
        // Save settings
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"[AddressablesAutoSetup] ========== SETUP COMPLETE ==========");
        Debug.Log($"[AddressablesAutoSetup] Textures: {textureCount}");
        Debug.Log($"[AddressablesAutoSetup] Room Prefabs: {prefabCount}");
        Debug.Log($"[AddressablesAutoSetup] Total addressables: {textureCount + prefabCount + 1}");
        Debug.Log($"[AddressablesAutoSetup] Next: Build → New Build → Default Build Script");
        
        EditorUtility.DisplayDialog("Addressables Setup Complete!", 
            $"✓ {textureCount} textures\n" +
            $"✓ {prefabCount} room prefabs\n" +
            $"✓ MainFinal scene\n\n" +
            $"Total: {textureCount + prefabCount + 1} addressables\n\n" +
            "Next step: Build addressables content",
            "OK");
    }
    
    private static AddressableAssetGroup FindOrCreateGroup(AddressableAssetSettings settings, string groupName, bool isRemote)
    {
        // Try to find existing group
        var group = settings.groups.FirstOrDefault(g => g.Name == groupName);
        
        if (group == null)
        {
            Debug.Log($"[AddressablesAutoSetup] Creating new group: {groupName}");
            group = settings.CreateGroup(groupName, false, false, true, null, typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema), typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema));
            
            // Configure as remote group
            var schema = group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
            if (schema != null && isRemote)
            {
                schema.BuildPath.SetVariableByName(settings, "RemoteBuildPath");
                schema.LoadPath.SetVariableByName(settings, "RemoteLoadPath");
                schema.BundleMode = UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                schema.Compression = UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            }
        }
        
        return group;
    }
    
    private static void ClearAddressables()
    {
        Debug.Log("[AddressablesAutoSetup] Clearing all addressables...");
        
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[AddressablesAutoSetup] Addressables settings not found!");
            return;
        }
        
        // Remove all entries from custom groups
        var groupsToProcess = settings.groups.Where(g => 
            g.Name.StartsWith("Remote_Textures") || 
            g.Name.StartsWith("Remote_Rooms") ||
            g.Name == "Remote_MainScene"
        ).ToList();
        
        int removed = 0;
        foreach (var group in groupsToProcess)
        {
            var entries = group.entries.ToList();
            foreach (var entry in entries)
            {
                settings.RemoveAssetEntry(entry.guid);
                removed++;
            }
        }
        
        AssetDatabase.SaveAssets();
        
        Debug.Log($"[AddressablesAutoSetup] ✓ Cleared {removed} addressable entries");
        EditorUtility.DisplayDialog("Addressables Cleared", $"Removed {removed} addressable entries", "OK");
    }
}
