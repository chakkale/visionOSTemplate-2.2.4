#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.IO;

/// <summary>
/// Editor utility for setting up Addressables for remote content delivery
/// </summary>
public class AddressablesSetup
{
    [MenuItem("Tools/Addressables/Setup Remote Content Configuration")]
    public static void SetupAddressables()
    {
        // Create or get settings
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            settings = AddressableAssetSettings.Create(
                AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
                true,
                true
            );
            Debug.Log("Created new AddressableAssetSettings");
        }
        
        // Configure settings for remote content
        settings.BuildRemoteCatalog = true;
        settings.DisableCatalogUpdateOnStartup = false; // Enable automatic update checks
        settings.ContiguousBundles = true;
        
        // Create Remote profile
        CreateOrUpdateProfile(settings);
        
        // Create Remote group for panoramic textures
        CreateRemoteTexturesGroup(settings);
        
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        
        Debug.Log("Addressables setup complete! Next steps:");
        Debug.Log("1. Mark all panoramic textures as Addressable in the Remote_Textures group");
        Debug.Log("2. Build addressables: Window > Asset Management > Addressables > Groups > Build > New Build > Default Build Script");
        Debug.Log("3. Upload ServerData folder contents to GitHub Releases");
        Debug.Log("4. Update RemoteLoadPath in Addressables profiles with your GitHub Release URL");
    }
    
    private static void CreateOrUpdateProfile(AddressableAssetSettings settings)
    {
        var profileSettings = settings.profileSettings;
        string profileName = "Remote Content";
        
        string profileId = profileSettings.GetProfileId(profileName);
        if (string.IsNullOrEmpty(profileId))
        {
            profileId = profileSettings.AddProfile(profileName, null);
            Debug.Log($"Created profile: {profileName}");
        }
        
        // Set up remote load path variable
        string loadPathVarName = "RemoteLoadPath";
        string buildPathVarName = "RemoteBuildPath";
        
        // Check if variables exist by trying to get them
        try
        {
            profileSettings.GetValueById(profileId, loadPathVarName);
        }
        catch
        {
            profileSettings.CreateValue(loadPathVarName, "https://github.com/YOUR-USERNAME/YOUR-REPO/releases/download/v1.0.0");
        }
        
        try
        {
            profileSettings.GetValueById(profileId, buildPathVarName);
        }
        catch
        {
            profileSettings.CreateValue(buildPathVarName, "[UnityEngine.Application.dataPath]/../ServerData/[BuildTarget]");
        }
        
        // Set remote profile as active
        settings.activeProfileId = profileId;
        
        Debug.Log($"Profile '{profileName}' configured. Remember to update {loadPathVarName} with your actual GitHub Release URL!");
    }
    
    private static void CreateRemoteTexturesGroup(AddressableAssetSettings settings)
    {
        string groupName = "Remote_Textures";
        
        // Check if group already exists
        AddressableAssetGroup existingGroup = settings.FindGroup(groupName);
        if (existingGroup != null)
        {
            Debug.Log($"Group '{groupName}' already exists");
            return;
        }
        
        // Create new group
        AddressableAssetGroup group = settings.CreateGroup(groupName, false, false, true, null, 
            typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        
        // Configure bundle schema
        var bundleSchema = group.GetSchema<BundledAssetGroupSchema>();
        if (bundleSchema != null)
        {
            bundleSchema.BuildPath.SetVariableByName(settings, "RemoteBuildPath");
            bundleSchema.LoadPath.SetVariableByName(settings, "RemoteLoadPath");
            bundleSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately; // Each texture gets its own bundle
            bundleSchema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.OnlyHash; // Use hash names for versioning
            bundleSchema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4; // Fast decompression
            bundleSchema.IncludeInBuild = true;
            bundleSchema.UseAssetBundleCache = true; // Enable caching
            bundleSchema.UseAssetBundleCrc = true;
            bundleSchema.Timeout = 0; // No timeout
        }
        
        // Configure content update schema
        var updateSchema = group.GetSchema<ContentUpdateGroupSchema>();
        if (updateSchema != null)
        {
            updateSchema.StaticContent = false; // Allow updates
        }
        
        Debug.Log($"Created remote group: {groupName}");
    }
    
    [MenuItem("Tools/Addressables/Mark Panoramic Textures as Addressable")]
    public static void MarkPanoramicTexturesAsAddressable()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("Please run 'Setup Remote Content Configuration' first!");
            return;
        }
        
        AddressableAssetGroup group = settings.FindGroup("Remote_Textures");
        if (group == null)
        {
            Debug.LogError("Remote_Textures group not found. Run setup first!");
            return;
        }
        
        // Find all textures in Wizio/Renders folder
        string searchPath = "Assets/Wizio/Renders";
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { searchPath });
        
        int addedCount = 0;
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            
            // Check if already addressable
            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                // Add to addressables
                entry = settings.CreateOrMoveEntry(guid, group, false, false);
                
                // Set address to filename without extension
                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                entry.SetAddress(fileName);
                
                addedCount++;
            }
        }
        
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Marked {addedCount} panoramic textures as addressable in Remote_Textures group");
        Debug.Log($"Total addressable textures: {guids.Length}");
    }
}
#endif
