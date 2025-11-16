using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Handles initial download of remote addressable content with progress display
/// Only downloads on first launch or when content needs updating
/// </summary>
public class InitialDownloadScene : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI downloadSizeText;
    [SerializeField] private UnityEngine.UI.Slider progressBar;
    [SerializeField] private GameObject downloadPanel;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TextMeshProUGUI errorText;
    
    [Header("Settings")]
    [SerializeField] private string mainSceneName = "MainFinal";
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool skipDownloadInEditor = false; // Skip download when testing in editor
    
    [Header("Room Data References")]
    [SerializeField] private RoomData[] allRooms; // Assign all RoomData assets in inspector
    
    private bool downloadFailed = false;
    
    private void Start()
    {
        if (errorPanel != null)
            errorPanel.SetActive(false);
            
        StartCoroutine(InitializeAndDownload());
    }
    
    private IEnumerator InitializeAndDownload()
    {
        UpdateStatus("Initializing...");
        
        // Skip download in editor if requested
        #if UNITY_EDITOR
        if (skipDownloadInEditor)
        {
            if (enableDebugLogs)
                Debug.Log("[InitialDownload] Skipping download in editor mode");
            
            UpdateStatus("Editor mode - skipping download");
            yield return new WaitForSeconds(1f);
            LoadMainScene();
            yield break;
        }
        
        Debug.LogWarning("[InitialDownload] Running addressables in EDITOR mode - this is for testing only!");
        Debug.LogWarning("[InitialDownload] For real device testing, build to Xcode and run on visionOS simulator/device");
        #endif
        
        // Initialize Addressables
        if (enableDebugLogs)
        {
            Debug.Log($"[InitialDownload] Starting Addressables initialization");
            #if UNITY_EDITOR
            Debug.Log($"[InitialDownload] Platform: UNITY_EDITOR");
            #elif UNITY_VISIONOS
            Debug.Log($"[InitialDownload] Platform: UNITY_VISIONOS");
            #else
            Debug.Log($"[InitialDownload] Platform: OTHER");
            #endif
            Debug.Log($"[InitialDownload] RuntimePath: {UnityEngine.AddressableAssets.Addressables.RuntimePath}");
            Debug.Log($"[InitialDownload] PersistentDataPath: {Application.persistentDataPath}");
            Debug.Log($"[InitialDownload] StreamingAssetsPath: {Application.streamingAssetsPath}");
            
            // Check if catalog files exist
            var runtimePath = UnityEngine.AddressableAssets.Addressables.RuntimePath;
            var catalogPath = System.IO.Path.Combine(runtimePath, "VisionOS", "catalog.bin");
            var hashPath = System.IO.Path.Combine(runtimePath, "VisionOS", "catalog.hash");
            var settingsPath = System.IO.Path.Combine(runtimePath, "VisionOS", "settings.json");
            
            Debug.Log($"[InitialDownload] Checking catalog.bin at: {catalogPath}");
            Debug.Log($"[InitialDownload] catalog.bin exists: {System.IO.File.Exists(catalogPath)}");
            Debug.Log($"[InitialDownload] catalog.hash exists: {System.IO.File.Exists(hashPath)}");
            Debug.Log($"[InitialDownload] settings.json exists: {System.IO.File.Exists(settingsPath)}");
            
            if (System.IO.Directory.Exists(runtimePath))
            {
                var files = System.IO.Directory.GetFiles(runtimePath, "*.*", System.IO.SearchOption.AllDirectories);
                Debug.Log($"[InitialDownload] Found {files.Length} files in RuntimePath");
                foreach (var file in files)
                {
                    Debug.Log($"[InitialDownload]   - {file}");
                }
            }
            else
            {
                Debug.LogError($"[InitialDownload] RuntimePath does not exist: {runtimePath}");
            }
        }
        
        if (enableDebugLogs)
            Debug.Log("[InitialDownload] Calling Addressables.InitializeAsync()...");
        
        bool initComplete = false;
        bool initSuccess = false;
        string initError = null;
        
        var initHandle = Addressables.InitializeAsync();
        initHandle.Completed += (op) =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                initSuccess = true;
                if (enableDebugLogs)
                    Debug.Log("[InitialDownload] Addressables initialized successfully");
            }
            else
            {
                initError = op.OperationException != null ? op.OperationException.ToString() : "Unknown error";
                Debug.LogError($"[InitialDownload] Addressables initialization failed: {initError}");
            }
            initComplete = true;
        };
        
        // Wait for completion
        while (!initComplete)
        {
            yield return null;
        }
        
        if (!initSuccess)
        {
            Debug.LogError($"[InitialDownload] Failed to initialize addressables: {initError}");
            ShowError("Failed to initialize content system.");
            yield break;
        }
        
        // Check download size
        UpdateStatus("Checking for updates...");
        
        long downloadSize = 0;
        
        var sizeHandle = Addressables.GetDownloadSizeAsync("remote");
        yield return sizeHandle;
        
        if (!sizeHandle.IsValid() || sizeHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[InitialDownload] Failed to check download size. Status: {(sizeHandle.IsValid() ? sizeHandle.Status.ToString() : "Invalid")}");
            if (sizeHandle.IsValid())
            {
                if (sizeHandle.OperationException != null)
                    Debug.LogError($"[InitialDownload] Exception: {sizeHandle.OperationException.Message}");
                Addressables.Release(sizeHandle);
            }
            ShowError("Failed to check download size");
            yield break;
        }
        
        downloadSize = sizeHandle.Result;
        if (sizeHandle.IsValid())
            Addressables.Release(sizeHandle);
        
        if (enableDebugLogs)
            Debug.Log($"[InitialDownload] Download size: {FormatBytes(downloadSize)}");
        
        // If nothing to download, proceed directly to main scene
        if (downloadSize == 0)
        {
            if (enableDebugLogs)
                Debug.Log("[InitialDownload] All content already cached");
            
            UpdateStatus("Content ready!");
            yield return new WaitForSeconds(0.5f);
            LoadMainScene();
            yield break;
        }
        
        // Show download size
        if (downloadSizeText != null)
            downloadSizeText.text = $"Download size: {FormatBytes(downloadSize)}";
        
        // Start download
        UpdateStatus("Downloading content...");
        
        if (enableDebugLogs)
            Debug.Log($"[InitialDownload] Starting download of {FormatBytes(downloadSize)}");
        
        var downloadHandle = Addressables.DownloadDependenciesAsync("remote");
        
        // Track progress
        while (!downloadHandle.IsDone)
        {
            float progress = downloadHandle.PercentComplete;
            
            if (progressBar != null)
                progressBar.value = progress;
                
            if (progressText != null)
                progressText.text = $"{(progress * 100):F0}%";
            
            yield return null;
        }
        
        if (!downloadHandle.IsValid() || downloadHandle.Status != AsyncOperationStatus.Succeeded)
        {
            string errorMsg = downloadHandle.OperationException?.Message ?? "Unknown error";
            Debug.LogError($"[InitialDownload] Download failed: {errorMsg}");
            ShowError($"Download failed: {errorMsg}");
            Addressables.Release(downloadHandle);
            yield break;
        }
        
        if (enableDebugLogs)
            Debug.Log("[InitialDownload] Download completed successfully");
            
        Addressables.Release(downloadHandle);
        
        // Download complete, proceed to main scene
        UpdateStatus("Content ready!");
        if (progressBar != null)
            progressBar.value = 1f;
        if (progressText != null)
            progressText.text = "100%";
        
        yield return new WaitForSeconds(1f);
        
        if (enableDebugLogs)
            Debug.Log("[InitialDownload] Loading main scene");
        
        LoadMainScene();
    }
    
    private IEnumerator TrackDownloadProgress(AsyncOperationHandle handle, System.Func<bool> isComplete)
    {
        while (!isComplete())
        {
            float progress = handle.PercentComplete;
            
            if (progressBar != null)
                progressBar.value = progress;
            
            if (progressText != null)
                progressText.text = $"{(progress * 100):F0}%";
            
            yield return null;
        }
    }
    
    private void UpdateStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
        
        if (enableDebugLogs)
            Debug.Log($"[InitialDownload] {status}");
    }
    
    private void ShowError(string error)
    {
        if (errorPanel != null)
            errorPanel.SetActive(true);
        
        if (errorText != null)
            errorText.text = error;
        
        if (downloadPanel != null)
            downloadPanel.SetActive(false);
        
        Debug.LogError($"[InitialDownload] {error}");
    }
    
    private void LoadMainScene()
    {
        SceneManager.LoadScene(mainSceneName);
    }
    
    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
    
    // Retry button handler
    public void OnRetryButtonClicked()
    {
        if (errorPanel != null)
            errorPanel.SetActive(false);
        
        if (downloadPanel != null)
            downloadPanel.SetActive(true);
        
        downloadFailed = false;
        
        StartCoroutine(InitializeAndDownload());
    }
}
