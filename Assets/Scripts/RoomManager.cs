using System.Collections;
using UnityEngine;
using DG.Tweening;
using System.Linq;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    public float fadeDuration = 1.0f;
    public Ease fadeEase = Ease.InOutSine;
    public Transform roomParent; // Parent for spawned room prefabs
    public Material skyFullMaterial; // Assign in Inspector - fixes addressables material loading issue

    private GameObject currentRoomPrefab;
    private MeshRenderer currentSphereRenderer;
    private Material currentSphereMaterial; // Cache material instance to avoid creating new ones
    private GameObject nextRoomPrefab;
    private MeshRenderer nextSphereRenderer;
    private bool isFading = false;
    private RoomData currentRoomData; // Track the current room data
    
    public RoomData CurrentRoomData => currentRoomData; // Public property to access current room data

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Disable stack traces for cleaner logs (visionOS/Xcode)
        #if UNITY_VISIONOS || DEVELOPMENT_BUILD
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        #endif
    }

    public void TeleportToRoom(RoomData roomData)
    {
        Debug.Log("[RoomManager] TeleportToRoom called for: " + (roomData != null ? roomData.roomName : "null") + " | isFading: " + isFading);
        if (isFading || roomData == null || roomData.roomPrefab == null) return;
        StartCoroutine(CrossfadeToRoom(roomData));
    }

    private IEnumerator CrossfadeToRoom(RoomData roomData)
    {
        Debug.Log("[RoomManager] CrossfadeToRoom started for: " + (roomData != null ? roomData.roomName : "null"));
        Debug.Log($"[RoomManager] RoomData details - UsesAddressables: {roomData.UsesAddressables()}, dayAddress: '{roomData.dayTextureAddress}', nightAddress: '{roomData.nightTextureAddress}'");
        isFading = true;

        // Instantiate the new room prefab as a child of roomParent
        nextRoomPrefab = Instantiate(roomData.roomPrefab, roomParent);
        nextRoomPrefab.SetActive(true);
        
        // DEBUG: Check instantiation position and scale
        Debug.Log($"[RoomManager] Instantiated {roomData.roomName} - local position: {nextRoomPrefab.transform.localPosition}, local scale: {nextRoomPrefab.transform.localScale}");
        Debug.Log($"[RoomManager] Parent (roomParent): {roomParent.name}, parent position: {roomParent.position}");
        
        nextSphereRenderer = FindSphereRenderer(nextRoomPrefab);
        if (nextSphereRenderer == null)
        {
            Debug.LogError("RoomManager: No MeshRenderer with correct shader found in new room prefab!");
            Destroy(nextRoomPrefab);
            isFading = false;
            yield break;
        }
        
        // CRITICAL FIX: Assign material manually FIRST because addressables don't load material references properly
        if (skyFullMaterial != null && nextSphereRenderer != null)
        {
            nextSphereRenderer.sharedMaterial = skyFullMaterial;
            Debug.Log("[RoomManager] Manually assigned SkyFull material to fix addressables loading issue");
        }
        else if (skyFullMaterial == null)
        {
            Debug.LogError("[RoomManager] skyFullMaterial is not assigned in Inspector! Please assign it.");
        }
        
        // Load texture asynchronously if using addressables, otherwise use direct reference
        bool textureLoaded = false;
        Texture2D textureToUse = null;
        
        if (roomData.UsesAddressables())
        {
            // Ensure TextureDownloadManager exists
            TextureDownloadManager.EnsureInstance();
            
            // Check if NightModeManager exists and get night mode state
            bool isNightMode = false;
            var nightModeManager = FindFirstObjectByType<NightModeManager>();
            if (nightModeManager != null)
            {
                isNightMode = nightModeManager.IsNightMode();
            }
            
            string textureAddress = roomData.GetTextureAddressForMode(isNightMode);
            Debug.Log($"[RoomManager] Using addressables, texture address: {textureAddress}");
            Debug.Log($"[RoomManager] Night mode: {isNightMode}, TextureDownloadManager exists: {TextureDownloadManager.Instance != null}");
            
            // Try to get from cache first
            if (TextureDownloadManager.Instance != null)
            {
                textureToUse = TextureDownloadManager.Instance.GetCachedTexture(textureAddress);
                
                if (textureToUse == null)
                {
                    // Load asynchronously
                    Debug.Log($"[RoomManager] Loading texture from addressables: {textureAddress}");
                    Debug.Log($"[RoomManager] Starting LoadTextureAsync call...");
                    
                    TextureDownloadManager.Instance.LoadTextureAsync(textureAddress,
                        (loadedTexture) => {
                            textureToUse = loadedTexture;
                            textureLoaded = true;
                            Debug.Log($"[RoomManager] ✓✓✓ Texture loaded successfully: {textureAddress}, texture is null: {loadedTexture == null}");
                            if (loadedTexture != null)
                            {
                                Debug.Log($"[RoomManager] Texture details - name: {loadedTexture.name}, size: {loadedTexture.width}x{loadedTexture.height}");
                            }
                        },
                        (error) => {
                            Debug.LogError($"[RoomManager] ✗✗✗ Failed to load texture: {error}");
                            Debug.LogError($"[RoomManager] Failed address was: '{textureAddress}'");
                            // Try direct reference as last resort
                            textureToUse = GetTextureForCurrentMode(roomData);
                            if (textureToUse != null)
                            {
                                Debug.LogWarning($"[RoomManager] Using fallback direct reference for {textureAddress}");
                            }
                            textureLoaded = true;
                        }
                    );
                    
                    Debug.Log($"[RoomManager] Waiting for texture to load...");
                    // Wait for texture to load
                    yield return new WaitUntil(() => textureLoaded);
                    Debug.Log($"[RoomManager] Wait complete, textureToUse is null: {textureToUse == null}");
                }
                else
                {
                    Debug.Log($"[RoomManager] Using cached texture: {textureAddress}");
                }
            }
            else
            {
                Debug.LogError("[RoomManager] TextureDownloadManager not found! Cannot load addressable textures.");
                Debug.LogError("[RoomManager] Make sure TextureDownloadManager is in the scene.");
                // Try direct reference as fallback
                textureToUse = GetTextureForCurrentMode(roomData);
                if (textureToUse != null)
                {
                    Debug.LogWarning($"[RoomManager] Using fallback direct reference");
                }
            }
        }
        else
        {
            // Use direct reference (legacy mode)
            Debug.Log("[RoomManager] Using legacy direct texture reference mode");
            textureToUse = GetTextureForCurrentMode(roomData);
        }
        
        // Set the texture on the material
        Debug.Log($"[RoomManager] About to set texture on material, textureToUse is null: {textureToUse == null}");
        
        // CRITICAL: Create a material INSTANCE so each room has its own opacity value
        // If we use sharedMaterial, all rooms share the same material and opacity!
        Material nextMaterial = nextSphereRenderer.material; // This creates an instance automatically
        
        if (nextMaterial == null)
        {
            Debug.LogError("[RoomManager] Failed to create material instance! Cannot set texture.");
            isFading = false;
            yield break;
        }
        
        Debug.Log($"[RoomManager] Using material: {nextMaterial.name}");
        
        if (textureToUse != null)
        {
            nextMaterial.SetTexture("_MainTex", textureToUse);
            Debug.Log($"[RoomManager] ✓ Set texture on material: {textureToUse.name}");
            Debug.Log($"[RoomManager] Material now has texture: {nextMaterial.GetTexture("_MainTex") != null}");
        }
        else
        {
            Debug.LogError($"[RoomManager] ✗✗✗ NO TEXTURE AVAILABLE for room: {roomData.roomName}");
            Debug.LogError($"[RoomManager] This is why the room appears black/unchanged!");
        }
        
        // Set initial opacity to 0
        nextMaterial.SetFloat("_Opacity", 0f);
        Debug.Log($"[RoomManager] Set initial opacity to 0 for new room");

        // Prepare current sphere renderer
        if (currentRoomPrefab != null && currentSphereRenderer == null)
        {
            currentSphereRenderer = FindSphereRenderer(currentRoomPrefab);
        }

        // --- Button Scale Animation ---
        // Set all new buttons to scale zero
        SetButtonsScale(nextRoomPrefab, Vector3.zero);
        Debug.Log("[RoomManager] SetButtonsScale to zero for new buttons.");
        // Animate old buttons to scale down
        AnimateButtonsScale(currentRoomPrefab, false, fadeDuration, fadeEase);
        // Animate new buttons to scale up
        AnimateButtonsScale(nextRoomPrefab, true, fadeDuration, fadeEase);
        // --- End Button Scale Animation ---

        // Crossfade using DOTween.To for float _Opacity
        Material nextMat = nextMaterial; // Already validated above
        Debug.Log($"[RoomManager] Starting crossfade animation - nextMat opacity before: {nextMat.GetFloat("_Opacity")}");
        Sequence fadeSeq = DOTween.Sequence();
        fadeSeq.Join(
            DOTween.To(
                () => nextMat.GetFloat("_Opacity"),
                x => {
                    nextMat.SetFloat("_Opacity", x);
                    Debug.Log($"[RoomManager] New room opacity: {x:F2}");
                },
                1f, fadeDuration
            ).SetEase(fadeEase)
        );
        if (currentSphereRenderer != null)
        {
            Debug.Log($"[RoomManager] Current room exists, will fade it out from opacity: {currentSphereRenderer.material.GetFloat("_Opacity")}");
            // Create material instance for current room too
            Material currentMat = currentSphereRenderer.material; // Instance, not shared
            
            if (currentMat != null)
            {
                Debug.Log($"[RoomManager] Adding fade OUT animation for old room material: {currentMat.name}");
                fadeSeq.Join(
                    DOTween.To(
                        () => currentMat.GetFloat("_Opacity"),
                        x => {
                            currentMat.SetFloat("_Opacity", x);
                            Debug.Log($"[RoomManager] Old room opacity: {x:F2}");
                        },
                        0f, fadeDuration
                    ).SetEase(fadeEase)
                );
            }
        }
        else
        {
            Debug.Log($"[RoomManager] No current room to fade out (first room load)");
        }
        fadeSeq.Play();
        Debug.Log($"[RoomManager] Crossfade animation started, waiting for completion...");
        yield return fadeSeq.WaitForCompletion();
        Debug.Log($"[RoomManager] Crossfade complete! Final opacity: {nextMat.GetFloat("_Opacity")}");

        // DEBUG: Verify final state
        Debug.Log($"[RoomManager] Post-crossfade verification:");
        Debug.Log($"  - New room active: {nextRoomPrefab.activeSelf}");
        Debug.Log($"  - New renderer enabled: {nextSphereRenderer.enabled}");
        Debug.Log($"  - New material: {nextMat.name}");
        Debug.Log($"  - New material shader: {nextMat.shader.name}");
        Debug.Log($"  - New material has texture: {nextMat.mainTexture != null}");
        if (nextMat.mainTexture != null)
        {
            Debug.Log($"  - Texture name: {nextMat.mainTexture.name}, size: {nextMat.mainTexture.width}x{nextMat.mainTexture.height}");
        }
        Debug.Log($"  - Final _Opacity value: {nextMat.GetFloat("_Opacity")}");

        // Cleanup old prefab
        if (currentRoomPrefab != null)
        {
            Debug.Log($"[RoomManager] Destroying old room: {currentRoomPrefab.name}");
            Debug.Log($"[RoomManager] Old room instance ID: {currentRoomPrefab.GetInstanceID()}");
            Debug.Log($"[RoomManager] Old renderer enabled before destroy: {currentSphereRenderer.enabled}");
            Debug.Log($"[RoomManager] Old material opacity before destroy: {currentSphereMaterial.GetFloat("_Opacity")}");
            
            // CRITICAL: Explicitly disable old renderer and set opacity to 0 BEFORE destroying
            // This ensures PolySpatial syncs the hidden state before the GameObject is removed
            currentSphereRenderer.enabled = false;
            currentSphereMaterial.SetFloat("_Opacity", 0f);
            Debug.Log($"[RoomManager] ⚠️ Disabled old room renderer and set opacity to 0");
            
            // Force a frame delay to let PolySpatial sync before destroying
            yield return null;
            
            Destroy(currentRoomPrefab);
            Debug.Log($"[RoomManager] Destroy() called on old room");
        }
        
        currentRoomPrefab = nextRoomPrefab;
        currentSphereRenderer = nextSphereRenderer;
        currentSphereMaterial = nextMat; // Cache the material instance
        
        Debug.Log($"[RoomManager] ✓ Updated current references - new room is now: {currentRoomPrefab.name}");
        Debug.Log($"[RoomManager] New room instance ID: {currentRoomPrefab.GetInstanceID()}");
        Debug.Log($"[RoomManager] Current renderer enabled: {currentSphereRenderer.enabled}");
        Debug.Log($"[RoomManager] Current material opacity: {currentSphereMaterial.GetFloat("_Opacity")}");
        
        nextRoomPrefab = null;
        nextSphereRenderer = null;
        
        // Store the current room data
        currentRoomData = roomData;
        Debug.Log($"[RoomManager] Successfully switched to room: {roomData.roomName}");
        
        // FORCE PolySpatial to re-sync with multiple strategies
        Debug.Log($"[RoomManager] 🔄 Forcing PolySpatial sync - applying workarounds");
        
        // Strategy 1: Toggle renderer
        currentSphereRenderer.enabled = false;
        yield return null;
        currentSphereRenderer.enabled = true;
        
        // Strategy 2: Force material property change to trigger PolySpatial sync
        // Set a dummy float property that doesn't affect rendering but forces update
        currentSphereMaterial.SetFloat("_DummySyncProperty", Time.time);
        
        // Strategy 3: Re-apply opacity to ensure RealityKit gets the update
        currentSphereMaterial.SetFloat("_Opacity", 1.0f);
        
        // Strategy 4: Re-apply texture to ensure RealityKit gets it
        Texture currentTexture = currentSphereMaterial.GetTexture("_MainTex");
        if (currentTexture != null)
        {
            currentSphereMaterial.SetTexture("_MainTex", null);
            yield return null;
            currentSphereMaterial.SetTexture("_MainTex", currentTexture);
            Debug.Log($"[RoomManager] 🔄 Texture re-applied to force PolySpatial sync");
        }
        
        // Strategy 5: Force transform update to trigger PolySpatial entity re-registration
        Vector3 originalPosition = currentRoomPrefab.transform.localPosition;
        currentRoomPrefab.transform.localPosition = originalPosition + new Vector3(0.001f, 0, 0);
        yield return null;
        currentRoomPrefab.transform.localPosition = originalPosition;
        Debug.Log($"[RoomManager] 🔄 Transform updated to force PolySpatial entity refresh");
        
        Debug.Log($"[RoomManager] 🔄 All PolySpatial sync workarounds applied");
        
        // Notify UI to update room info
        NotifyRoomChanged();
        
        isFading = false;

        // Update HDRISkyController's skyboxSphere reference if present
        HDRISkyController skyController = Object.FindFirstObjectByType<HDRISkyController>();
        if (skyController != null)
        {
            Transform sphereTransform = FindSphereTransform(currentRoomPrefab);
            if (sphereTransform != null)
            {
                skyController.SetSkyboxSphere(sphereTransform);
                Debug.Log($"[RoomManager] Updated HDRISkyController reference to: {sphereTransform.name}");
            }
        }
        
        // Verify state after short delay to catch PolySpatial sync issues
        StartCoroutine(VerifyRoomStateAfterDelay(1.0f));
    }

    // Helper to set all button scales in a prefab
    private void SetButtonsScale(GameObject roomPrefab, Vector3 scale)
    {
        if (roomPrefab == null) return;
        foreach (var btn in roomPrefab.GetComponentsInChildren<RoomTeleportButton>(true))
        {
            var pulser = btn.GetComponent<ScalePulser>();
            if (pulser != null && scale == Vector3.zero)
                btn.transform.localScale = Vector3.zero;
            else if (pulser != null)
                btn.transform.localScale = pulser.OriginalScale;
            else
                btn.transform.localScale = scale;
            var pulserScript = btn.GetComponent<ScalePulser>();
            if (pulserScript != null) pulserScript.enabled = false;
        }
    }

    // Helper to animate all button scales in a prefab
    private void AnimateButtonsScale(GameObject roomPrefab, bool scaleUp, float duration, Ease ease)
    {
        if (roomPrefab == null) return;
        var buttons = roomPrefab.GetComponentsInChildren<RoomTeleportButton>(true);
        Debug.Log($"[RoomManager] AnimateButtonsScale: {buttons.Length} buttons in {roomPrefab.name}");
        foreach (var btn in buttons)
        {
            btn.transform.DOKill();
            var pulser = btn.GetComponent<ScalePulser>();
            Vector3 targetScale = scaleUp && pulser != null ? pulser.OriginalScale : Vector3.zero;
            Debug.Log($"[RoomManager] Animating {btn.name} from {btn.transform.localScale} to {targetScale}");
            
            // Only disable pulser before animation if scaling up (to prevent it from interfering)
            // For scaling down, disable it after starting the animation to preserve current scale
            if (pulser != null && scaleUp) pulser.enabled = false;
            
            btn.transform.DOScale(targetScale, duration)
                .SetEase(ease)
                .OnComplete(() => {
                    if (pulser != null && scaleUp)
                    {
                        pulser.enabled = true;
                        pulser.RestartPulse();
                    }
                    else if (pulser != null && !scaleUp)
                    {
                        // Disable pulser after scale-down completes
                        pulser.enabled = false;
                    }
                });
        }
    }

    // Helper to find the sphere MeshRenderer in the prefab
    private MeshRenderer FindSphereRenderer(GameObject roomPrefab)
    {
        Debug.Log($"[RoomManager] FindSphereRenderer searching in: {roomPrefab.name}");
        
        // First, try to find by common naming patterns
        Transform sphereTransform = roomPrefab.transform.Find("Sphere");
        if (sphereTransform != null)
        {
            var mr = sphereTransform.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Debug.Log($"[RoomManager] Found sphere by name 'Sphere': {mr.gameObject.name}");
                return mr;
            }
        }
        
        // Try finding the root prefab's own MeshRenderer (some prefabs have sphere at root)
        var rootMR = roomPrefab.GetComponent<MeshRenderer>();
        if (rootMR != null)
        {
            Debug.Log($"[RoomManager] Found MeshRenderer on root: {roomPrefab.name}");
            return rootMR;
        }
        
        // Search all children - look for MeshRenderer with _Opacity property
        var allRenderers = roomPrefab.GetComponentsInChildren<MeshRenderer>(true);
        Debug.Log($"[RoomManager] Found {allRenderers.Length} MeshRenderers in children");
        
        foreach (var mr in allRenderers)
        {
            if (mr == null) continue;
            
            try
            {
                // Always prefer sharedMaterial to avoid creating instances during search
                Material matToCheck = mr.sharedMaterial;
                if (matToCheck != null && matToCheck.HasProperty("_Opacity"))
                {
                    Debug.Log($"[RoomManager] Found renderer with _Opacity: {mr.gameObject.name}");
                    return mr;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RoomManager] Error checking material on {mr.gameObject.name}: {e.Message}");
            }
        }
        
        // Last resort: just return the first MeshRenderer we find
        if (allRenderers.Length > 0)
        {
            Debug.LogWarning($"[RoomManager] No renderer with _Opacity found, using first MeshRenderer: {allRenderers[0].gameObject.name}");
            return allRenderers[0];
        }
        
        Debug.LogError($"[RoomManager] No MeshRenderer found at all in {roomPrefab.name}!");
        return null;
    }

    // Helper to find the sphere Transform in the prefab
    private Transform FindSphereTransform(GameObject roomPrefab)
    {
        // Try by name first
        Transform sphereTransform = roomPrefab.transform.Find("Sphere");
        if (sphereTransform != null) return sphereTransform;
        
        // Try root
        var rootMR = roomPrefab.GetComponent<MeshRenderer>();
        if (rootMR != null) return rootMR.transform;
        
        // Search children with material check - use sharedMaterial only
        foreach (var mr in roomPrefab.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (mr == null) continue;
            
            try
            {
                Material matToCheck = mr.sharedMaterial;
                if (matToCheck != null && matToCheck.HasProperty("_Opacity"))
                {
                    return mr.transform;
                }
            }
            catch
            {
                // Ignore errors, continue searching
            }
        }
        
        // Last resort: first renderer
        var allRenderers = roomPrefab.GetComponentsInChildren<MeshRenderer>(true);
        return allRenderers.Length > 0 ? allRenderers[0].transform : null;
    }
    
    /// <summary>
    /// Gets the appropriate texture for the current lighting mode
    /// </summary>
    /// <param name="roomData">The room data to get texture from</param>
    /// <returns>The texture to use based on current night mode state</returns>
    private Texture2D GetTextureForCurrentMode(RoomData roomData)
    {
        if (roomData == null) return null;
        
        // Check if NightModeManager exists and get night mode state
        bool isNightMode = false;
        var nightModeManager = FindFirstObjectByType<NightModeManager>();
        if (nightModeManager != null)
        {
            isNightMode = nightModeManager.IsNightMode();
        }
        
        return roomData.GetTextureForMode(isNightMode);
    }
    
    /// <summary>
    /// Updates the current room's texture based on night mode state (called by NightModeManager)
    /// </summary>
    public void UpdateCurrentRoomForNightMode()
    {
        if (currentRoomData == null || currentSphereRenderer == null) return;
        
        // Get or create cached material reference
        if (currentSphereMaterial == null && currentSphereRenderer != null)
        {
            currentSphereMaterial = currentSphereRenderer.sharedMaterial;
            if (currentSphereMaterial == null)
            {
                currentSphereMaterial = currentSphereRenderer.material;
            }
        }
        
        // Use addressables if available, otherwise fall back to direct reference
        if (currentRoomData.UsesAddressables() && TextureDownloadManager.Instance != null)
        {
            var nightModeManager = FindFirstObjectByType<NightModeManager>();
            bool isNightMode = nightModeManager != null && nightModeManager.IsNightMode();
            
            string textureAddress = currentRoomData.GetTextureAddressForMode(isNightMode);
            
            // Try to load from cache or download
            TextureDownloadManager.Instance.LoadTextureAsync(textureAddress,
                (loadedTexture) => {
                    if (currentSphereMaterial != null)
                    {
                        currentSphereMaterial.SetTexture("_MainTex", loadedTexture);
                        Debug.Log($"[RoomManager] Updated current room texture for {(isNightMode ? "night" : "day")} mode");
                    }
                },
                (error) => {
                    Debug.LogError($"[RoomManager] Failed to load texture for night mode update: {error}");
                }
            );
        }
        else
        {
            // Legacy direct reference mode
            Texture2D textureToUse = GetTextureForCurrentMode(currentRoomData);
            if (textureToUse != null)
            {
                currentSphereMaterial.SetTexture("_MainTex", textureToUse);
                
                var nightModeManager = FindFirstObjectByType<NightModeManager>();
                bool isNight = nightModeManager != null && nightModeManager.IsNightMode();
                Debug.Log($"[RoomManager] Updated current room texture for {(isNight ? "night" : "day")} mode");
            }
        }
    }
    
    // Notify UI components that the room has changed
    private void NotifyRoomChanged()
    {
        XRUIFlowManager uiManager = Object.FindFirstObjectByType<XRUIFlowManager>();
        if (uiManager != null)
        {
            // XRUIFlowManager reads room name from RoomManager.Instance.CurrentRoomData internally
            uiManager.ForceUpdateRoomInfoText();
        }
    }
    
    private IEnumerator VerifyRoomStateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"[RoomManager] ========== POST-SWITCH VERIFICATION (after {delay}s) ==========");
        Debug.Log($"[RoomManager] Current room prefab: {(currentRoomPrefab != null ? currentRoomPrefab.name : "NULL")}");
        Debug.Log($"[RoomManager] Current room active: {(currentRoomPrefab != null ? currentRoomPrefab.activeSelf : false)}");
        Debug.Log($"[RoomManager] Current renderer: {(currentSphereRenderer != null ? currentSphereRenderer.name : "NULL")}");
        Debug.Log($"[RoomManager] Current renderer enabled: {(currentSphereRenderer != null ? currentSphereRenderer.enabled : false)}");
        Debug.Log($"[RoomManager] Current material opacity: {(currentSphereMaterial != null ? currentSphereMaterial.GetFloat("_Opacity") : -1)}");
        Debug.Log($"[RoomManager] Current material texture: {(currentSphereMaterial != null && currentSphereMaterial.mainTexture != null ? currentSphereMaterial.mainTexture.name : "NULL")}");
        
        // Check for duplicate sphere renderers in scene (potential conflict)
        MeshRenderer[] allRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        int sphereCount = 0;
        int visibleSphereCount = 0;
        foreach (MeshRenderer renderer in allRenderers)
        {
            if (renderer.gameObject.name.Contains("Clone") || renderer.gameObject.name.Contains("Patio") || renderer.gameObject.name.Contains("E-"))
            {
                sphereCount++;
                if (renderer.enabled)
                {
                    visibleSphereCount++;
                    Material mat = renderer.sharedMaterial;
                    float opacity = mat != null ? mat.GetFloat("_Opacity") : -1;
                    Debug.Log($"[RoomManager] ⚠️ FOUND VISIBLE SPHERE: {renderer.gameObject.name} | Enabled: {renderer.enabled} | Opacity: {opacity} | Texture: {(mat != null && mat.mainTexture != null ? mat.mainTexture.name : "NULL")}");
                }
            }
        }
        Debug.Log($"[RoomManager] Total sphere GameObjects found: {sphereCount}, Visible: {visibleSphereCount}");
        
        if (visibleSphereCount > 1)
        {
            Debug.LogError($"[RoomManager] 🔴 CONFLICT DETECTED: {visibleSphereCount} visible sphere renderers found! Expected only 1.");
        }
        else if (visibleSphereCount == 0)
        {
            Debug.LogError($"[RoomManager] 🔴 ERROR: No visible sphere renderers found! Room may be invisible.");
        }
        else
        {
            Debug.Log($"[RoomManager] ✓ Room state looks correct - exactly 1 visible sphere");
        }
        
        Debug.Log($"[RoomManager] ========== END VERIFICATION ==========");
        
        // Continue monitoring for 5 more seconds to catch late modifications
        StartCoroutine(MonitorRoomStateChanges(5.0f));
    }
    
    private IEnumerator MonitorRoomStateChanges(float duration)
    {
        float elapsed = 0f;
        bool lastRendererState = currentSphereRenderer != null ? currentSphereRenderer.enabled : false;
        float lastOpacity = currentSphereMaterial != null ? currentSphereMaterial.GetFloat("_Opacity") : -1;
        
        Debug.Log($"[RoomManager] 👁️ Starting 5-second room state monitoring...");
        
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
            
            if (currentSphereRenderer == null || currentSphereMaterial == null)
            {
                Debug.LogError($"[RoomManager] 🔴 CRITICAL: Room references became NULL at {elapsed}s!");
                break;
            }
            
            bool currentRendererState = currentSphereRenderer.enabled;
            float currentOpacity = currentSphereMaterial.GetFloat("_Opacity");
            
            if (currentRendererState != lastRendererState || Mathf.Abs(currentOpacity - lastOpacity) > 0.01f)
            {
                Debug.LogWarning($"[RoomManager] ⚠️ STATE CHANGE DETECTED at {elapsed}s:");
                Debug.LogWarning($"[RoomManager]   Renderer: {lastRendererState} → {currentRendererState}");
                Debug.LogWarning($"[RoomManager]   Opacity: {lastOpacity} → {currentOpacity}");
                Debug.LogWarning($"[RoomManager]   GameObject active: {currentRoomPrefab.activeSelf}");
                
                lastRendererState = currentRendererState;
                lastOpacity = currentOpacity;
            }
        }
        
        Debug.Log($"[RoomManager] ✓ Monitoring complete - final state: Renderer={lastRendererState}, Opacity={lastOpacity}");
    }
} 