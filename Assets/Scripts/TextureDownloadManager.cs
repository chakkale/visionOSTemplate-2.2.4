using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Singleton manager for loading and caching addressable textures from remote sources
/// Supports download progress tracking and persistent caching
/// </summary>
public class TextureDownloadManager : MonoBehaviour
{
    public static TextureDownloadManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool verboseLogging = true;
    
    [Header("Download Progress")]
    [SerializeField] private float overallProgress = 0f;
    
    // Cache for loaded textures (in-memory)
    private Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();
    
    // Track active download operations
    private Dictionary<string, AsyncOperationHandle<Texture2D>> activeOperations = new Dictionary<string, AsyncOperationHandle<Texture2D>>();
    
    // Events for download progress
    public event Action<string, float> OnTextureDownloadProgress;
    public event Action<string, Texture2D> OnTextureLoaded;
    public event Action<string, string> OnTextureLoadFailed;
    public event Action<float> OnOverallProgressChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (enableDebugLogs)
                Debug.Log("[TextureDownloadManager] Initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Ensures TextureDownloadManager exists in the scene
    /// Call this before using the manager
    /// </summary>
    public static void EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("TextureDownloadManager");
            Instance = go.AddComponent<TextureDownloadManager>();
            DontDestroyOnLoad(go);
            Debug.Log("[TextureDownloadManager] Auto-created instance");
        }
    }
    
    private void OnDestroy()
    {
        // Release all active operations
        foreach (var operation in activeOperations.Values)
        {
            if (operation.IsValid())
            {
                Addressables.Release(operation);
            }
        }
        activeOperations.Clear();
        loadedTextures.Clear();
    }
    
    /// <summary>
    /// Check if a texture is already loaded in memory
    /// </summary>
    public bool IsTextureLoaded(string address)
    {
        return loadedTextures.ContainsKey(address);
    }
    
    /// <summary>
    /// Get a cached texture if available
    /// </summary>
    public Texture2D GetCachedTexture(string address)
    {
        if (loadedTextures.TryGetValue(address, out Texture2D texture))
        {
            return texture;
        }
        return null;
    }
    
    /// <summary>
    /// Get overall progress of texture loading (0-1)
    /// </summary>
    public float GetOverallProgress()
    {
        return overallProgress;
    }
    
    /// <summary>
    /// Load a texture asynchronously via Addressables
    /// Returns immediately if already cached
    /// </summary>
    public void LoadTextureAsync(string address, Action<Texture2D> onComplete, Action<string> onError = null)
    {
        if (verboseLogging)
            Debug.Log($"[TextureDownloadManager] LoadTextureAsync called with address: '{address}'");
        
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogWarning("[TextureDownloadManager] Cannot load texture with empty address");
            onError?.Invoke("Empty address");
            return;
        }
        
        // Return cached texture immediately
        if (loadedTextures.TryGetValue(address, out Texture2D cachedTexture))
        {
            if (enableDebugLogs)
                Debug.Log($"[TextureDownloadManager] Returning cached texture: {address}");
            onComplete?.Invoke(cachedTexture);
            return;
        }
        
        // Check if already downloading
        if (activeOperations.ContainsKey(address))
        {
            if (enableDebugLogs)
                Debug.Log($"[TextureDownloadManager] Texture already downloading: {address}");
            
            // Subscribe to existing operation
            var existingOp = activeOperations[address];
            existingOp.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    onComplete?.Invoke(op.Result);
                }
                else
                {
                    onError?.Invoke(op.OperationException?.Message ?? "Unknown error");
                }
            };
            return;
        }
        
        // Start new download
        Debug.Log($"[TextureDownloadManager] >>> Starting addressable load for: '{address}'");
        
        var handle = Addressables.LoadAssetAsync<Texture2D>(address);
        
        if (verboseLogging)
            Debug.Log($"[TextureDownloadManager] Handle created, IsValid: {handle.IsValid()}");
        activeOperations[address] = handle;
        
        handle.Completed += (op) =>
        {
            activeOperations.Remove(address);
            
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                Texture2D loadedTexture = op.Result;
                loadedTextures[address] = loadedTexture;
                
                if (enableDebugLogs)
                    Debug.Log($"[TextureDownloadManager] Successfully loaded: {address}");
                
                OnTextureLoaded?.Invoke(address, loadedTexture);
                onComplete?.Invoke(loadedTexture);
            }
            else
            {
                string error = op.OperationException?.Message ?? "Unknown error";
                Debug.LogError($"[TextureDownloadManager] Failed to load {address}: {error}");
                
                OnTextureLoadFailed?.Invoke(address, error);
                onError?.Invoke(error);
            }
        };
        
        // Track progress
        StartCoroutine(TrackDownloadProgress(address, handle));
    }
    
    private IEnumerator TrackDownloadProgress(string address, AsyncOperationHandle<Texture2D> handle)
    {
        while (!handle.IsDone)
        {
            float progress = handle.PercentComplete;
            OnTextureDownloadProgress?.Invoke(address, progress);
            
            if (enableDebugLogs && progress > 0)
                Debug.Log($"[TextureDownloadManager] {address} progress: {progress * 100:F1}%");
            
            yield return null;
        }
        
        // Final progress update
        OnTextureDownloadProgress?.Invoke(address, 1f);
    }
    
    /// <summary>
    /// Preload multiple textures for a room (day and night)
    /// </summary>
    public void PreloadRoomTextures(RoomData roomData, Action onComplete = null, Action<string> onError = null)
    {
        if (!roomData.UsesAddressables())
        {
            if (enableDebugLogs)
                Debug.Log($"[TextureDownloadManager] Room {roomData.roomName} doesn't use addressables, skipping preload");
            onComplete?.Invoke();
            return;
        }
        
        int texturesToLoad = 0;
        int texturesLoaded = 0;
        
        // Count textures to load
        if (!string.IsNullOrEmpty(roomData.dayTextureAddress))
            texturesToLoad++;
        if (!string.IsNullOrEmpty(roomData.nightTextureAddress))
            texturesToLoad++;
        
        if (texturesToLoad == 0)
        {
            onComplete?.Invoke();
            return;
        }
        
        void CheckComplete()
        {
            texturesLoaded++;
            if (texturesLoaded >= texturesToLoad)
            {
                if (enableDebugLogs)
                    Debug.Log($"[TextureDownloadManager] Preloaded all textures for room: {roomData.roomName}");
                onComplete?.Invoke();
            }
        }
        
        // Load day texture
        if (!string.IsNullOrEmpty(roomData.dayTextureAddress))
        {
            LoadTextureAsync(roomData.dayTextureAddress, 
                texture => CheckComplete(), 
                error => { onError?.Invoke(error); CheckComplete(); });
        }
        
        // Load night texture
        if (!string.IsNullOrEmpty(roomData.nightTextureAddress))
        {
            LoadTextureAsync(roomData.nightTextureAddress, 
                texture => CheckComplete(), 
                error => { onError?.Invoke(error); CheckComplete(); });
        }
    }
    
    /// <summary>
    /// Preload all textures for multiple rooms with overall progress tracking
    /// </summary>
    public void PreloadAllRoomTextures(RoomData[] rooms, Action onComplete = null, Action<string> onError = null)
    {
        StartCoroutine(PreloadAllRoomsCoroutine(rooms, onComplete, onError));
    }
    
    private IEnumerator PreloadAllRoomsCoroutine(RoomData[] rooms, Action onComplete, Action<string> onError)
    {
        int totalRooms = rooms.Length;
        int roomsProcessed = 0;
        
        if (enableDebugLogs)
            Debug.Log($"[TextureDownloadManager] Starting preload of {totalRooms} rooms");
        
        foreach (RoomData room in rooms)
        {
            bool roomComplete = false;
            
            PreloadRoomTextures(room, 
                () => roomComplete = true,
                (error) => { onError?.Invoke(error); roomComplete = true; });
            
            // Wait for room to complete
            yield return new WaitUntil(() => roomComplete);
            
            roomsProcessed++;
            overallProgress = (float)roomsProcessed / totalRooms;
            OnOverallProgressChanged?.Invoke(overallProgress);
            
            if (enableDebugLogs)
                Debug.Log($"[TextureDownloadManager] Overall progress: {overallProgress * 100:F1}% ({roomsProcessed}/{totalRooms})");
        }
        
        if (enableDebugLogs)
            Debug.Log("[TextureDownloadManager] All textures preloaded!");
        
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Get download size for all addressable content (requires Addressables catalog to be loaded)
    /// </summary>
    public void GetDownloadSize(Action<long> onComplete)
    {
        StartCoroutine(GetDownloadSizeCoroutine(onComplete));
    }
    
    private IEnumerator GetDownloadSizeCoroutine(Action<long> onComplete)
    {
        var sizeHandle = Addressables.GetDownloadSizeAsync("Remote_Textures");
        yield return sizeHandle;
        
        if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
        {
            long size = sizeHandle.Result;
            if (enableDebugLogs)
                Debug.Log($"[TextureDownloadManager] Download size: {FormatBytes(size)}");
            onComplete?.Invoke(size);
        }
        else
        {
            Debug.LogError($"[TextureDownloadManager] Failed to get download size: {sizeHandle.OperationException?.Message}");
            onComplete?.Invoke(0);
        }
        
        Addressables.Release(sizeHandle);
    }
    
    /// <summary>
    /// Clear the addressables cache (for testing purposes)
    /// </summary>
    public void ClearCache()
    {
        Caching.ClearCache();
        loadedTextures.Clear();
        
        if (enableDebugLogs)
            Debug.Log("[TextureDownloadManager] Cache cleared");
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
}
