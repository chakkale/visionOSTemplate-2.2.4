#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// Editor utility to calculate app size savings with addressables
/// </summary>
public class AddressablesSizeCalculator : EditorWindow
{
    private long textureSize = 0;
    private int textureCount = 0;
    
    [MenuItem("Tools/Addressables/Calculate Size Savings")]
    public static void ShowWindow()
    {
        var window = GetWindow<AddressablesSizeCalculator>("Size Savings Calculator");
        window.minSize = new Vector2(450, 400);
        window.CalculateSizes();
    }
    
    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Addressables Size Savings Calculator", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This tool calculates how much app size you'll save by using remote addressables for panoramic textures.", 
            MessageType.Info);
        
        GUILayout.Space(10);
        
        // Texture size section
        EditorGUILayout.LabelField("Panoramic Textures Analysis", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.LabelField($"Total Textures Found: {textureCount}");
        EditorGUILayout.LabelField($"Total Texture Size: {FormatBytes(textureSize)}");
        
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Savings breakdown
        EditorGUILayout.LabelField("Size Comparison", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        DrawSizeComparison("Without Addressables (All bundled)", textureSize, Color.red);
        DrawSizeComparison("With Addressables (Remote)", 0, Color.green);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Savings: {FormatBytes(textureSize)}", EditorStyles.boldLabel);
        
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Apple limits
        EditorGUILayout.LabelField("Apple Size Limits", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        long appleLimitBytes = 4L * 1024 * 1024 * 1024; // 4GB
        long estimatedAppSize = 200L * 1024 * 1024; // ~200MB estimated without textures
        
        EditorGUILayout.LabelField($"Apple's 4GB limit: {FormatBytes(appleLimitBytes)}");
        EditorGUILayout.LabelField($"Estimated app size (no textures): {FormatBytes(estimatedAppSize)}");
        EditorGUILayout.LabelField($"App + Textures (bundled): {FormatBytes(estimatedAppSize + textureSize)}");
        
        bool exceedsLimit = (estimatedAppSize + textureSize) > appleLimitBytes;
        
        if (exceedsLimit)
        {
            EditorGUILayout.HelpBox(
                "⚠️ App would EXCEED Apple's 4GB limit without addressables!", 
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "✓ App is under Apple's 4GB limit", 
                MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Detailed breakdown
        if (GUILayout.Button("Show Detailed Breakdown"))
        {
            ShowDetailedBreakdown();
        }
        
        if (GUILayout.Button("Refresh Calculations"))
        {
            CalculateSizes();
        }
        
        GUILayout.Space(10);
        
        // Download time estimate
        EditorGUILayout.LabelField("User Download Estimates", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        DrawDownloadEstimate("Fast WiFi (50 Mbps)", textureSize, 50);
        DrawDownloadEstimate("Average WiFi (25 Mbps)", textureSize, 25);
        DrawDownloadEstimate("Slow WiFi (10 Mbps)", textureSize, 10);
        DrawDownloadEstimate("Mobile 5G (20 Mbps)", textureSize, 20);
        
        EditorGUILayout.HelpBox(
            "Note: These are estimates. Actual download time depends on server speed, network congestion, and device performance.", 
            MessageType.Info);
        
        EditorGUILayout.EndVertical();
    }
    
    private void CalculateSizes()
    {
        string searchPath = "Assets/Wizio/Renders";
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { searchPath });
        
        textureSize = 0;
        textureCount = guids.Length;
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            FileInfo fileInfo = new FileInfo(assetPath);
            if (fileInfo.Exists)
            {
                textureSize += fileInfo.Length;
            }
        }
        
        Repaint();
    }
    
    private void DrawSizeComparison(string label, long size, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        
        var oldColor = GUI.color;
        GUI.color = color;
        GUILayout.Label("■", GUILayout.Width(20));
        GUI.color = oldColor;
        
        EditorGUILayout.LabelField($"{label}: {FormatBytes(size)}");
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawDownloadEstimate(string label, long bytes, float mbps)
    {
        // Convert bytes to megabits
        float megabits = (bytes * 8f) / (1024f * 1024f);
        
        // Calculate time in seconds
        float seconds = megabits / mbps;
        
        // Format time
        string timeStr;
        if (seconds < 60)
        {
            timeStr = $"{seconds:F0} seconds";
        }
        else if (seconds < 3600)
        {
            timeStr = $"{(seconds / 60):F1} minutes";
        }
        else
        {
            timeStr = $"{(seconds / 3600):F1} hours";
        }
        
        EditorGUILayout.LabelField($"{label}: ~{timeStr}");
    }
    
    private void ShowDetailedBreakdown()
    {
        string searchPath = "Assets/Wizio/Renders";
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { searchPath });
        
        // Group by size ranges
        int under10MB = 0;
        int under50MB = 0;
        int under100MB = 0;
        int under500MB = 0;
        int over500MB = 0;
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            FileInfo fileInfo = new FileInfo(assetPath);
            
            if (!fileInfo.Exists) continue;
            
            long sizeMB = fileInfo.Length / (1024 * 1024);
            
            if (sizeMB < 10) under10MB++;
            else if (sizeMB < 50) under50MB++;
            else if (sizeMB < 100) under100MB++;
            else if (sizeMB < 500) under500MB++;
            else over500MB++;
        }
        
        string breakdown = "Texture Size Distribution:\n\n";
        breakdown += $"Under 10 MB:   {under10MB} textures\n";
        breakdown += $"10-50 MB:      {under50MB} textures\n";
        breakdown += $"50-100 MB:     {under100MB} textures\n";
        breakdown += $"100-500 MB:    {under500MB} textures\n";
        breakdown += $"Over 500 MB:   {over500MB} textures\n";
        breakdown += $"\nTotal: {textureCount} textures\n";
        breakdown += $"Total Size: {FormatBytes(textureSize)}";
        
        EditorUtility.DisplayDialog("Detailed Breakdown", breakdown, "OK");
    }
    
    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
#endif
