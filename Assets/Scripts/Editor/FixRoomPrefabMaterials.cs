using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Editor tool to ensure all room prefabs have the correct SkyFull material assigned
/// </summary>
public class FixRoomPrefabMaterials : EditorWindow
{
    [MenuItem("Tools/Fix Room Prefab Materials")]
    public static void ShowWindow()
    {
        GetWindow<FixRoomPrefabMaterials>("Fix Room Materials");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "This tool will:\n" +
            "1. Find the SkyFull material\n" +
            "2. Find all room prefabs\n" +
            "3. Assign SkyFull material to all MeshRenderers in each prefab\n" +
            "4. Save the prefabs",
            MessageType.Info
        );

        if (GUILayout.Button("Fix All Room Prefabs", GUILayout.Height(40)))
        {
            FixAllRoomPrefabs();
        }
    }

    private static void FixAllRoomPrefabs()
    {
        // Find SkyFull material
        string[] materialGuids = AssetDatabase.FindAssets("SkyFull t:Material");
        if (materialGuids.Length == 0)
        {
            Debug.LogError("[FixRoomPrefabs] Could not find SkyFull material!");
            EditorUtility.DisplayDialog("Error", "Could not find SkyFull material in project!", "OK");
            return;
        }

        string materialPath = AssetDatabase.GUIDToAssetPath(materialGuids[0]);
        Material skyFullMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        
        Debug.Log($"[FixRoomPrefabs] Found SkyFull material at: {materialPath}");

        // Find all room prefabs in Wizio/Rooms folder
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Wizio/Rooms" });
        
        int fixedCount = 0;
        int failedCount = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null) continue;

            // Get all MeshRenderers in the prefab
            MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[FixRoomPrefabs] No MeshRenderer found in: {prefab.name}");
                failedCount++;
                continue;
            }

            bool modified = false;
            foreach (MeshRenderer renderer in renderers)
            {
                // Check if material is missing or incorrect
                if (renderer.sharedMaterial == null || 
                    renderer.sharedMaterial.name.Contains("Error") ||
                    renderer.sharedMaterial != skyFullMaterial)
                {
                    renderer.sharedMaterial = skyFullMaterial;
                    modified = true;
                    Debug.Log($"[FixRoomPrefabs] Assigned SkyFull to {renderer.gameObject.name} in {prefab.name}");
                }
            }

            if (modified)
            {
                // Mark prefab as dirty and save
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
                fixedCount++;
                Debug.Log($"[FixRoomPrefabs] ✓ Fixed: {prefab.name}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = $"Fixed {fixedCount} prefabs\nFailed: {failedCount}";
        Debug.Log($"[FixRoomPrefabs] {message}");
        EditorUtility.DisplayDialog("Complete", message, "OK");
    }
}
