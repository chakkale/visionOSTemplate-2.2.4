using UnityEngine;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Manages night mode functionality for rooms, working in conjunction with the XRUIFlowManager's dark/light toggle
/// </summary>
public class NightModeManager : MonoBehaviour
{
    public static NightModeManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Night Mode State")]
    [SerializeField] private bool isNightMode = false; // Default to day mode
    
    [Header("Crossfade Settings")]
    [SerializeField] private float crossfadeDuration = 1.0f;
    [SerializeField] private Ease crossfadeEase = Ease.InOutSine;
    
    // Events
    public System.Action<bool> OnNightModeChanged;
    
    private XRUIFlowManager uiFlowManager;
    private RoomManager roomManager;
    private bool isTransitioning = false;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        Debug.Log("=== NIGHT MODE MANAGER START CALLED ===");
        
        // Find required components
        uiFlowManager = FindFirstObjectByType<XRUIFlowManager>();
        roomManager = FindFirstObjectByType<RoomManager>();
        
        Debug.Log($"Found XRUIFlowManager: {uiFlowManager != null}");
        Debug.Log($"Found RoomManager: {roomManager != null}");
        
        if (uiFlowManager == null)
        {
            Debug.LogError("[NightModeManager] XRUIFlowManager not found!");
        }
        else if (enableDebugLogs)
        {
            Debug.Log($"[NightModeManager] Found XRUIFlowManager: {uiFlowManager.name}");
        }
        
        if (roomManager == null)
        {
            Debug.LogError("[NightModeManager] RoomManager not found!");
        }
        else if (enableDebugLogs)
        {
            Debug.Log($"[NightModeManager] Found RoomManager: {roomManager.name}");
        }
        
        Debug.Log($"[NightModeManager] Initialized in {(isNightMode ? "Night" : "Day")} mode");
        
        // Ensure we start in day mode and force room textures to day mode
        if (isNightMode)
        {
            Debug.Log("[NightModeManager] Correcting to day mode as default");
            isNightMode = false;
        }
        
        // Ensure we update the current room texture based on initial state
        if (roomManager != null)
        {
            Debug.Log("Calling UpdateCurrentRoomTexture from Start");
            UpdateCurrentRoomTexture();
        }
    }
    
    /// <summary>
    /// Sets the night mode state and updates the current room if needed
    /// </summary>
    /// <param name="nightMode">True for night mode, false for day mode</param>
    public void SetNightMode(bool nightMode)
    {
        Debug.Log($"=== SETNIGHT MODE CALLED: {nightMode} (current: {isNightMode}) ===");
        
        if (isNightMode == nightMode) 
        {
            Debug.Log("No change needed - same mode");
            return; // No change needed
        }
        
        isNightMode = nightMode;
        
        Debug.Log($"Night mode changed to: {isNightMode}");
        
        if (enableDebugLogs)
        {
            Debug.Log($"[NightModeManager] Night mode {(isNightMode ? "ENABLED" : "DISABLED")}");
        }
        
        // Invoke event
        OnNightModeChanged?.Invoke(isNightMode);
        
        // Update current room texture if a room is active
        UpdateCurrentRoomTexture();
    }
    
    /// <summary>
    /// Gets the current night mode state
    /// </summary>
    /// <returns>True if in night mode, false if in day mode</returns>
    public bool IsNightMode()
    {
        return isNightMode;
    }
    
    /// <summary>
    /// Updates the current room's texture based on the night mode state with crossfade effect
    /// </summary>
    public void UpdateCurrentRoomTexture()
    {
        Debug.Log($"=== UPDATE CURRENT ROOM TEXTURE CALLED - Night mode: {isNightMode} ===");
        
        if (enableDebugLogs)
        {
            Debug.Log($"[NightModeManager] UpdateCurrentRoomTexture called - Night mode: {isNightMode}");
        }
        
        if (roomManager == null)
        {
            Debug.LogWarning("[NightModeManager] RoomManager is null!");
            return;
        }
        
        RoomData currentRoom = roomManager.CurrentRoomData;
        if (currentRoom == null)
        {
            Debug.LogWarning("[NightModeManager] No current room data!");
            return;
        }
        
        if (isTransitioning)
        {
            Debug.Log("[NightModeManager] Already transitioning, ignoring request");
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[NightModeManager] Current room: {currentRoom.roomName}");
            Debug.Log($"[NightModeManager] Has night mode: {currentRoom.HasNightMode()}");
            Debug.Log($"[NightModeManager] Day texture: {(currentRoom.GetDayTexture() != null ? currentRoom.GetDayTexture().name : "null")}");
            Debug.Log($"[NightModeManager] Night texture: {(currentRoom.nightTexture != null ? currentRoom.nightTexture.name : "null")}");
        }
        
        // Check if using addressables
        if (currentRoom.UsesAddressables() && TextureDownloadManager.Instance != null)
        {
            string textureAddress = currentRoom.GetTextureAddressForMode(isNightMode);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[NightModeManager] Loading texture from addressables: {textureAddress}");
            }
            
            // Load texture asynchronously
            TextureDownloadManager.Instance.LoadTextureAsync(textureAddress,
                (loadedTexture) => {
                    if (loadedTexture != null)
                    {
                        if (enableDebugLogs)
                        {
                            string modeName = isNightMode ? "night" : "day";
                            Debug.Log($"[NightModeManager] Starting crossfade to {modeName} mode with texture: {loadedTexture.name}");
                        }
                        StartCoroutine(CrossfadeToTexture(loadedTexture));
                    }
                    else
                    {
                        Debug.LogWarning($"[NightModeManager] Loaded texture is null for address: {textureAddress}");
                    }
                },
                (error) => {
                    Debug.LogError($"[NightModeManager] Failed to load texture: {error}");
                }
            );
        }
        else
        {
            // Legacy mode: Get the target texture for the new mode
            Texture2D targetTexture = currentRoom.GetTextureForMode(isNightMode);
            if (targetTexture == null)
            {
                Debug.LogWarning($"[NightModeManager] No {(isNightMode ? "night" : "day")} texture available for room '{currentRoom.roomName}'");
                return;
            }
            
            if (enableDebugLogs)
            {
                string modeName = isNightMode ? "night" : "day";
                Debug.Log($"[NightModeManager] Starting crossfade to {modeName} mode with texture: {targetTexture.name}");
            }
            
            // Start the crossfade coroutine
            StartCoroutine(CrossfadeToTexture(targetTexture));
        }
    }
    
    /// <summary>
    /// Crossfades to a new texture using DOTween for smooth transition
    /// This creates a true crossfade by using two sphere renderers simultaneously
    /// </summary>
    /// <param name="targetTexture">The texture to fade to</param>
    private System.Collections.IEnumerator CrossfadeToTexture(Texture2D targetTexture)
    {
        isTransitioning = true;
        
        // Get the current sphere renderer from RoomManager
        var currentSphereRenderer = GetCurrentSphereRenderer();
        if (currentSphereRenderer == null)
        {
            Debug.LogWarning("[NightModeManager] No current sphere renderer found!");
            isTransitioning = false;
            yield break;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[NightModeManager] Starting true crossfade to texture: {targetTexture.name}");
            Debug.Log($"[NightModeManager] Current sphere renderer: {currentSphereRenderer.name}");
        }
        
        // Store original values
        Material originalMaterial = currentSphereRenderer.material;
        Texture2D originalTexture = originalMaterial.GetTexture("_MainTex") as Texture2D;
        float originalOpacity = originalMaterial.GetFloat("_Opacity");
        
        // Create a duplicate sphere for crossfading
        GameObject currentRoomPrefab = currentSphereRenderer.transform.root.gameObject;
        GameObject tempRoomPrefab = Object.Instantiate(currentRoomPrefab, currentRoomPrefab.transform.parent);
        tempRoomPrefab.name = currentRoomPrefab.name + "_TempCrossfade";
        
        // Get the temp sphere renderer
        MeshRenderer tempSphereRenderer = null;
        foreach (var mr in tempRoomPrefab.GetComponentsInChildren<MeshRenderer>())
        {
            if (mr.material.HasProperty("_Opacity"))
            {
                tempSphereRenderer = mr;
                break;
            }
        }
        
        if (tempSphereRenderer == null)
        {
            Debug.LogWarning("[NightModeManager] Could not find temp sphere renderer!");
            Object.Destroy(tempRoomPrefab);
            isTransitioning = false;
            yield break;
        }
        
        // Setup temp renderer with target texture
        tempSphereRenderer.material.SetTexture("_MainTex", targetTexture);
        tempSphereRenderer.material.SetFloat("_Opacity", 0f);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[NightModeManager] True crossfade: {originalTexture?.name} -> {targetTexture.name}");
        }
        
        // Create simultaneous fade animations
        Sequence crossfadeSequence = DOTween.Sequence();
        
        // Fade OUT original texture
        crossfadeSequence.Join(
            DOTween.To(
                () => originalMaterial.GetFloat("_Opacity"),
                x => originalMaterial.SetFloat("_Opacity", x),
                0f, crossfadeDuration
            ).SetEase(crossfadeEase)
        );
        
        // Fade IN new texture (simultaneously)
        crossfadeSequence.Join(
            DOTween.To(
                () => tempSphereRenderer.material.GetFloat("_Opacity"),
                x => tempSphereRenderer.material.SetFloat("_Opacity", x),
                originalOpacity, crossfadeDuration
            ).SetEase(crossfadeEase)
        );
        
        // Wait for crossfade to complete
        yield return crossfadeSequence.WaitForCompletion();
        
        // Switch the original renderer to the new texture and restore opacity
        originalMaterial.SetTexture("_MainTex", targetTexture);
        originalMaterial.SetFloat("_Opacity", originalOpacity);
        
        // Clean up temporary objects
        Object.Destroy(tempRoomPrefab);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[NightModeManager] True crossfade complete! Now showing: {targetTexture.name}");
        }
        
        isTransitioning = false;
    }
    
    /// <summary>
    /// Gets the current sphere renderer from RoomManager
    /// </summary>
    /// <returns>The current sphere renderer, or null if not found</returns>
    private MeshRenderer GetCurrentSphereRenderer()
    {
        if (roomManager == null)
            return null;
            
        // Access RoomManager's currentSphereRenderer through reflection since it's private
        var currentSphereRendererField = typeof(RoomManager).GetField("currentSphereRenderer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (currentSphereRendererField != null)
        {
            return currentSphereRendererField.GetValue(roomManager) as MeshRenderer;
        }
        
        Debug.LogWarning("[NightModeManager] Could not access RoomManager's currentSphereRenderer field");
        return null;
    }

    /// <summary>
    /// Public method to toggle night mode (can be called from UI or other scripts)
    /// </summary>
    public void ToggleNightMode()
    {
        SetNightMode(!isNightMode);
    }
    
    /// <summary>
    /// Called by XRUIFlowManager when dark/light mode is toggled
    /// </summary>
    /// <param name="isLightMode">True if UI is in light mode, false if in dark mode</param>
    public void OnUILightModeChanged(bool isLightMode)
    {
        // Set night mode to the opposite of light mode
        SetNightMode(!isLightMode);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[NightModeManager] UI mode changed to {(isLightMode ? "Light" : "Dark")}, setting night mode to {(!isLightMode ? "ON" : "OFF")}");
        }
    }
    
    /// <summary>
    /// Context menu method for testing
    /// </summary>
    [ContextMenu("Toggle Night Mode")]
    public void TestToggleNightMode()
    {
        ToggleNightMode();
    }
    
    /// <summary>
    /// Context menu method for forcing day mode
    /// </summary>
    [ContextMenu("Force Day Mode")]
    public void ForceDayMode()
    {
        SetNightMode(false);
    }
    
    /// <summary>
    /// Context menu method for forcing night mode
    /// </summary>
    [ContextMenu("Force Night Mode")]
    public void ForceNightMode()
    {
        SetNightMode(true);
    }
    
    private void OnDestroy()
    {
        // Clean up events
        OnNightModeChanged = null;
    }
}
