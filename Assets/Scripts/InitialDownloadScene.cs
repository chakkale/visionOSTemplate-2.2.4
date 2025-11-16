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
        #endif
        
        // Initialize Addressables
        var initHandle = Addressables.InitializeAsync();
        yield return initHandle;
        
        if (initHandle.Status != AsyncOperationStatus.Succeeded)
        {
            ShowError("Failed to initialize content system");
            yield break;
        }
        
        if (enableDebugLogs)
            Debug.Log("[InitialDownload] Addressables initialized");
        
        // Check download size
        UpdateStatus("Checking for updates...");
        
        long downloadSize = 0;
        bool sizeCheckComplete = false;
        
        var sizeHandle = Addressables.GetDownloadSizeAsync("Remote_Textures");
        sizeHandle.Completed += (op) => {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                downloadSize = op.Result;
            }
            sizeCheckComplete = true;
        };
        
        yield return new WaitUntil(() => sizeCheckComplete);
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
        
        bool downloadComplete = false;
        
        var downloadHandle = Addressables.DownloadDependenciesAsync("Remote_Textures");
        
        // Track progress
        StartCoroutine(TrackDownloadProgress(downloadHandle, () => downloadComplete));
        
        downloadHandle.Completed += (op) => {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                if (enableDebugLogs)
                    Debug.Log("[InitialDownload] Download completed successfully");
                downloadComplete = true;
            }
            else
            {
                Debug.LogError($"[InitialDownload] Download failed: {op.OperationException?.Message}");
                ShowError($"Download failed: {op.OperationException?.Message ?? "Unknown error"}");
                downloadFailed = true;
            }
        };
        
        yield return new WaitUntil(() => downloadComplete || downloadFailed);
        Addressables.Release(downloadHandle);
        
        if (downloadFailed)
        {
            yield break;
        }
        
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
