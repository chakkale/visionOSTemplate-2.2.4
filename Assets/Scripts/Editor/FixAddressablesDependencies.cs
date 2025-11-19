using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

/// <summary>
/// Ensures SkyFull material and its shader are properly included in addressables
/// so room prefabs can reference them when loaded dynamically
/// </summary>
public class FixAddressablesDependencies : EditorWindow
{
    [MenuItem("Tools/Fix Addressables Dependencies")]
    public static void ShowWindow()
    {
        GetWindow<FixAddressablesDependencies>("Fix Dependencies");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "This tool will:\n" +
            "1. Find SkyFull material\n" +
            "2. Find Stereo360Panorama shader\n" +
            "3. Add them to Addressables so room prefabs can reference them\n" +
            "4. This fixes the 'Hidden/InternalErrorShader' issue",
            MessageType.Info
        );

        if (GUILayout.Button("Fix Material & Shader Dependencies", GUILayout.Height(40)))
        {
            FixDependencies();
        }
    }

    private static void FixDependencies()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Error", "Addressables settings not found!", "OK");
            return;
        }

        // Find or create a "Dependencies" group
        var dependenciesGroup = settings.FindGroup("Dependencies");
        if (dependenciesGroup == null)
        {
            dependenciesGroup = settings.CreateGroup("Dependencies", false, false, false, null);
            Debug.Log("[FixDependencies] Created 'Dependencies' addressables group");
        }

        int addedCount = 0;

        // Add SkyFull material
        string[] materialGuids = AssetDatabase.FindAssets("SkyFull t:Material");
        if (materialGuids.Length > 0)
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(materialGuids[0]);
            var entry = settings.CreateOrMoveEntry(materialGuids[0], dependenciesGroup);
            entry.address = "SkyFull";
            Debug.Log($"[FixDependencies] Added SkyFull material: {materialPath}");
            addedCount++;
        }

        // Add Stereo360Panorama shader
        string[] shaderGuids = AssetDatabase.FindAssets("Stereo360Panorama t:Shader");
        foreach (string guid in shaderGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var entry = settings.CreateOrMoveEntry(guid, dependenciesGroup);
            entry.address = System.IO.Path.GetFileNameWithoutExtension(path);
            Debug.Log($"[FixDependencies] Added shader: {path}");
            addedCount++;
        }

        // Save settings
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        string message = $"Added {addedCount} dependencies to Addressables.\n\n" +
                        "Now rebuild addressables:\n" +
                        "Window → Asset Management → Addressables → Groups\n" +
                        "Build → New Build → Default Build Script";
        
        Debug.Log($"[FixDependencies] {message}");
        EditorUtility.DisplayDialog("Complete", message, "OK");
    }
}
