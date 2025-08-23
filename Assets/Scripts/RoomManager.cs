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

    private GameObject currentRoomPrefab;
    private MeshRenderer currentSphereRenderer;
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
        isFading = true;

        // Instantiate the new room prefab as a child of roomParent
        nextRoomPrefab = Instantiate(roomData.roomPrefab, roomParent);
        nextRoomPrefab.SetActive(true);
        nextSphereRenderer = FindSphereRenderer(nextRoomPrefab);
        if (nextSphereRenderer == null)
        {
            Debug.LogError("RoomManager: No MeshRenderer with correct shader found in new room prefab!");
            Destroy(nextRoomPrefab);
            isFading = false;
            yield break;
        }
        // Set the correct texture from RoomData based on current night mode
        Texture2D textureToUse = GetTextureForCurrentMode(roomData);
        if (textureToUse != null)
        {
            nextSphereRenderer.material.SetTexture("_MainTex", textureToUse);
        }
        // Set initial opacity to 0
        nextSphereRenderer.material.SetFloat("_Opacity", 0f);

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
        Sequence fadeSeq = DOTween.Sequence();
        fadeSeq.Join(
            DOTween.To(
                () => nextSphereRenderer.material.GetFloat("_Opacity"),
                x => nextSphereRenderer.material.SetFloat("_Opacity", x),
                1f, fadeDuration
            ).SetEase(fadeEase)
        );
        if (currentSphereRenderer != null)
        {
            fadeSeq.Join(
                DOTween.To(
                    () => currentSphereRenderer.material.GetFloat("_Opacity"),
                    x => currentSphereRenderer.material.SetFloat("_Opacity", x),
                    0f, fadeDuration
                ).SetEase(fadeEase)
            );
        }
        fadeSeq.Play();
        yield return fadeSeq.WaitForCompletion();

        // Cleanup old prefab
        if (currentRoomPrefab != null)
        {
            Destroy(currentRoomPrefab);
        }
        currentRoomPrefab = nextRoomPrefab;
        currentSphereRenderer = nextSphereRenderer;
        nextRoomPrefab = null;
        nextSphereRenderer = null;
        
        // Store the current room data
        currentRoomData = roomData;
        Debug.Log($"[RoomManager] Successfully switched to room: {roomData.roomName}");
        
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
            }
        }
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
        // Assumes the sphere is a child named "Sphere" or has a MeshRenderer with the correct shader
        foreach (var mr in roomPrefab.GetComponentsInChildren<MeshRenderer>())
        {
            if (mr.material.HasProperty("_Opacity"))
                return mr;
        }
        return null;
    }

    // Helper to find the sphere Transform in the prefab
    private Transform FindSphereTransform(GameObject roomPrefab)
    {
        foreach (var mr in roomPrefab.GetComponentsInChildren<MeshRenderer>())
        {
            if (mr.material.HasProperty("_Opacity"))
                return mr.transform;
        }
        return null;
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
        
        Texture2D textureToUse = GetTextureForCurrentMode(currentRoomData);
        if (textureToUse != null)
        {
            currentSphereRenderer.material.SetTexture("_MainTex", textureToUse);
            
            var nightModeManager = FindFirstObjectByType<NightModeManager>();
            bool isNight = nightModeManager != null && nightModeManager.IsNightMode();
            Debug.Log($"[RoomManager] Updated current room texture for {(isNight ? "night" : "day")} mode");
        }
    }
    
    // Notify UI components that the room has changed
    private void NotifyRoomChanged()
    {
        // Find XRUIFlowManager and update room info
        XRUIFlowManager uiManager = Object.FindFirstObjectByType<XRUIFlowManager>();
        if (uiManager != null)
        {
            uiManager.OnRoomChanged();
        }
    }
} 