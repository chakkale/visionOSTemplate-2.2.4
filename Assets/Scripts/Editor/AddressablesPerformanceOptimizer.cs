using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

/// <summary>
/// Optimizes Addressables settings for better download performance
/// Window -> Addressables -> Optimize Performance Settings
/// </summary>
public class AddressablesPerformanceOptimizer : EditorWindow
{
    [MenuItem("Window/Addressables/Optimize Performance Settings")]
    public static void ShowWindow()
    {
        GetWindow<AddressablesPerformanceOptimizer>("Addressables Performance");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Addressables Performance Settings", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Current Settings:\n" +
            "• Max Concurrent Web Requests: 3 (default)\n" +
            "• Catalog Requests Timeout: 0 (no timeout)\n" +
            "• Bundle Timeout: 0 (no timeout)\n" +
            "• Bundle Retry Count: 0 (no retries)\n\n" +
            "Optimized Settings:\n" +
            "• Max Concurrent Web Requests: 10 (faster parallel downloads)\n" +
            "• Catalog Requests Timeout: 30 seconds\n" +
            "• Bundle Timeout: 60 seconds\n" +
            "• Bundle Retry Count: 3 (auto-retry on failure)\n" +
            "• Bundle Redirect Limit: 5 (handle redirects)",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Apply Optimized Settings", GUILayout.Height(40)))
        {
            ApplyOptimizations();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("View Current Settings", GUILayout.Height(30)))
        {
            ShowCurrentSettings();
        }
    }
    
    private static void ApplyOptimizations()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[AddressablesOptimizer] Addressables settings not found!");
            EditorUtility.DisplayDialog("Error", "Addressables settings not found!", "OK");
            return;
        }
        
        Debug.Log("[AddressablesOptimizer] Applying performance optimizations...");
        
        // Increase max concurrent web requests from 3 to 10
        settings.MaxConcurrentWebRequests = 10;
        Debug.Log("[AddressablesOptimizer] ✓ Max Concurrent Web Requests: 3 → 10");
        
        // Set catalog requests timeout (in seconds)
        settings.CatalogRequestsTimeout = 30;
        Debug.Log("[AddressablesOptimizer] ✓ Catalog Requests Timeout: 0 → 30 seconds");
        
        // Set bundle timeout (in seconds)
        settings.BundleTimeout = 60;
        Debug.Log("[AddressablesOptimizer] ✓ Bundle Timeout: 0 → 60 seconds");
        
        // Set bundle retry count
        settings.BundleRetryCount = 3;
        Debug.Log("[AddressablesOptimizer] ✓ Bundle Retry Count: 0 → 3");
        
        // Set bundle redirect limit
        settings.BundleRedirectLimit = 5;
        Debug.Log("[AddressablesOptimizer] ✓ Bundle Redirect Limit: -1 → 5");
        
        // Save settings
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        
        Debug.Log("[AddressablesOptimizer] ========== OPTIMIZATION COMPLETE ==========");
        Debug.Log("[AddressablesOptimizer] Settings saved. Rebuild addressables for changes to take effect.");
        
        EditorUtility.DisplayDialog("Optimization Complete!", 
            "✓ Max Concurrent Requests: 10\n" +
            "✓ Catalog Timeout: 30s\n" +
            "✓ Bundle Timeout: 60s\n" +
            "✓ Retry Count: 3\n" +
            "✓ Redirect Limit: 5\n\n" +
            "Next: Rebuild addressables content\n" +
            "(Window → Addressables → Groups → Build)",
            "OK");
    }
    
    private static void ShowCurrentSettings()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[AddressablesOptimizer] Addressables settings not found!");
            return;
        }
        
        Debug.Log("[AddressablesOptimizer] ========== CURRENT SETTINGS ==========");
        Debug.Log($"[AddressablesOptimizer] Max Concurrent Web Requests: {settings.MaxConcurrentWebRequests}");
        Debug.Log($"[AddressablesOptimizer] Catalog Requests Timeout: {settings.CatalogRequestsTimeout}s");
        Debug.Log($"[AddressablesOptimizer] Bundle Timeout: {settings.BundleTimeout}s");
        Debug.Log($"[AddressablesOptimizer] Bundle Retry Count: {settings.BundleRetryCount}");
        Debug.Log($"[AddressablesOptimizer] Bundle Redirect Limit: {settings.BundleRedirectLimit}");
        Debug.Log($"[AddressablesOptimizer] Build Remote Catalog: {settings.BuildRemoteCatalog}");
        Debug.Log("[AddressablesOptimizer] =========================================");
        
        string message = 
            $"Max Concurrent Web Requests: {settings.MaxConcurrentWebRequests}\n" +
            $"Catalog Requests Timeout: {settings.CatalogRequestsTimeout}s\n" +
            $"Bundle Timeout: {settings.BundleTimeout}s\n" +
            $"Bundle Retry Count: {settings.BundleRetryCount}\n" +
            $"Bundle Redirect Limit: {settings.BundleRedirectLimit}\n" +
            $"Build Remote Catalog: {settings.BuildRemoteCatalog}";
        
        EditorUtility.DisplayDialog("Current Addressables Settings", message, "OK");
    }
}
