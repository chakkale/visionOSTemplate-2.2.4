#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.IO;

/// <summary>
/// Convenient editor window for managing addressables remote content workflow
/// </summary>
public class AddressablesWorkflowWindow : EditorWindow
{
    private string remoteUrl = "https://github.com/YOUR-USERNAME/YOUR-REPO/releases/download/v1.0.0";
    private Vector2 scrollPosition;
    private bool isSetupComplete = false;
    private bool areTexturesMarked = false;
    private bool areAddressesPopulated = false;
    private int textureCount = 0;
    private int roomDataCount = 0;
    
    [MenuItem("Window/Addressables Workflow")]
    public static void ShowWindow()
    {
        var window = GetWindow<AddressablesWorkflowWindow>("Addressables Workflow");
        window.minSize = new Vector2(500, 600);
    }
    
    private void OnEnable()
    {
        RefreshStatus();
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Addressables Remote Content Workflow", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Follow these steps to set up remote content delivery for your visionOS app.", MessageType.Info);
        
        GUILayout.Space(10);
        DrawStatusSection();
        
        GUILayout.Space(10);
        DrawSetupSection();
        
        GUILayout.Space(10);
        DrawBuildSection();
        
        GUILayout.Space(10);
        DrawDeploySection();
        
        GUILayout.Space(10);
        DrawUtilitySection();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawStatusSection()
    {
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        
        DrawStatusItem("Addressables Setup", isSetupComplete);
        DrawStatusItem($"Textures Marked ({textureCount} found)", areTexturesMarked);
        DrawStatusItem($"RoomData Addresses ({roomDataCount} rooms)", areAddressesPopulated);
        
        if (GUILayout.Button("Refresh Status"))
        {
            RefreshStatus();
        }
    }
    
    private void DrawStatusItem(string label, bool status)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(status ? "✓" : "○", GUILayout.Width(20));
        GUILayout.Label(label);
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawSetupSection()
    {
        EditorGUILayout.LabelField("1. Setup", EditorStyles.boldLabel);
        
        if (!isSetupComplete)
        {
            EditorGUILayout.HelpBox("Run initial setup to create Addressables configuration.", MessageType.Warning);
            if (GUILayout.Button("Setup Remote Content Configuration"))
            {
                AddressablesSetup.SetupAddressables();
                RefreshStatus();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Setup complete! ✓", MessageType.Info);
        }
        
        GUILayout.Space(5);
        
        if (!areTexturesMarked)
        {
            if (isSetupComplete)
            {
                if (GUILayout.Button("Mark Panoramic Textures as Addressable"))
                {
                    AddressablesSetup.MarkPanoramicTexturesAsAddressable();
                    RefreshStatus();
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("Mark Panoramic Textures (Setup Required First)");
                EditorGUI.EndDisabledGroup();
            }
        }
        else
        {
            EditorGUILayout.HelpBox($"{textureCount} textures marked as addressable ✓", MessageType.Info);
        }
        
        GUILayout.Space(5);
        
        if (!areAddressesPopulated)
        {
            if (GUILayout.Button("Auto-Populate RoomData Addresses"))
            {
                RoomDataAddressableHelper.AutoPopulateRoomDataAddresses();
                RefreshStatus();
            }
        }
        else
        {
            EditorGUILayout.HelpBox($"{roomDataCount} RoomData addresses populated ✓", MessageType.Info);
        }
        
        if (GUILayout.Button("Validate RoomData Addresses"))
        {
            RoomDataAddressableHelper.ValidateRoomDataAddresses();
        }
    }
    
    private void DrawBuildSection()
    {
        EditorGUILayout.LabelField("2. Configure & Build", EditorStyles.boldLabel);
        
        EditorGUILayout.LabelField("Remote Load Path:", EditorStyles.miniBoldLabel);
        remoteUrl = EditorGUILayout.TextField(remoteUrl);
        EditorGUILayout.HelpBox("This should be your GitHub Release URL:\nhttps://github.com/USERNAME/REPO/releases/download/v1.0.0", MessageType.Info);
        
        if (GUILayout.Button("Update Remote Load Path"))
        {
            UpdateRemoteLoadPath(remoteUrl);
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Open Addressables Groups Window"))
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
        }
        
        if (GUILayout.Button("Build Addressables Content"))
        {
            BuildAddressablesContent();
        }
        
        // Check if ServerData exists
        string serverDataPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ServerData");
        if (Directory.Exists(serverDataPath))
        {
            EditorGUILayout.HelpBox($"ServerData folder exists at: {serverDataPath}", MessageType.Info);
            if (GUILayout.Button("Open ServerData Folder"))
            {
                EditorUtility.RevealInFinder(serverDataPath);
            }
        }
    }
    
    private void DrawDeploySection()
    {
        EditorGUILayout.LabelField("3. Deploy", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "1. Create GitHub Release with tag matching your Remote Load Path\n" +
            "2. Upload all files from ServerData/VisionOS/ to the release\n" +
            "3. Publish the release", 
            MessageType.Info);
        
        if (GUILayout.Button("Open GitHub Releases (in browser)"))
        {
            // Extract repo URL from remoteUrl if possible
            if (remoteUrl.Contains("github.com"))
            {
                string repoUrl = remoteUrl.Split(new[] { "/releases/" }, System.StringSplitOptions.None)[0];
                Application.OpenURL(repoUrl + "/releases/new");
            }
            else
            {
                Application.OpenURL("https://github.com");
            }
        }
    }
    
    private void DrawUtilitySection()
    {
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Open Addressables Profiles"))
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Profiles");
        }
        
        if (GUILayout.Button("Open Addressables Settings"))
        {
            Selection.activeObject = AddressableAssetSettingsDefaultObject.Settings;
        }
        
        GUILayout.Space(5);
        
        EditorGUILayout.LabelField("Cleanup:", EditorStyles.miniBoldLabel);
        
        if (GUILayout.Button("Clear Build Cache"))
        {
            if (EditorUtility.DisplayDialog("Clear Build Cache?", 
                "This will delete the AddressableAssetsData build cache. You'll need to rebuild.", "Clear", "Cancel"))
            {
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings != null)
                {
                    string buildPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, 
                        "Library", "com.unity.addressables");
                    if (Directory.Exists(buildPath))
                    {
                        Directory.Delete(buildPath, true);
                        Debug.Log("Build cache cleared");
                    }
                }
            }
        }
        
        GUI.color = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("Clear All RoomData Addresses"))
        {
            RoomDataAddressableHelper.ClearRoomDataAddresses();
            RefreshStatus();
        }
        GUI.color = Color.white;
    }
    
    private void RefreshStatus()
    {
        // Check if setup is complete
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        isSetupComplete = settings != null;
        
        // Check if textures are marked
        if (isSetupComplete)
        {
            var group = settings.FindGroup("Remote_Textures");
            areTexturesMarked = group != null && group.entries.Count > 0;
            textureCount = areTexturesMarked ? group.entries.Count : 0;
        }
        
        // Check if RoomData addresses are populated
        string[] roomGuids = AssetDatabase.FindAssets("t:RoomData");
        roomDataCount = roomGuids.Length;
        int populatedCount = 0;
        
        foreach (string guid in roomGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            RoomData roomData = AssetDatabase.LoadAssetAtPath<RoomData>(assetPath);
            
            if (roomData != null && !string.IsNullOrEmpty(roomData.dayTextureAddress))
            {
                populatedCount++;
            }
        }
        
        areAddressesPopulated = populatedCount > 0 && populatedCount == roomDataCount;
        
        Repaint();
    }
    
    private void UpdateRemoteLoadPath(string url)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Error", "Addressables not set up yet!", "OK");
            return;
        }
        
        var profileSettings = settings.profileSettings;
        var profileId = profileSettings.GetProfileId("Remote Content");
        
        if (string.IsNullOrEmpty(profileId))
        {
            EditorUtility.DisplayDialog("Error", "Remote Content profile not found. Run setup first!", "OK");
            return;
        }
        
        // Update the RemoteLoadPath variable
        try
        {
            profileSettings.SetValue(profileId, "RemoteLoadPath", url);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Updated RemoteLoadPath to: {url}");
            EditorUtility.DisplayDialog("Success", $"Remote Load Path updated to:\n{url}", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update RemoteLoadPath: {e.Message}");
            EditorUtility.DisplayDialog("Error", "RemoteLoadPath variable not found! Run setup first.", "OK");
        }
    }
    
    private void BuildAddressablesContent()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Error", "Addressables not set up yet!", "OK");
            return;
        }
        
        if (EditorUtility.DisplayDialog("Build Addressables Content", 
            "This will build all addressable content. Continue?", "Build", "Cancel"))
        {
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent();
            
            EditorUtility.DisplayDialog("Build Complete", 
                "Addressables content built successfully!\n\nCheck ServerData folder for output.", "OK");
            
            RefreshStatus();
        }
    }
}
#endif
