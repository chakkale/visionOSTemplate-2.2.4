using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.State;
using TMPro;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class XRUIFlowManager : MonoBehaviour
{
    [Header("Scene GameObjects")]
    [SerializeField] private GameObject introTestButton;
    [SerializeField] private GameObject mainMenu; // Parent container for all main menu elements
    [SerializeField] private GameObject mapButton; // Child of MainMenu
    [SerializeField] private GameObject dayNightButton; // Child of MainMenu (DarkLightToggle)
    [SerializeField] private GameObject currentMapInfoDisplay; // MapInfo - Child of MainMenu
    [SerializeField] private GameObject activeRoomInfoDisplay; // RoomInfo - Child of MainMenu
    [SerializeField] private GameObject mapMain;
    [SerializeField] private GameObject mapList; // Child of MapMain
    [SerializeField] private GameObject closeButton; // Child of MapMain
    [SerializeField] private GameObject[] maps = new GameObject[6]; // Children of MapMain
    
    [Header("UI Text Components")]
    [SerializeField] private TMP_Text mapInfoText; // TMP_Text component under MapInfo to show selected map name
    [SerializeField] private TMP_Text roomInfoText; // TMP_Text component under RoomInfo to show current room name
    
    [Header("Dark/Light Toggle Components")]
    [SerializeField] private SpriteRenderer iconLight; // Light mode sprite
    [SerializeField] private SpriteRenderer iconDark; // Dark mode sprite
    [SerializeField] private MeshRenderer buttonMeshRenderer; // The 3D mesh button beneath the icons
    [SerializeField] private float toggleFadeDuration = 0.5f;
    [SerializeField] private Ease toggleFadeEase = Ease.InOutQuad;
    [SerializeField] private float colorTransitionDuration = 0.5f;
    [SerializeField] private Ease colorTransitionEase = Ease.InOutQuad;
    [SerializeField] private bool syncColorTransitionWithSprites = true; // Automatically sync color transition duration with sprite fade duration
    
    [Header("Toggle Colors")]
    [SerializeField] private Color lightModeColor = new Color(213f/255f, 200f/255f, 187f/255f, 123f/255f); // #D5C8BB with alpha 123
    [SerializeField] private Color darkModeColor = new Color(45f/255f, 45f/255f, 45f/255f, 123f/255f); // #2D2D2D with alpha 123
    
    [Header("Map Button Text Colors")]
    [SerializeField] private Color activeMapButtonTextColor = Color.black; // Text color when button is active
    
    [Header("XR Interactables")]
    [SerializeField] private XRSimpleInteractable startInteractable; // In IntroTestButton
    [SerializeField] private XRSimpleInteractable mapInteractable; // In MapButton
    [SerializeField] private XRSimpleInteractable dayNightInteractable; // In DayNightButton
    [SerializeField] private XRSimpleInteractable closeInteractable; // In CloseButton
    
    [Header("Animation Settings")]
    [SerializeField] private float scaleAnimationDuration = 0.6f;
    [SerializeField] private Ease scaleUpEase = Ease.OutBack;
    [SerializeField] private Ease scaleDownEase = Ease.InBack;
    #pragma warning disable CS0414 // Field is assigned but its value is never used
    [SerializeField] private float animationDelay = 0.1f;
    #pragma warning restore CS0414 // Field is assigned but its value is never used
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool autoRemoveBaseMapTexture = true; // Automatically remove Base Map texture to allow color changes
    
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDebounceTime = 0.3f; // Minimum time between interactions to prevent fast-clicking issues
    
    private Vector3 mapButtonOriginalScale;
    private Vector3 dayNightButtonOriginalScale;
    private Vector3 currentMapInfoDisplayOriginalScale;
    private Vector3 activeRoomInfoDisplayOriginalScale;
    private Vector3 mapMainOriginalScale;
    private Vector3 mapListOriginalScale;
    private Vector3 closeButtonOriginalScale;
    private Vector3[] roomMapOriginalScales = new Vector3[6];
    private bool isTransitioning = false;
    private float lastInteractionTime = 0f;
    
    // Current UI state
    public enum UIState { Start, MainMenu, MapView }
    private UIState currentState = UIState.Start;
    private int currentRoomIndex = -1; // Remembers which room user was in
    private int activeMapIndex = -1; // Currently active map button
    
    // Dark/Light toggle state
    private bool isLightMode = true; // Default to light mode
    private bool isTogglingMode = false; // Prevent multiple toggles during animation
    
    // Material instances for sprites and button mesh
    private Material iconLightMaterial;
    private Material iconDarkMaterial;
    private Material buttonMeshMaterial;
    private Color originalIconLightColor;
    private Color originalIconDarkColor;
    
    // Map button text color management
    private TMP_Text[] mapButtonTexts = new TMP_Text[6]; // Text components for each map button
    private Color[] originalMapButtonTextColors = new Color[6]; // Original text colors for each map button
    
    // Button active states
    private GameObject[] mainMenuButtons; // Array of main menu buttons for easy management
    #pragma warning disable CS0618 // Type or member is obsolete
    private Dictionary<int, XRInteractableAffordanceStateProvider> mapButtonAffordanceProviders = new Dictionary<int, XRInteractableAffordanceStateProvider>(); // Cache affordance providers
    #pragma warning restore CS0618 // Type or member is obsolete
    
    private void Awake()
    {
        FindSceneReferences();
        StoreOriginalValues();
        SetupMainMenuButtons();
        SetupXRListeners();
        InitializeUI();
    }
    
    private void FindSceneReferences()
    {
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Finding scene references...");
        
        // Find scene GameObjects if not assigned
        if (introTestButton == null)
        {
            introTestButton = GameObject.Find("IntroTestButton");
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] IntroTestButton found: {introTestButton != null}");
        }
        
        // Find MainMenu container first
        if (mainMenu == null)
        {
            mainMenu = GameObject.Find("MainMenu");
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] MainMenu found: {mainMenu != null}");
        }
        
        // Find children within MainMenu container
        if (mainMenu != null)
        {
        if (mapButton == null)
            {
                Transform mapButtonTransform = FindChildRecursive(mainMenu.transform, "MapButton");
                if (mapButtonTransform != null)
                    mapButton = mapButtonTransform.gameObject;
                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] MapButton found in MainMenu: {mapButton != null}");
            }
                
            if (dayNightButton == null)
            {
                Transform dayNightTransform = FindChildRecursive(mainMenu.transform, "DarkLightToggle");
                if (dayNightTransform != null)
                    dayNightButton = dayNightTransform.gameObject;
                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] DarkLightToggle found in MainMenu: {dayNightButton != null}");
            }
                
            if (currentMapInfoDisplay == null)
            {
                Transform mapInfoTransform = FindChildRecursive(mainMenu.transform, "MapInfo");
                if (mapInfoTransform != null)
                    currentMapInfoDisplay = mapInfoTransform.gameObject;
                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] MapInfo found in MainMenu: {currentMapInfoDisplay != null}");
            }
                
            if (activeRoomInfoDisplay == null)
            {
                Transform roomInfoTransform = FindChildRecursive(mainMenu.transform, "RoomInfo");
                if (roomInfoTransform != null)
                    activeRoomInfoDisplay = roomInfoTransform.gameObject;
                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] RoomInfo found in MainMenu: {activeRoomInfoDisplay != null}");
            }
            
            // Find TMP_Text component under MapInfo
            if (mapInfoText == null && currentMapInfoDisplay != null)
            {
                mapInfoText = currentMapInfoDisplay.GetComponentInChildren<TMP_Text>();
                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] MapInfo TMP_Text component found: {mapInfoText != null}");
            }
            
            // Find TMP_Text component under RoomInfo
            if (roomInfoText == null && activeRoomInfoDisplay != null)
            {
                roomInfoText = activeRoomInfoDisplay.GetComponentInChildren<TMP_Text>();
                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] RoomInfo TMP_Text component found: {roomInfoText != null}");
            }
            
            // Find Dark/Light toggle sprite components
            if (dayNightButton != null)
            {
                if (iconLight == null)
                {
                    // Try multiple possible names for light icon
                    string[] lightNames = {"iconLight", "IconLight", "LightIcon", "Light", "Sun", "Day"};
                    foreach (string lightName in lightNames)
                    {
                        Transform iconLightTransform = FindChildRecursive(dayNightButton.transform, lightName);
                        if (iconLightTransform != null)
                        {
                            iconLight = iconLightTransform.GetComponent<SpriteRenderer>();
                            if (iconLight != null)
                            {
                                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] iconLight found: {iconLight != null} (GameObject: {iconLightTransform.name})");
                                break;
                            }
                        }
                    }
                    
                    if (iconLight == null)
                    {
                        if (enableDebugLogs) Debug.LogWarning("[XRUIFlowManager] iconLight not found with any expected names. Listing all children:");
                        ListAllChildren(dayNightButton.transform, 0);
                    }
                }
                
                if (iconDark == null)
                {
                    // Try multiple possible names for dark icon
                    string[] darkNames = {"iconDark", "IconDark", "DarkIcon", "Dark", "Moon", "Night"};
                    foreach (string darkName in darkNames)
                    {
                        Transform iconDarkTransform = FindChildRecursive(dayNightButton.transform, darkName);
                        if (iconDarkTransform != null)
                        {
                            iconDark = iconDarkTransform.GetComponent<SpriteRenderer>();
                            if (iconDark != null)
                            {
                                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] iconDark found: {iconDark != null} (GameObject: {iconDarkTransform.name})");
                                break;
                            }
                        }
                    }
                    
                    if (iconDark == null)
                    {
                        if (enableDebugLogs) Debug.LogWarning("[XRUIFlowManager] iconDark not found with any expected names");
                    }
                }
                
                // Find the button mesh renderer (usually the main mesh under the button)
                if (buttonMeshRenderer == null)
                {
                    // Try to find the mesh renderer on the dayNightButton itself first
                    buttonMeshRenderer = dayNightButton.GetComponent<MeshRenderer>();
                    if (buttonMeshRenderer == null)
                    {
                        // If not found, look for the first mesh renderer in children
                        MeshRenderer[] allMeshRenderers = dayNightButton.GetComponentsInChildren<MeshRenderer>();
                        if (allMeshRenderers.Length > 0)
                        {
                            buttonMeshRenderer = allMeshRenderers[0];
                            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Found {allMeshRenderers.Length} mesh renderers, using: {buttonMeshRenderer.name}");
                            
                            // List all mesh renderers for debugging
                            for (int i = 0; i < allMeshRenderers.Length; i++)
                            {
                                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] MeshRenderer {i}: {allMeshRenderers[i].name} on {allMeshRenderers[i].gameObject.name}");
                            }
                        }
                    }
                    else
                    {
                        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] buttonMeshRenderer found on dayNightButton itself: {buttonMeshRenderer.name}");
                    }
                    
                    if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Final buttonMeshRenderer: {buttonMeshRenderer != null}");
                }
            }
            else
            {
                if (enableDebugLogs) Debug.LogWarning("[XRUIFlowManager] dayNightButton is null - cannot find icon sprites");
            }
        }
            
        if (mapMain == null)
        {
            mapMain = GameObject.Find("MapMain");
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] MapMain found: {mapMain != null}");
        }
        
        if (mapMain != null)
        {
            // Find mapList within MapMain
            Transform mapListTransform = FindChildRecursive(mapMain.transform, "mapList");
            if (mapListTransform != null)
                mapList = mapListTransform.gameObject;
            
            // Find CloseButton within MapMain
            Transform closeButtonTransform = FindChildRecursive(mapMain.transform, "CloseButton");
            if (closeButtonTransform != null)
                closeButton = closeButtonTransform.gameObject;
            
            // Find all room maps within MapMain
            for (int i = 0; i < 6; i++)
            {
                string mapName = GetMapName(i);
                Transform roomTransform = FindChildRecursive(mapMain.transform, mapName);
                if (roomTransform != null)
                {
                    maps[i] = roomTransform.gameObject;
                    if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Found {mapName} in MapMain");
                }
            }
        }
        
        // Find XR Interactables (only for interactive elements)
        if (startInteractable == null && introTestButton != null)
        {
            startInteractable = introTestButton.GetComponentInChildren<XRSimpleInteractable>();
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] StartInteractable found: {startInteractable != null}");
        }
            
        if (mapInteractable == null && mapButton != null)
        {
            mapInteractable = mapButton.GetComponentInChildren<XRSimpleInteractable>();
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] MapInteractable found: {mapInteractable != null}");
        }
            
        if (dayNightInteractable == null && dayNightButton != null)
        {
            dayNightInteractable = dayNightButton.GetComponentInChildren<XRSimpleInteractable>();
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] DayNightInteractable found: {dayNightInteractable != null}");
        }
            
        // Current Map Info and Active Room Info are non-interactive display elements
        // No XRSimpleInteractable components needed
            
        if (closeInteractable == null && closeButton != null)
        {
            closeInteractable = closeButton.GetComponent<XRSimpleInteractable>();
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] CloseInteractable found: {closeInteractable != null}");
        }
    }
    
    private string GetMapName(int index)
    {
        switch (index)
        {
            case 0: return "Patio";
            case 1: return "1ºD";
            case 2: return "1ºE";
            case 3: return "2ºA";
            case 4: return "3ºA";
            case 5: return "Rooftop";
            default: return $"Map{index + 1}";
        }
    }
    
    private void SetupMainMenuButtons()
    {
        // Create array of main menu elements for easy management (both interactive and non-interactive)
        // All elements are now children of MainMenu container
        mainMenuButtons = new GameObject[] { mapButton, dayNightButton, currentMapInfoDisplay, activeRoomInfoDisplay };
        
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Setup main menu buttons array with {mainMenuButtons.Length} elements");
    }
    
    private bool CanInteract(string interactionSource = "")
    {
        float currentTime = Time.time;
        
        // For room selections, use lighter debouncing to prevent fast switching issues
        float effectiveDebounceTime = interactionSource.Contains("Room") ? 0.1f : interactionDebounceTime;
        
        bool canInteract = (currentTime - lastInteractionTime) >= effectiveDebounceTime;
        
        if (!canInteract && enableDebugLogs)
        {
            Debug.Log($"[XRUIFlowManager] {interactionSource} interaction blocked - debounce (last: {lastInteractionTime:F2}, current: {currentTime:F2}, diff: {currentTime - lastInteractionTime:F2}, required: {effectiveDebounceTime:F2})");
        }
        
        return canInteract;
    }
    
    private void RegisterInteraction(string interactionSource = "")
    {
        lastInteractionTime = Time.time;
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] {interactionSource} interaction registered at time {lastInteractionTime:F2}");
    }
    
    private void StartTransition(string transitionName = "")
    {
        if (isTransitioning && enableDebugLogs)
        {
            Debug.LogWarning($"[XRUIFlowManager] Starting transition '{transitionName}' but already transitioning!");
        }
        
        isTransitioning = true;
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Started transition: {transitionName}");
    }
    
    private void EndTransition(string transitionName = "")
    {
        isTransitioning = false;
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Ended transition: {transitionName}");
    }
    
    private void KillSpecificAnimations(string context = "")
    {
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Killing animations - context: {context}");
        
        // Kill main menu button animations
        if (mainMenuButtons != null)
        {
            foreach (var button in mainMenuButtons)
            {
                if (button != null) DOTween.Kill(button.transform);
            }
        }
        
        // Kill map main animations
        if (mapMain != null) DOTween.Kill(mapMain.transform);
        
        // Kill room animations
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i] != null) DOTween.Kill(maps[i].transform);
        }
        
        // Kill toggle animations
        if (iconLightMaterial != null) DOTween.Kill(iconLightMaterial);
        if (iconDarkMaterial != null) DOTween.Kill(iconDarkMaterial);
        if (buttonMeshMaterial != null) DOTween.Kill(buttonMeshMaterial);
    }
    
    private void StoreOriginalValues()
    {
        if (mapButton != null)
            mapButtonOriginalScale = mapButton.transform.localScale;
            
        if (dayNightButton != null)
            dayNightButtonOriginalScale = dayNightButton.transform.localScale;
            
        if (currentMapInfoDisplay != null)
            currentMapInfoDisplayOriginalScale = currentMapInfoDisplay.transform.localScale;
            
        if (activeRoomInfoDisplay != null)
            activeRoomInfoDisplayOriginalScale = activeRoomInfoDisplay.transform.localScale;
            
        if (mapMain != null)
            mapMainOriginalScale = mapMain.transform.localScale;
            
        if (mapList != null)
            mapListOriginalScale = mapList.transform.localScale;
            
        if (closeButton != null)
            closeButtonOriginalScale = closeButton.transform.localScale;
        
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i] != null)
                roomMapOriginalScales[i] = maps[i].transform.localScale;
        }
        
        // Find and store map button text components and their original colors
        FindMapButtonTexts();
    }
    
    private void FindMapButtonTexts()
    {
        if (mapList == null) 
        {
            if (enableDebugLogs) Debug.LogError("[XRUIFlowManager] MapList is null - cannot find map button texts!");
            return;
        }
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Finding map button text components...");
        
        for (int i = 0; i < 6; i++)
        {
            string mapButtonName = $"Map{i + 1}Button";
            Transform buttonTransform = FindChildRecursive(mapList.transform, mapButtonName);
            
            if (buttonTransform != null)
            {
                // Find the Text component under the button
                TMP_Text textComponent = buttonTransform.GetComponentInChildren<TMP_Text>();
                if (textComponent != null)
                {
                    mapButtonTexts[i] = textComponent;
                    originalMapButtonTextColors[i] = textComponent.color;
                    if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Found text for {mapButtonName}: original color R={textComponent.color.r:F2} G={textComponent.color.g:F2} B={textComponent.color.b:F2} A={textComponent.color.a:F2}");
                }
                else
                {
                    if (enableDebugLogs) Debug.LogWarning($"[XRUIFlowManager] No TMP_Text component found for {mapButtonName}!");
                }
            }
            else
            {
                if (enableDebugLogs) Debug.LogWarning($"[XRUIFlowManager] {mapButtonName} not found in mapList!");
            }
        }
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Map button text components initialization complete");
    }
    
    private void SetMapButtonTextColor(int mapIndex, bool active)
    {
        if (mapIndex < 0 || mapIndex >= mapButtonTexts.Length) return;
        if (mapButtonTexts[mapIndex] == null) return;
        
        Color targetColor = active ? activeMapButtonTextColor : originalMapButtonTextColors[mapIndex];
        mapButtonTexts[mapIndex].color = targetColor;
        
        if (enableDebugLogs)
        {
            string mapName = GetMapName(mapIndex);
            string state = active ? "ACTIVE (black)" : "INACTIVE (original)";
            Debug.Log($"[XRUIFlowManager] Set {mapName} text color to {state}: R={targetColor.r:F2} G={targetColor.g:F2} B={targetColor.b:F2} A={targetColor.a:F2}");
        }
    }
    
    private void ResetAllMapButtonTextColors()
    {
        for (int i = 0; i < mapButtonTexts.Length; i++)
        {
            SetMapButtonTextColor(i, false); // Reset to original color
        }
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] All map button text colors reset to original");
    }
    
    private void SetupXRListeners()
    {
        if (startInteractable != null)
        {
            startInteractable.selectEntered.AddListener(OnStartSelected);
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Start interactable listener added");
        }
        
        if (mapInteractable != null)
        {
            mapInteractable.selectEntered.AddListener(OnMapSelected);
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Map interactable listener added");
        }
        
        if (dayNightInteractable != null)
        {
            dayNightInteractable.selectEntered.AddListener(OnDayNightSelected);
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Day/Night interactable listener added");
        }
        
        // Current Map Info and Active Room Info displays are non-interactive
        // No event listeners needed
        
        if (closeInteractable != null)
        {
            closeInteractable.selectEntered.AddListener(OnCloseSelected);
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Close interactable listener added");
        }
        
        // Setup room button listeners  
        SetupRoomButtonListeners();
        
        // Cache affordance providers for better performance
        CacheAffordanceProviders();
    }
    
    private void SetupRoomButtonListeners()
    {
        if (mapList == null) 
        {
            if (enableDebugLogs) Debug.LogError("[XRUIFlowManager] MapList is null - cannot setup room button listeners!");
            return;
        }
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Setting up room button listeners in mapList");
        
        for (int i = 0; i < 6; i++)
        {
            string mapButtonName = $"Map{i + 1}Button";
            string mapName = GetMapName(i);
            Transform buttonTransform = FindChildRecursive(mapList.transform, mapButtonName);
            
            if (buttonTransform != null)
            {
                XRSimpleInteractable roomButton = buttonTransform.GetComponent<XRSimpleInteractable>();
                if (roomButton != null)
                {
                    int roomIndex = i; // Capture for closure
                    roomButton.selectEntered.AddListener((args) => OnRoomSelected(roomIndex));
                    if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] {mapButtonName} (Map {i + 1} - {mapName}) button listener added successfully");
                }
                else
                {
                    if (enableDebugLogs) Debug.LogWarning($"[XRUIFlowManager] {mapButtonName} button found but has no XRSimpleInteractable component!");
                }
            }
            else
            {
                if (enableDebugLogs) Debug.LogWarning($"[XRUIFlowManager] {mapButtonName} button not found in mapList!");
            }
        }
    }
    
    #pragma warning disable CS0618 // Type or member is obsolete
    private void CacheAffordanceProviders()
    {
        if (mapList == null) return;
        
        mapButtonAffordanceProviders.Clear();
        
        for (int i = 0; i < 6; i++)
        {
            string mapButtonName = $"Map{i + 1}Button";
            Transform buttonTransform = FindChildRecursive(mapList.transform, mapButtonName);
            
            if (buttonTransform != null)
            {
                XRInteractableAffordanceStateProvider affordanceProvider = buttonTransform.GetComponentInChildren<XRInteractableAffordanceStateProvider>();
                if (affordanceProvider != null)
                {
                    mapButtonAffordanceProviders[i] = affordanceProvider;
                    if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Cached affordance provider for {mapButtonName}");
                }
                else
                {
                    if (enableDebugLogs) Debug.LogWarning($"[XRUIFlowManager] No affordance provider found for {mapButtonName}");
                }
            }
        }
        
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Cached {mapButtonAffordanceProviders.Count} affordance providers");
    }
    #pragma warning restore CS0618 // Type or member is obsolete
    
    private void InitializeUI()
    {
        // Set initial states - hide all main menu buttons and map main
        HideAllMainMenuButtons();
        if (mapMain != null)
            mapMain.SetActive(false);
        
        // Set default room to Patio (index 0)
        currentRoomIndex = 0;
        
        // Initialize MapInfo text to show default room
        UpdateMapInfoText();
        
        // Initialize RoomInfo text to show current room from RoomManager
        UpdateRoomInfoText();
        
        // Initialize Dark/Light toggle to light mode
        InitializeDarkLightToggle();
        
        currentState = UIState.Start;
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] UI initialized with Patio as default room");
    }
    
    private void OnStartSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (!CanInteract("Start")) return;
        
        RegisterInteraction("Start");
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Start button selected - going to MainMenu");
        
        // Set Patio as default room for when user eventually goes to MapMain
        currentRoomIndex = 0; // Set Patio as default
        ShowMainMenu();
    }
    
    private void OnMapSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (!CanInteract("Map")) return;
        
        RegisterInteraction("Map");
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Map button selected - starting ShowMapView");
        
        SetButtonActiveState(mapButton, true);
        ShowMapView();
    }
    
    private void OnDayNightSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (!CanInteract("DayNight") || isTogglingMode) return;
        
        RegisterInteraction("DayNight");
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Dark/Light toggle button selected");
        
        ToggleDarkLightMode();
    }
    
    // Note: OnCurrentMapInfoSelected and OnActiveRoomInfoSelected methods removed
    // as these are now non-interactive display elements
    
    private void OnRoomSelected(int roomIndex)
    {
        if (!CanInteract($"Room{roomIndex + 1}") || roomIndex < 0 || roomIndex >= maps.Length) 
        {
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Room selection blocked - index: {roomIndex}");
            return;
        }
        
        if (maps[roomIndex] == null) 
        {
            if (enableDebugLogs) Debug.LogError($"[XRUIFlowManager] Room {roomIndex + 1} map is null!");
            return;
        }
        
        RegisterInteraction($"Room{roomIndex + 1}");
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Room {roomIndex + 1} selected - changing from room {currentRoomIndex} to {roomIndex}");
        
        StartTransition($"Room{roomIndex + 1}Selection");
        
        // Store the old room index before updating
        int oldRoomIndex = currentRoomIndex;
        
        // Update current room index to new room
        currentRoomIndex = roomIndex;
        
        // Update MapInfo text to show selected map name
        UpdateMapInfoText();
        
        // Set active state for the selected map button (this will reset others and set this one active)
        SetMapButtonActiveState(roomIndex, true);
        
        // Kill any existing room animations to prevent overlapping
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i] != null) DOTween.Kill(maps[i].transform);
        }
        
        // Hide old room and show selected room
        HideSpecificRoom(oldRoomIndex, () => {
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Old room {oldRoomIndex + 1} hidden, now showing room {roomIndex + 1}");
            ShowRoom(roomIndex);
            
            // NEW: Actually load the room data through RoomManager
            LoadRoomData(roomIndex);
        });
    }
    
    private void OnCloseSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (!CanInteract("Close")) return;
        
        RegisterInteraction("Close");
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Close button selected - closing map view");
        
        // Reset all button active states
        ResetAllButtonActiveStates();
        
        HideMapView();
    }
    
    private void ShowMainMenu()
    {
        StartTransition("ShowMainMenu");
        currentState = UIState.MainMenu;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Showing main menu");
        
        // Update room info text before showing main menu
        UpdateRoomInfoText();
        
        // Show all main menu buttons with staggered animation
        ShowAllMainMenuButtons(() => {
            EndTransition("ShowMainMenu");
        });
    }
    
    private void ShowMapView()
    {
        StartTransition("ShowMapView");
        currentState = UIState.MapView;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Showing map view - ensuring main menu is hidden first");
        
        // Always ensure main menu buttons are hidden before showing MapMain
        // Check if any main menu buttons are currently visible
        bool anyMainMenuVisible = false;
        foreach (var button in mainMenuButtons)
        {
            if (button != null && button.activeInHierarchy)
            {
                anyMainMenuVisible = true;
                break;
            }
        }
        
        if (anyMainMenuVisible)
        {
            // Hide main menu buttons first, then show MapMain
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Main menu buttons visible - hiding them first");
            HideAllMainMenuButtons(() => {
                if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Main menu buttons hidden - now showing MapMain");
                ShowMapMain();
            });
        }
        else
        {
            // Main menu already hidden, go directly to MapMain
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Main menu already hidden - showing MapMain directly");
            ShowMapMain();
        }
    }
    
    private void HideMapView()
    {
        StartTransition("HideMapView");
        currentState = UIState.MainMenu;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Hiding map view - starting transition back to main menu");
        
        // Hide map main, then show main menu buttons
        HideMapMain(() => {
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] MapMain hidden - now showing main menu buttons");
            ShowAllMainMenuButtons(() => {
                EndTransition("HideMapView");
                if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Main menu buttons shown - transition complete");
            });
        });
    }
    
    private void ShowAllMainMenuButtons(System.Action onComplete = null)
    {
        // Kill any existing main menu animations
        foreach (var button in mainMenuButtons)
        {
            if (button != null) DOTween.Kill(button.transform);
        }
        
        var activeButtons = new List<GameObject>();
        foreach (var button in mainMenuButtons)
        {
            if (button != null) activeButtons.Add(button);
        }
        
        if (activeButtons.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }
        
        int buttonsCompleted = 0;
        System.Action checkComplete = () => {
            buttonsCompleted++;
            if (buttonsCompleted >= activeButtons.Count)
            {
                onComplete?.Invoke();
            }
        };
        
        for (int i = 0; i < activeButtons.Count; i++)
        {
            var button = activeButtons[i];
            button.SetActive(true);
            button.transform.localScale = Vector3.zero;
            
            // Stagger the animations
            float delay = i * 0.1f;
            button.transform.DOScale(GetButtonOriginalScale(button), scaleAnimationDuration)
                .SetEase(scaleUpEase)
                .SetDelay(delay)
                .SetId($"ShowMainMenuButton_{i}")
                .OnComplete(() => checkComplete());
        }
    }
    
    private void HideAllMainMenuButtons(System.Action onComplete = null)
    {
        // Kill any existing main menu animations
        foreach (var button in mainMenuButtons)
        {
            if (button != null) DOTween.Kill(button.transform);
        }
        
        var activeButtons = new List<GameObject>();
        foreach (var button in mainMenuButtons)
        {
            if (button != null && button.activeInHierarchy) activeButtons.Add(button);
        }
        
        if (activeButtons.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }
        
        int buttonsCompleted = 0;
        System.Action checkComplete = () => {
            buttonsCompleted++;
            if (buttonsCompleted >= activeButtons.Count)
            {
                onComplete?.Invoke();
            }
        };
        
        for (int i = 0; i < activeButtons.Count; i++)
        {
            var button = activeButtons[i];
            
            // Reverse stagger the animations
            float delay = (activeButtons.Count - i - 1) * 0.1f;
            button.transform.DOScale(Vector3.zero, scaleAnimationDuration)
                .SetEase(scaleDownEase)
                .SetDelay(delay)
                .SetId($"HideMainMenuButton_{i}")
                .OnComplete(() => {
                    if (button != null)
                    {
                        button.SetActive(false);
                    }
                    checkComplete();
                });
        }
    }
    
    private void ShowMapMain()
    {
        if (mapMain == null) 
        {
            if (enableDebugLogs) Debug.LogError("[XRUIFlowManager] MapMain is null - cannot show map!");
            return;
        }
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] ShowMapMain called - activating MapMain");
        
        // Activate MapMain and reset it to its prefab state
            mapMain.SetActive(true);
        
        // Reset all child components to their original prefab states
        ResetMapMainToPrefabState();
        
        // Start with scale zero for animation
            mapMain.transform.localScale = Vector3.zero;
            
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Starting MapMain scale animation");
            
        // Animate MapMain scaling up
            mapMain.transform.DOScale(mapMainOriginalScale, scaleAnimationDuration)
                .SetEase(scaleUpEase)
            .SetId("ShowMapMain")
            .OnComplete(() => {
                EndTransition("ShowMapMain");
                // Ensure button states are correct after animation
                RestoreAllButtonStates();
                if (enableDebugLogs) Debug.Log("[XRUIFlowManager] MapMain animation complete - MapMain should be visible now");
            });
    }
    
    private void ResetMapMainToPrefabState()
    {
        if (mapMain == null) return;
        
        // Ensure mapList is active and properly scaled (should be visible in prefab)
        if (mapList != null)
        {
            mapList.SetActive(true);
            mapList.transform.localScale = mapListOriginalScale;
        }
        
        // Ensure close button is active and properly scaled (should be visible in prefab)
        if (closeButton != null)
        {
            closeButton.SetActive(true);
            closeButton.transform.localScale = closeButtonOriginalScale;
        }
        
        // Reset all room maps to their prefab states
        // Make sure Patio (index 0) is active by default if no current selection
        if (currentRoomIndex == -1)
        {
            currentRoomIndex = 0; // Default to Patio
        }
        
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i] != null)
            {
                // Reset each room map to its original scale
                maps[i].transform.localScale = roomMapOriginalScales[i];
                
                // Activate the current room (Patio by default)
                if (i == currentRoomIndex)
                {
                    maps[i].SetActive(true);
                    if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Activated map {i + 1} ({GetMapName(i)}) as default");
                }
                else
                {
                    maps[i].SetActive(false);
                }
            }
        }
        
        // Ensure affordance providers are cached (in case MapMain was recreated)
        CacheAffordanceProviders();
        
        // Restore all button states (ensure visual states are correct)
        RestoreAllButtonStates();
        
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] MapMain reset to prefab state with {GetMapName(currentRoomIndex)} active");
    }
    
    private void HideMapMain(System.Action onComplete = null)
    {
        if (mapMain == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        mapMain.transform.DOScale(Vector3.zero, scaleAnimationDuration)
            .SetEase(scaleDownEase)
            .SetId("HideMapMain")
            .OnComplete(() => {
                mapMain.SetActive(false);
                onComplete?.Invoke();
            });
    }
    
    private void ShowRoom(int roomIndex)
    {
        if (roomIndex < 0 || roomIndex >= maps.Length || maps[roomIndex] == null) return;
        
        currentRoomIndex = roomIndex;
        
        maps[roomIndex].SetActive(true);
        maps[roomIndex].transform.localScale = Vector3.zero;
        
        maps[roomIndex].transform.DOScale(roomMapOriginalScales[roomIndex], scaleAnimationDuration)
            .SetEase(scaleUpEase)
            .SetId($"ShowRoom_{roomIndex}")
            .OnComplete(() => {
                EndTransition($"Room{roomIndex + 1}Selection");
                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Room {roomIndex + 1} shown");
            });
    }
    
    private void HideCurrentRoom(System.Action onComplete = null)
    {
        HideSpecificRoom(currentRoomIndex, onComplete);
    }
    
    private void HideSpecificRoom(int roomIndex, System.Action onComplete = null)
    {
        if (roomIndex >= 0 && roomIndex < maps.Length && maps[roomIndex] != null)
        {
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Hiding room {roomIndex + 1} ({GetMapName(roomIndex)})");
            
            maps[roomIndex].transform.DOScale(Vector3.zero, scaleAnimationDuration)
                .SetEase(scaleDownEase)
                .SetId($"HideRoom_{roomIndex}")
                .OnComplete(() => {
                    maps[roomIndex].SetActive(false);
                    if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Room {roomIndex + 1} ({GetMapName(roomIndex)}) hidden");
                    onComplete?.Invoke();
                });
        }
        else
        {
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] No room to hide (index: {roomIndex})");
            onComplete?.Invoke();
        }
    }
    
    /// <summary>
    /// Loads the actual room data through RoomManager based on room index
    /// </summary>
    /// <param name="roomIndex">The index of the room to load (0 = Patio, 1-6 = E-4D rooms)</param>
    private void LoadRoomData(int roomIndex)
    {
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] LoadRoomData called for room index: {roomIndex}");
        
        // Map room indices to their corresponding RoomData assets
        string[] roomAssetPaths = {
            "Assets/Wizio/Rooms/Patio/Patio.asset",           // Index 0 = Patio
            "Assets/Wizio/Rooms/1D/E-1D-C01.asset",          // Index 1 = 1ºD
            "Assets/Wizio/Rooms/1E/E-1E-C01.asset",          // Index 2 = 1ºE
            "Assets/Wizio/Rooms/2A/E-2A-C01.asset",          // Index 3 = 2ºA
            "Assets/Wizio/Rooms/3A/E-3A-C01.asset",          // Index 4 = 3ºA
            "Assets/Wizio/Rooms/Patio/Patio.asset"           // Index 5 = Rooftop (fallback to Patio for now)
        };
        
        if (roomIndex < 0 || roomIndex >= roomAssetPaths.Length)
        {
            Debug.LogWarning($"[XRUIFlowManager] Invalid room index: {roomIndex}. Must be 0-{roomAssetPaths.Length - 1}");
            return;
        }
        
#if UNITY_EDITOR
        // Load the RoomData asset
        RoomData roomData = UnityEditor.AssetDatabase.LoadAssetAtPath<RoomData>(roomAssetPaths[roomIndex]);
        if (roomData == null)
        {
            Debug.LogError($"[XRUIFlowManager] Failed to load RoomData from: {roomAssetPaths[roomIndex]}");
            return;
        }
        
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Loaded RoomData: {roomData.roomName}");
        
        // Call RoomManager to actually load the room
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.TeleportToRoom(roomData);
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Triggered RoomManager.TeleportToRoom for: {roomData.roomName}");
        }
        else
        {
            Debug.LogError("[XRUIFlowManager] RoomManager.Instance is null!");
        }
#else
        Debug.LogWarning("[XRUIFlowManager] LoadRoomData only works in editor mode for asset loading");
#endif
    }

    private Vector3 GetButtonOriginalScale(GameObject button)
    {
        if (button == mapButton) return mapButtonOriginalScale;
        if (button == dayNightButton) return dayNightButtonOriginalScale;
        if (button == currentMapInfoDisplay) return currentMapInfoDisplayOriginalScale;
        if (button == activeRoomInfoDisplay) return activeRoomInfoDisplayOriginalScale;
        
        return Vector3.one; // Default scale
    }
    
    private void SetButtonActiveState(GameObject button, bool active)
    {
        if (button == null) return;
        
        // TODO: Integrate with ButtonAffordance system
        // This would typically involve:
        // 1. Getting ButtonAffordance component from button
        // 2. Setting active state using ButtonAffordance API
        // 3. Applying visual feedback based on UIButtonColorTheme
        
        // For now, just log the state change
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Setting {button.name} active state to {active}");
        
        // Placeholder implementation - replace with actual ButtonAffordance integration
        // ButtonAffordance buttonAffordance = button.GetComponent<ButtonAffordance>();
        // if (buttonAffordance != null)
        // {
        //     buttonAffordance.SetActiveState(active);
        // }
    }
    
    private void SetMapButtonActiveState(int mapIndex, bool active)
    {
        if (mapIndex < 0 || mapIndex >= maps.Length) return;
        
        // Reset all map button active states first
        if (active)
        {
            for (int i = 0; i < maps.Length; i++)
            {
                if (i != mapIndex)
                {
                    // Reset other map buttons to inactive state
                    SetSingleMapButtonActiveState(i, false);
                }
            }
            
            activeMapIndex = mapIndex;
        }
        else if (activeMapIndex == mapIndex)
        {
            activeMapIndex = -1;
        }
        
        // Set the visual active state for this specific button
        SetSingleMapButtonActiveState(mapIndex, active);
        
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Setting Map{mapIndex + 1}Button (Map {mapIndex + 1} - {GetMapName(mapIndex)}) active state to {active}");
    }
    
    #pragma warning disable CS0618 // Type or member is obsolete
    private void SetSingleMapButtonActiveState(int mapIndex, bool active)
    {
        if (mapIndex < 0 || mapIndex >= 6) return;
        
        string mapButtonName = $"Map{mapIndex + 1}Button";
        
        // Set text color for the button (active = black, inactive = original)
        SetMapButtonTextColor(mapIndex, active);
        
        // Try to use cached affordance provider first
        if (mapButtonAffordanceProviders.TryGetValue(mapIndex, out XRInteractableAffordanceStateProvider affordanceProvider) && affordanceProvider != null)
        {
            if (active)
            {
                // Set to selected state to show as active
                var activeState = new AffordanceStateData(AffordanceStateShortcuts.selected, 1.0f);
                affordanceProvider.UpdateAffordanceState(activeState);
                
                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Set {mapButtonName} to selected/active state using cached affordance provider");
            }
            else
            {
                // Refresh to normal state (idle/disabled based on interactable state)
                affordanceProvider.RefreshState();
                
                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Reset {mapButtonName} to normal state using cached affordance provider");
            }
            
            return;
        }
        
        // Fallback: Try to find affordance provider if not cached (in case MapMain was recreated)
        if (mapList != null)
        {
            Transform buttonTransform = FindChildRecursive(mapList.transform, mapButtonName);
            
            if (buttonTransform != null)
            {
                affordanceProvider = buttonTransform.GetComponentInChildren<XRInteractableAffordanceStateProvider>();
                
                if (affordanceProvider != null)
                {
                    // Cache it for future use
                    mapButtonAffordanceProviders[mapIndex] = affordanceProvider;
                    
                    if (active)
                    {
                        var activeState = new AffordanceStateData(AffordanceStateShortcuts.selected, 1.0f);
                        affordanceProvider.UpdateAffordanceState(activeState);
                        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Set {mapButtonName} to selected/active state using found affordance provider");
                    }
                    else
                    {
                        affordanceProvider.RefreshState();
                        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Reset {mapButtonName} to normal state using found affordance provider");
                    }
                    
                    return;
                }
                
                // Final fallback to material manipulation
                MeshRenderer meshRenderer = buttonTransform.GetComponentInChildren<MeshRenderer>();
                
                if (meshRenderer != null)
                {
                    Material material = meshRenderer.material;
                    
                    if (active)
                    {
                        if (material.HasProperty("_EmissionColor"))
                        {
                            material.SetColor("_EmissionColor", Color.cyan * 0.5f);
                            material.EnableKeyword("_EMISSION");
                        }
                        else if (material.HasProperty("_Color"))
                        {
                            material.color = Color.cyan;
                        }
                    }
                    else
                    {
                        if (material.HasProperty("_EmissionColor"))
                        {
                            material.SetColor("_EmissionColor", Color.black);
                            material.DisableKeyword("_EMISSION");
                        }
                        else if (material.HasProperty("_Color"))
                        {
                            material.color = Color.white;
                        }
                    }
                    
                    if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Set {mapButtonName} visual state to {(active ? "active" : "inactive")} using fallback material manipulation");
            return;
                }
            }
        }
        
        if (enableDebugLogs) Debug.LogWarning($"[XRUIFlowManager] Could not set {mapButtonName} state - no affordance provider, button transform, or material found");
    }
    #pragma warning restore CS0618 // Type or member is obsolete
    
    private void UpdateMapInfoText()
    {
        if (mapInfoText == null) return;
        
        if (currentRoomIndex >= 0 && currentRoomIndex < 6)
        {
            string mapName = GetMapName(currentRoomIndex);
            mapInfoText.text = mapName;
            
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Updated MapInfo text to: {mapName}");
        }
        else
        {
            mapInfoText.text = "No Map Selected";
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Reset MapInfo text to default");
        }
    }
    
    private void UpdateRoomInfoText()
    {
        if (roomInfoText == null) return;
        
        // Get current room name from RoomManager
        string roomName = "No Room Active";
        if (RoomManager.Instance != null && RoomManager.Instance.CurrentRoomData != null)
        {
            roomName = RoomManager.Instance.CurrentRoomData.roomName;
        }
        
        roomInfoText.text = roomName;
        
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Updated RoomInfo text to: {roomName}");
    }
    
    private void InitializeDarkLightToggle()
    {
        // Set initial state to light mode
        isLightMode = true;
        isTogglingMode = false;
        
        // Create material instances for sprites to avoid affecting shared materials
        if (iconLight != null && iconLight.material != null)
        {
            iconLightMaterial = new Material(iconLight.material);
            iconLight.material = iconLightMaterial;
            originalIconLightColor = iconLightMaterial.color;
            
            // Set iconLight visible
            Color lightColor = originalIconLightColor;
            lightColor.a = 1.0f;
            iconLightMaterial.color = lightColor;
        }
        
        if (iconDark != null && iconDark.material != null)
        {
            iconDarkMaterial = new Material(iconDark.material);
            iconDark.material = iconDarkMaterial;
            originalIconDarkColor = iconDarkMaterial.color;
            
            // Set iconDark hidden
            Color darkColor = originalIconDarkColor;
            darkColor.a = 0.0f;
            iconDarkMaterial.color = darkColor;
        }
        
        // Create material instance for button mesh and set light mode color
        if (buttonMeshRenderer != null && buttonMeshRenderer.material != null)
        {
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Creating material instance from: {buttonMeshRenderer.material.name}");
            buttonMeshMaterial = new Material(buttonMeshRenderer.material);
            buttonMeshRenderer.material = buttonMeshMaterial;
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Button mesh material instance created: {buttonMeshMaterial.name}");
            
            // Check for Base Map texture that might interfere with color changes
            if (buttonMeshMaterial.HasProperty("_BaseMap"))
            {
                Texture baseMapTexture = buttonMeshMaterial.GetTexture("_BaseMap");
                if (baseMapTexture != null)
                {
                    if (enableDebugLogs) Debug.LogWarning($"[XRUIFlowManager] Base Map texture detected: {baseMapTexture.name}. This will interfere with color changes.");
                    
                    if (autoRemoveBaseMapTexture)
                    {
                        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Auto-removing Base Map texture to allow color tinting...");
                        
                        // Remove the Base Map texture so _BaseColor can work properly
                        buttonMeshMaterial.SetTexture("_BaseMap", null);
                        
                        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Base Map texture removed. Color changes should now work.");
                    }
                    else
                    {
                        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Base Map texture kept. Use 'Remove Base Map Texture' context menu if color changes don't work.");
                    }
                }
            }
            
            // Log expected colors for reference
            if (enableDebugLogs)
            {
                Debug.Log($"[XRUIFlowManager] Light mode color (expected): R={lightModeColor.r:F2} G={lightModeColor.g:F2} B={lightModeColor.b:F2} A={lightModeColor.a:F2}");
                Debug.Log($"[XRUIFlowManager] Dark mode color (expected): R={darkModeColor.r:F2} G={darkModeColor.g:F2} B={darkModeColor.b:F2} A={darkModeColor.a:F2}");
            }
            
            SetButtonMeshColor(lightModeColor); // Use instant color for initialization
        }
        else
        {
            if (enableDebugLogs) Debug.LogError($"[XRUIFlowManager] Cannot create button mesh material - buttonMeshRenderer: {buttonMeshRenderer != null}, material: {buttonMeshRenderer?.material != null}");
        }
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Dark/Light toggle initialized to Light mode with material instances");
    }
    
    private void ToggleDarkLightMode()
    {
        if (isTogglingMode) return;
        
        isTogglingMode = true;
        isLightMode = !isLightMode;
        
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Toggling to {(isLightMode ? "Light" : "Dark")} mode");
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Sprite status - iconLight: {iconLight != null}, iconDark: {iconDark != null}");
        
        // Animate button mesh color based on mode
        Color targetMeshColor = isLightMode ? lightModeColor : darkModeColor;
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Mode is {(isLightMode ? "Light" : "Dark")}, animating to color: R={targetMeshColor.r:F2} G={targetMeshColor.g:F2} B={targetMeshColor.b:F2} A={targetMeshColor.a:F2}");
        AnimateButtonMeshColor(targetMeshColor);
        
        if (iconLight == null || iconDark == null)
        {
            if (enableDebugLogs) Debug.LogError("[XRUIFlowManager] Cannot toggle sprites - one or both icons are null!");
            isTogglingMode = false;
            return;
        }
        
        if (iconLightMaterial == null || iconDarkMaterial == null)
        {
            if (enableDebugLogs) Debug.LogError("[XRUIFlowManager] Cannot toggle sprites - materials not initialized!");
            isTogglingMode = false;
            return;
        }
        
        if (isLightMode)
        {
            // Switching to Light mode: fade out iconDark, fade in iconLight
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Switching to Light mode - fading iconDark out, iconLight in");
            FadeSprite(iconDark, 0f, () => {
                FadeSprite(iconLight, 1f, () => {
                    isTogglingMode = false;
                    if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Light mode transition complete");
                    
                    // Notify NightModeManager of the change
                    NotifyNightModeManager();
                });
            });
        }
        else
        {
            // Switching to Dark mode: fade out iconLight, fade in iconDark
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Switching to Dark mode - fading iconLight out, iconDark in");
            FadeSprite(iconLight, 0f, () => {
                FadeSprite(iconDark, 1f, () => {
                    isTogglingMode = false;
                    if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Dark mode transition complete");
                    
                    // Notify NightModeManager of the change
                    NotifyNightModeManager();
                });
            });
        }
        
        // TODO: Add actual dark/light mode functionality here
        // This could include changing lighting, environment colors, UI themes, etc.
    }
    
    /// <summary>
    /// Notifies the NightModeManager about the current light mode state
    /// </summary>
    private void NotifyNightModeManager()
    {
        Debug.Log($"[XRUIFlowManager] NotifyNightModeManager called - isLightMode: {isLightMode}");
        
        var nightModeManager = FindFirstObjectByType<NightModeManager>();
        if (nightModeManager != null)
        {
            Debug.Log($"[XRUIFlowManager] Found NightModeManager, calling OnUILightModeChanged({isLightMode})");
            nightModeManager.OnUILightModeChanged(isLightMode);
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("[XRUIFlowManager] NightModeManager not found - room night mode won't be updated");
        }
    }
    
    private void FadeSprite(SpriteRenderer sprite, float targetAlpha, System.Action onComplete = null)
    {
        if (sprite == null || sprite.material == null)
        {
            if (enableDebugLogs) Debug.LogWarning("[XRUIFlowManager] FadeSprite called with null sprite or material");
            onComplete?.Invoke();
            return;
        }
        
        Material material = sprite.material;
        Color currentColor = material.color;
        Color targetColor = currentColor;
        targetColor.a = targetAlpha;
        
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Fading {sprite.name} material from α={currentColor.a:F2} to α={targetAlpha:F2}");
        
        material.DOColor(targetColor, toggleFadeDuration)
            .SetEase(toggleFadeEase)
            .OnComplete(() => {
                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Fade complete for {sprite.name}");
                onComplete?.Invoke();
            });
    }
    
    private void AnimateButtonMeshColor(Color targetColor, System.Action onComplete = null)
    {
        if (buttonMeshMaterial == null) 
        {
            onComplete?.Invoke();
            return;
        }
        
        // Get current color
        Color currentColor = buttonMeshMaterial.HasProperty("_BaseColor") ? 
            buttonMeshMaterial.GetColor("_BaseColor") : 
            buttonMeshMaterial.color;
        
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Animating button mesh color from R={currentColor.r:F2} G={currentColor.g:F2} B={currentColor.b:F2} A={currentColor.a:F2}");
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Animating button mesh color to R={targetColor.r:F2} G={targetColor.g:F2} B={targetColor.b:F2} A={targetColor.a:F2}");
        
        // Animate the color change
        float actualDuration = syncColorTransitionWithSprites ? toggleFadeDuration : colorTransitionDuration;
        Ease actualEase = syncColorTransitionWithSprites ? toggleFadeEase : colorTransitionEase;
        
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Using {(syncColorTransitionWithSprites ? "synced" : "independent")} duration: {actualDuration}s with {actualEase} easing");
        
        DOTween.To(() => currentColor, 
                  color => SetButtonMeshColorImmediate(color), 
                  targetColor, 
                  actualDuration)
            .SetEase(actualEase)
            .OnComplete(() =>
            {
                if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Button mesh color animation complete");
                onComplete?.Invoke();
            });
    }
    
    private void SetButtonMeshColorImmediate(Color targetColor)
    {
        if (buttonMeshMaterial == null) return;
        
        // Set color immediately without logging (used during animation)
        if (buttonMeshMaterial.HasProperty("_BaseColor"))
        {
            buttonMeshMaterial.SetColor("_BaseColor", targetColor);
            buttonMeshMaterial.color = targetColor;
        }
        else if (buttonMeshMaterial.HasProperty("_Color"))
        {
            buttonMeshMaterial.color = targetColor;
        }
        else if (buttonMeshMaterial.HasProperty("_MainColor"))
        {
            buttonMeshMaterial.SetColor("_MainColor", targetColor);
        }
        else if (buttonMeshMaterial.HasProperty("_TintColor"))
        {
            buttonMeshMaterial.SetColor("_TintColor", targetColor);
        }
    }
    
    private void SetButtonMeshColor(Color targetColor)
    {
        if (buttonMeshMaterial == null)
        {
            if (enableDebugLogs) Debug.LogError("[XRUIFlowManager] SetButtonMeshColor called but buttonMeshMaterial is null!");
            return;
        }
        
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Setting button mesh color to R={targetColor.r:F2} G={targetColor.g:F2} B={targetColor.b:F2} A={targetColor.a:F2}");
        
        // Log current color before change
        Color currentColor = buttonMeshMaterial.color;
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Current mesh color: R={currentColor.r:F2} G={currentColor.g:F2} B={currentColor.b:F2} A={currentColor.a:F2}");
        
        // Log available material properties
        if (enableDebugLogs)
        {
            Debug.Log($"[XRUIFlowManager] Material '{buttonMeshMaterial.name}' shader: {buttonMeshMaterial.shader.name}");
            Debug.Log($"[XRUIFlowManager] Has _Color: {buttonMeshMaterial.HasProperty("_Color")}");
            Debug.Log($"[XRUIFlowManager] Has _BaseColor: {buttonMeshMaterial.HasProperty("_BaseColor")}");
            Debug.Log($"[XRUIFlowManager] Has _MainColor: {buttonMeshMaterial.HasProperty("_MainColor")}");
            Debug.Log($"[XRUIFlowManager] Has _TintColor: {buttonMeshMaterial.HasProperty("_TintColor")}");
            Debug.Log($"[XRUIFlowManager] Has _BaseMap: {buttonMeshMaterial.HasProperty("_BaseMap")}");
            
            // Check if there's a base map texture that might interfere
            if (buttonMeshMaterial.HasProperty("_BaseMap"))
            {
                Texture baseMap = buttonMeshMaterial.GetTexture("_BaseMap");
                Debug.Log($"[XRUIFlowManager] Base Map texture: {(baseMap != null ? baseMap.name : "None")}");
            }
            
            // Check current _BaseColor value
            if (buttonMeshMaterial.HasProperty("_BaseColor"))
            {
                Color currentBaseColor = buttonMeshMaterial.GetColor("_BaseColor");
                Debug.Log($"[XRUIFlowManager] Current _BaseColor before change: R={currentBaseColor.r:F2} G={currentBaseColor.g:F2} B={currentBaseColor.b:F2} A={currentBaseColor.a:F2}");
            }
            
            // Check surface settings that might affect transparency
            if (buttonMeshMaterial.HasProperty("_Surface"))
            {
                float surfaceType = buttonMeshMaterial.GetFloat("_Surface");
                Debug.Log($"[XRUIFlowManager] Surface Type: {(surfaceType == 0 ? "Opaque" : "Transparent")}");
            }
            
            if (buttonMeshMaterial.HasProperty("_Blend"))
            {
                float blendMode = buttonMeshMaterial.GetFloat("_Blend");
                Debug.Log($"[XRUIFlowManager] Blend Mode: {blendMode}");
            }
            
            if (buttonMeshMaterial.HasProperty("_AlphaClip"))
            {
                float alphaClip = buttonMeshMaterial.GetFloat("_AlphaClip");
                Debug.Log($"[XRUIFlowManager] Alpha Clipping: {(alphaClip == 1 ? "Enabled" : "Disabled")}");
            }
        }
        
        bool colorSet = false;
        
        // Try different material properties depending on shader type
        // For URP/Lit shaders, prioritize _BaseColor over _Color
        if (buttonMeshMaterial.HasProperty("_BaseColor"))
        {
            // Check if there's a Base Map texture that might interfere
            if (buttonMeshMaterial.HasProperty("_BaseMap"))
            {
                Texture baseMapTexture = buttonMeshMaterial.GetTexture("_BaseMap");
                if (baseMapTexture != null)
                {
                    if (enableDebugLogs) Debug.LogWarning($"[XRUIFlowManager] Base Map texture detected: {baseMapTexture.name}. This may interfere with color changes.");
                    if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Consider using 'Remove Base Map Texture' or 'Create White Base Map' context menu options.");
                }
            }
            
            // Set the color using multiple approaches to ensure it takes effect
            buttonMeshMaterial.SetColor("_BaseColor", targetColor);
            
            // Also try setting the main color property as backup
            buttonMeshMaterial.color = targetColor;
            
            // Force material to update its properties
            buttonMeshMaterial.SetFloat("_Surface", buttonMeshMaterial.GetFloat("_Surface"));
            
            colorSet = true;
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Used _BaseColor property (URP primary) + forced update");
            
            // Immediately verify the color was set
            Color verifyColor = buttonMeshMaterial.GetColor("_BaseColor");
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Verification - _BaseColor now: R={verifyColor.r:F2} G={verifyColor.g:F2} B={verifyColor.b:F2} A={verifyColor.a:F2}");
        }
        else if (buttonMeshMaterial.HasProperty("_Color"))
        {
            buttonMeshMaterial.color = targetColor;
            colorSet = true;
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Used _Color property (Legacy)");
        }
        else if (buttonMeshMaterial.HasProperty("_MainColor"))
        {
            buttonMeshMaterial.SetColor("_MainColor", targetColor);
            colorSet = true;
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Used _MainColor property");
        }
        else if (buttonMeshMaterial.HasProperty("_TintColor"))
        {
            buttonMeshMaterial.SetColor("_TintColor", targetColor);
            colorSet = true;
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Used _TintColor property");
        }
        
        if (!colorSet)
        {
            if (enableDebugLogs) Debug.LogError("[XRUIFlowManager] No compatible color property found on material!");
        }
        else
        {
            // Log color after change to verify it was set
            Color newColor;
            if (buttonMeshMaterial.HasProperty("_BaseColor"))
            {
                newColor = buttonMeshMaterial.GetColor("_BaseColor");
                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] New _BaseColor after change: R={newColor.r:F2} G={newColor.g:F2} B={newColor.b:F2} A={newColor.a:F2}");
            }
            else
            {
                newColor = buttonMeshMaterial.color;
                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] New _Color after change: R={newColor.r:F2} G={newColor.g:F2} B={newColor.b:F2} A={newColor.a:F2}");
            }
        }
    }
    
    private void ResetAllButtonActiveStates()
    {
        // Reset main menu button states
        SetButtonActiveState(mapButton, false);
        SetButtonActiveState(dayNightButton, false);
        // Note: currentMapInfoDisplay and activeRoomInfoDisplay are non-interactive
        // so they don't have active states to reset
        
        // Reset map button states
        for (int i = 0; i < maps.Length; i++)
        {
            SetMapButtonActiveState(i, false);
        }
        
        // Reset all map button text colors to original
        ResetAllMapButtonTextColors();
        
        activeMapIndex = -1;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] All button active states and text colors reset");
    }
    
    private void RestoreAllButtonStates()
    {
        // First reset all button states
        for (int i = 0; i < 6; i++)
        {
            SetSingleMapButtonActiveState(i, false);
        }
        
        // Then set the current active button
        if (currentRoomIndex >= 0 && currentRoomIndex < 6)
        {
            activeMapIndex = currentRoomIndex;
            SetSingleMapButtonActiveState(currentRoomIndex, true);
            
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Restored button states - {GetMapName(currentRoomIndex)} button is active");
        }
        else
        {
            activeMapIndex = -1;
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] No active button to restore");
        }
        
        // Update MapInfo text to reflect current selection
        UpdateMapInfoText();
    }
    
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
            
            Transform result = FindChildRecursive(child, childName);
            if (result != null)
                return result;
        }
        return null;
    }
    
    private void ListAllChildren(Transform parent, int depth)
    {
        string indent = new string(' ', depth * 2);
        SpriteRenderer sr = parent.GetComponent<SpriteRenderer>();
        MeshRenderer mr = parent.GetComponent<MeshRenderer>();
        
        string info = "";
        if (sr != null)
        {
            Color srColor = sr.material != null ? sr.material.color : sr.color;
            info += $" [SpriteRenderer: α={srColor.a:F2}]";
        }
        if (mr != null)
        {
            Color mrColor = mr.material != null ? mr.material.color : Color.white;
            info += $" [MeshRenderer: R={mrColor.r:F2} G={mrColor.g:F2} B={mrColor.b:F2} A={mrColor.a:F2}]";
        }
        
        if (enableDebugLogs) Debug.Log($"{indent}- {parent.name}{info}");
        
        foreach (Transform child in parent)
        {
            ListAllChildren(child, depth + 1);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up DOTween animations more comprehensively
        DOTween.Kill(this);
        
        // Kill specific object animations
        if (mapMain != null) DOTween.Kill(mapMain.transform);
        foreach (var button in mainMenuButtons)
        {
            if (button != null) DOTween.Kill(button.transform);
        }
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i] != null) DOTween.Kill(maps[i].transform);
        }
        
        // Kill material animations
        if (iconLightMaterial != null) DOTween.Kill(iconLightMaterial);
        if (iconDarkMaterial != null) DOTween.Kill(iconDarkMaterial);
        if (buttonMeshMaterial != null) DOTween.Kill(buttonMeshMaterial);
        
        // Clean up material instances to prevent memory leaks
        if (iconLightMaterial != null)
        {
            DestroyImmediate(iconLightMaterial);
        }
        if (iconDarkMaterial != null)
        {
            DestroyImmediate(iconDarkMaterial);
        }
        if (buttonMeshMaterial != null)
        {
            DestroyImmediate(buttonMeshMaterial);
        }
    }
    
    // Debug/Test methods
    [ContextMenu("Test Start Interaction")]
    public void TestStartInteraction()
    {
        OnStartSelected(null);
    }
    
    [ContextMenu("Test Direct to MapMain")]
    public void TestDirectToMapMain()
    {
        currentRoomIndex = 0; // Patio
        ShowMapView();
    }
    
    [ContextMenu("Test Map Interaction")]
    public void TestMapInteraction()
    {
        OnMapSelected(null);
    }
    
    [ContextMenu("Test Room 1 Selection")]
    public void TestRoom1Selection()
    {
        OnRoomSelected(0);
    }
    
    [ContextMenu("Test Close Interaction")]
    public void TestCloseInteraction()
    {
        OnCloseSelected(null);
    }
    
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log($"[XRUIFlowManager] === DEBUG STATE ===");
        Debug.Log($"Current UI State: {currentState}");
        Debug.Log($"Is Transitioning: {isTransitioning}");
        Debug.Log($"Current Room Index: {currentRoomIndex}");
        Debug.Log($"Active Map Index: {activeMapIndex}");
        Debug.Log($"MapMain Active: {mapMain != null && mapMain.activeInHierarchy}");
        Debug.Log($"MapList Active: {mapList != null && mapList.activeInHierarchy}");
        Debug.Log($"CloseButton Active: {closeButton != null && closeButton.activeInHierarchy}");
        
        // Debug room info
        if (RoomManager.Instance != null && RoomManager.Instance.CurrentRoomData != null)
        {
            Debug.Log($"RoomManager Current Room: {RoomManager.Instance.CurrentRoomData.roomName}");
        }
        else
        {
            Debug.Log("RoomManager Current Room: None");
        }
        
        // Debug dark/light mode info
        Debug.Log($"Current Mode: {(isLightMode ? "Light" : "Dark")} | Is Toggling: {isTogglingMode}");
        
        if (maps != null)
        {
            for (int i = 0; i < maps.Length; i++)
            {
                if (maps[i] != null)
                {
                    Debug.Log($"Map {i + 1} ({GetMapName(i)}) Active: {maps[i].activeInHierarchy}");
                }
            }
        }
        
        Debug.Log($"[XRUIFlowManager] === END DEBUG ===");
    }
    
    [ContextMenu("Update Room Info Text")]
    public void ForceUpdateRoomInfoText()
    {
        UpdateRoomInfoText();
    }
    
    [ContextMenu("Test Dark/Light Toggle")]
    public void TestDarkLightToggle()
    {
        ToggleDarkLightMode();
    }
    
    [ContextMenu("Reset to Light Mode")]
    public void ResetToLightMode()
    {
        InitializeDarkLightToggle();
    }
    
    [ContextMenu("Force Light Mode Color")]
    public void ForceLightModeColor()
    {
        SetButtonMeshColor(lightModeColor);
    }
    
    [ContextMenu("Force Dark Mode Color")]
    public void ForceDarkModeColor()
    {
        SetButtonMeshColor(darkModeColor);
    }
    
    [ContextMenu("Test Extreme Colors")]
    public void TestExtremeColors()
    {
        // Use bright red and bright blue for testing
        Color testRed = new Color(1f, 0f, 0f, 1f);
        Color testBlue = new Color(0f, 0f, 1f, 1f);
        
        Debug.Log("[XRUIFlowManager] Testing with extreme colors - setting to bright red");
        SetButtonMeshColor(testRed);
        
        // Schedule blue color change after 2 seconds
        StartCoroutine(TestColorChangeCoroutine(testBlue));
    }
    
    [ContextMenu("Direct Set Base Color Red")]
    public void DirectSetBaseColorRed()
    {
        if (buttonMeshMaterial != null)
        {
            Color brightRed = new Color(1f, 0f, 0f, 1f);
            Debug.Log("[XRUIFlowManager] DIRECT: Setting _BaseColor to bright red");
            buttonMeshMaterial.SetColor("_BaseColor", brightRed);
            
            // Force renderer to update
            if (buttonMeshRenderer != null)
            {
                buttonMeshRenderer.material = buttonMeshMaterial;
            }
            
            Debug.Log("[XRUIFlowManager] DIRECT: Color set and renderer updated");
        }
    }
    
    [ContextMenu("Direct Set Base Color Blue")]
    public void DirectSetBaseColorBlue()
    {
        if (buttonMeshMaterial != null)
        {
            Color brightBlue = new Color(0f, 0f, 1f, 1f);
            Debug.Log("[XRUIFlowManager] DIRECT: Setting _BaseColor to bright blue");
            buttonMeshMaterial.SetColor("_BaseColor", brightBlue);
            
            // Force renderer to update
            if (buttonMeshRenderer != null)
            {
                buttonMeshRenderer.material = buttonMeshMaterial;
            }
            
            Debug.Log("[XRUIFlowManager] DIRECT: Color set and renderer updated");
        }
    }
    
    [ContextMenu("Test Full Alpha Colors")]
    public void TestFullAlphaColors()
    {
        if (buttonMeshMaterial != null)
        {
            // Test with full alpha versions of the light/dark colors
            Color lightModeFullAlpha = new Color(lightModeColor.r, lightModeColor.g, lightModeColor.b, 1f);
            Color darkModeFullAlpha = new Color(darkModeColor.r, darkModeColor.g, darkModeColor.b, 1f);
            
            Debug.Log($"[XRUIFlowManager] Testing with full alpha - Light: R={lightModeFullAlpha.r:F2} G={lightModeFullAlpha.g:F2} B={lightModeFullAlpha.b:F2} A={lightModeFullAlpha.a:F2}");
            Debug.Log($"[XRUIFlowManager] Testing with full alpha - Dark: R={darkModeFullAlpha.r:F2} G={darkModeFullAlpha.g:F2} B={darkModeFullAlpha.b:F2} A={darkModeFullAlpha.a:F2}");
            
            // Set to light mode with full alpha
            buttonMeshMaterial.SetColor("_BaseColor", lightModeFullAlpha);
            
            // Schedule dark mode change after 2 seconds
            StartCoroutine(TestFullAlphaColorChangeCoroutine(darkModeFullAlpha, lightModeFullAlpha));
        }
    }
    
    [ContextMenu("Remove Base Map Texture")]
    public void RemoveBaseMapTexture()
    {
        if (buttonMeshMaterial != null)
        {
            Debug.Log("[XRUIFlowManager] Removing Base Map texture to allow color tinting");
            buttonMeshMaterial.SetTexture("_BaseMap", null);
            
            // Set to current mode color
            SetButtonMeshColor(isLightMode ? lightModeColor : darkModeColor);
        }
    }
    
    [ContextMenu("Create White Base Map")]
    public void CreateWhiteBaseMap()
    {
        if (buttonMeshMaterial != null)
        {
            Debug.Log("[XRUIFlowManager] Creating white texture for Base Map to allow color tinting");
            
            // Create a simple white texture
            Texture2D whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
            
            buttonMeshMaterial.SetTexture("_BaseMap", whiteTexture);
            
            // Set to current mode color
            SetButtonMeshColor(isLightMode ? lightModeColor : darkModeColor);
        }
    }
    
    [ContextMenu("Toggle Auto Remove Base Map")]
    public void ToggleAutoRemoveBaseMap()
    {
        autoRemoveBaseMapTexture = !autoRemoveBaseMapTexture;
        Debug.Log($"[XRUIFlowManager] Auto Remove Base Map Texture: {(autoRemoveBaseMapTexture ? "ENABLED" : "DISABLED")}");
        
        if (autoRemoveBaseMapTexture)
        {
            Debug.Log("[XRUIFlowManager] Will auto-remove Base Map texture on next initialization");
        }
        else
        {
            Debug.Log("[XRUIFlowManager] Will keep Base Map texture - use manual context menu options if needed");
        }
    }
    
    [ContextMenu("Test Animated Color Transition")]
    public void TestAnimatedColorTransition()
    {
        Debug.Log("[XRUIFlowManager] Testing animated color transition - Light to Dark");
        StartCoroutine(TestColorTransitionCoroutine());
    }
    
    private System.Collections.IEnumerator TestColorTransitionCoroutine()
    {
        AnimateButtonMeshColor(darkModeColor);
        yield return new WaitForSeconds(colorTransitionDuration + 0.5f);
        
        Debug.Log("[XRUIFlowManager] First transition complete, now Dark to Light");
        AnimateButtonMeshColor(lightModeColor);
        yield return new WaitForSeconds(colorTransitionDuration + 0.5f);
        
        Debug.Log("[XRUIFlowManager] Color transition test complete");
    }
    
    [ContextMenu("Test Fast Color Transition")]
    public void TestFastColorTransition()
    {
        float originalDuration = colorTransitionDuration;
        colorTransitionDuration = 0.2f;
        Debug.Log("[XRUIFlowManager] Testing fast color transition (0.2s)");
        
        AnimateButtonMeshColor(isLightMode ? darkModeColor : lightModeColor, () => 
        {
            colorTransitionDuration = originalDuration;
            Debug.Log($"[XRUIFlowManager] Fast transition complete, duration restored to {originalDuration}s");
        });
    }
    
    [ContextMenu("Test Slow Color Transition")]
    public void TestSlowColorTransition()
    {
        float originalDuration = colorTransitionDuration;
        colorTransitionDuration = 2.0f;
        Debug.Log("[XRUIFlowManager] Testing slow color transition (2.0s)");
        
        AnimateButtonMeshColor(isLightMode ? darkModeColor : lightModeColor, () => 
        {
            colorTransitionDuration = originalDuration;
            Debug.Log($"[XRUIFlowManager] Slow transition complete, duration restored to {originalDuration}s");
        });
    }
    
    [ContextMenu("Test Synchronized Transitions")]
    public void TestSynchronizedTransitions()
    {
        Debug.Log("[XRUIFlowManager] Testing synchronized sprite and button color transitions");
        
        // Temporarily sync the durations
        float originalColorDuration = colorTransitionDuration;
        colorTransitionDuration = toggleFadeDuration;
        
        if (isLightMode)
        {
            // Switch to dark mode
            AnimateButtonMeshColor(darkModeColor);
            if (iconLight != null) FadeSprite(iconLight, 0f);
            if (iconDark != null) FadeSprite(iconDark, 1f, () => 
            {
                colorTransitionDuration = originalColorDuration;
                Debug.Log("[XRUIFlowManager] Synchronized transition to dark mode complete");
            });
        }
        else
        {
            // Switch to light mode
            AnimateButtonMeshColor(lightModeColor);
            if (iconDark != null) FadeSprite(iconDark, 0f);
            if (iconLight != null) FadeSprite(iconLight, 1f, () => 
            {
                colorTransitionDuration = originalColorDuration;
                Debug.Log("[XRUIFlowManager] Synchronized transition to light mode complete");
            });
        }
        
        // Toggle the mode state for next test
        isLightMode = !isLightMode;
    }
    
    [ContextMenu("Toggle Sync Color With Sprites")]
    public void ToggleSyncColorWithSprites()
    {
        syncColorTransitionWithSprites = !syncColorTransitionWithSprites;
        Debug.Log($"[XRUIFlowManager] Sync Color Transition With Sprites: {(syncColorTransitionWithSprites ? "ENABLED" : "DISABLED")}");
        
        if (syncColorTransitionWithSprites)
        {
            Debug.Log($"[XRUIFlowManager] Button color will animate with sprite fade duration ({toggleFadeDuration}s) and easing ({toggleFadeEase})");
        }
        else
        {
            Debug.Log($"[XRUIFlowManager] Button color will use independent duration ({colorTransitionDuration}s) and easing ({colorTransitionEase})");
        }
    }
    
    private System.Collections.IEnumerator TestFullAlphaColorChangeCoroutine(Color darkColor, Color lightColor)
    {
        yield return new WaitForSeconds(2f);
        Debug.Log("[XRUIFlowManager] Changing to dark mode with full alpha");
        buttonMeshMaterial.SetColor("_BaseColor", darkColor);
        
        yield return new WaitForSeconds(2f);
        Debug.Log("[XRUIFlowManager] Changing back to light mode with full alpha");
        buttonMeshMaterial.SetColor("_BaseColor", lightColor);
        
        yield return new WaitForSeconds(2f);
        Debug.Log("[XRUIFlowManager] Restoring original alpha values");
        SetButtonMeshColor(isLightMode ? lightModeColor : darkModeColor);
    }
    
    private System.Collections.IEnumerator TestColorChangeCoroutine(Color targetColor)
    {
        yield return new WaitForSeconds(2f);
        Debug.Log("[XRUIFlowManager] Changing to bright blue");
        SetButtonMeshColor(targetColor);
        
        yield return new WaitForSeconds(2f);
        Debug.Log("[XRUIFlowManager] Restoring original color");
        SetButtonMeshColor(isLightMode ? lightModeColor : darkModeColor);
    }
    
    [ContextMenu("Check Button Material Color")]
    public void CheckButtonMaterialColor()
    {
        if (buttonMeshMaterial != null)
        {
            Debug.Log($"[XRUIFlowManager] === Button Material Color Status ===");
            Debug.Log($"[XRUIFlowManager] Current toggle state: {(isLightMode ? "Light" : "Dark")} mode");
            Debug.Log($"[XRUIFlowManager] Expected Light: R={lightModeColor.r:F2} G={lightModeColor.g:F2} B={lightModeColor.b:F2} A={lightModeColor.a:F2}");
            Debug.Log($"[XRUIFlowManager] Expected Dark: R={darkModeColor.r:F2} G={darkModeColor.g:F2} B={darkModeColor.b:F2} A={darkModeColor.a:F2}");
            
            if (buttonMeshMaterial.HasProperty("_BaseColor"))
            {
                Color baseColor = buttonMeshMaterial.GetColor("_BaseColor");
                Debug.Log($"[XRUIFlowManager] Actual _BaseColor: R={baseColor.r:F2} G={baseColor.g:F2} B={baseColor.b:F2} A={baseColor.a:F2}");
                
                // Check if it matches expected color
                Color expected = isLightMode ? lightModeColor : darkModeColor;
                bool matches = Mathf.Approximately(baseColor.r, expected.r) && 
                              Mathf.Approximately(baseColor.g, expected.g) && 
                              Mathf.Approximately(baseColor.b, expected.b) && 
                              Mathf.Approximately(baseColor.a, expected.a);
                Debug.Log($"[XRUIFlowManager] Color matches expected: {matches}");
            }
            if (buttonMeshMaterial.HasProperty("_Color"))
            {
                Color color = buttonMeshMaterial.color;
                Debug.Log($"[XRUIFlowManager] Actual _Color: R={color.r:F2} G={color.g:F2} B={color.b:F2} A={color.a:F2}");
            }
        }
        else
        {
            Debug.LogError("[XRUIFlowManager] buttonMeshMaterial is null!");
        }
    }
    
    [ContextMenu("Debug Sprite Status")]
    public void DebugSpriteStatus()
    {
        Debug.Log($"[XRUIFlowManager] === SPRITE DEBUG ===");
        Debug.Log($"dayNightButton: {dayNightButton != null}");
        Debug.Log($"iconLight: {iconLight != null}");
        Debug.Log($"iconDark: {iconDark != null}");
        Debug.Log($"buttonMeshRenderer: {buttonMeshRenderer != null}");
        
        Debug.Log($"iconLightMaterial: {iconLightMaterial != null}");
        Debug.Log($"iconDarkMaterial: {iconDarkMaterial != null}");
        Debug.Log($"buttonMeshMaterial: {buttonMeshMaterial != null}");
        
        if (iconLight != null && iconLightMaterial != null)
        {
            Debug.Log($"iconLight.name: {iconLight.name}, material alpha: {iconLightMaterial.color.a:F2}, active: {iconLight.gameObject.activeInHierarchy}");
        }
        
        if (iconDark != null && iconDarkMaterial != null)
        {
            Debug.Log($"iconDark.name: {iconDark.name}, material alpha: {iconDarkMaterial.color.a:F2}, active: {iconDark.gameObject.activeInHierarchy}");
        }
        
        if (buttonMeshRenderer != null && buttonMeshMaterial != null)
        {
            Color meshColor = buttonMeshMaterial.color;
            Debug.Log($"buttonMesh color: R={meshColor.r:F2} G={meshColor.g:F2} B={meshColor.b:F2} A={meshColor.a:F2}");
        }
        
        Debug.Log($"Current mode: {(isLightMode ? "Light" : "Dark")}, isTogglingMode: {isTogglingMode}");
        Debug.Log($"lightModeColor: R={lightModeColor.r:F2} G={lightModeColor.g:F2} B={lightModeColor.b:F2} A={lightModeColor.a:F2}");
        Debug.Log($"darkModeColor: R={darkModeColor.r:F2} G={darkModeColor.g:F2} B={darkModeColor.b:F2} A={darkModeColor.a:F2}");
        Debug.Log($"[XRUIFlowManager] === END SPRITE DEBUG ===");
    }
    
    [ContextMenu("Debug Map Button Text Colors")]
    public void DebugMapButtonTextColors()
    {
        Debug.Log($"[XRUIFlowManager] === MAP BUTTON TEXT COLORS DEBUG ===");
        Debug.Log($"Active text color: R={activeMapButtonTextColor.r:F2} G={activeMapButtonTextColor.g:F2} B={activeMapButtonTextColor.b:F2} A={activeMapButtonTextColor.a:F2}");
        
        for (int i = 0; i < mapButtonTexts.Length; i++)
        {
            if (mapButtonTexts[i] != null)
            {
                Color currentColor = mapButtonTexts[i].color;
                Color originalColor = originalMapButtonTextColors[i];
                string mapName = GetMapName(i);
                
                Debug.Log($"{mapName} text - Current: R={currentColor.r:F2} G={currentColor.g:F2} B={currentColor.b:F2} A={currentColor.a:F2}");
                Debug.Log($"{mapName} text - Original: R={originalColor.r:F2} G={originalColor.g:F2} B={originalColor.b:F2} A={originalColor.a:F2}");
                
                bool isActive = Mathf.Approximately(currentColor.r, activeMapButtonTextColor.r) && 
                               Mathf.Approximately(currentColor.g, activeMapButtonTextColor.g) && 
                               Mathf.Approximately(currentColor.b, activeMapButtonTextColor.b) && 
                               Mathf.Approximately(currentColor.a, activeMapButtonTextColor.a);
                Debug.Log($"{mapName} text - Status: {(isActive ? "ACTIVE" : "INACTIVE")}");
            }
            else
            {
                Debug.Log($"Map{i + 1} text component is null!");
            }
        }
        
        Debug.Log($"Current active map index: {activeMapIndex}");
        Debug.Log($"[XRUIFlowManager] === END MAP BUTTON TEXT COLORS DEBUG ===");
    }
    
    [ContextMenu("Test Map Button Text Colors")]
    public void TestMapButtonTextColors()
    {
        Debug.Log("[XRUIFlowManager] Testing map button text colors - setting Patio to active");
        SetMapButtonTextColor(0, true); // Set Patio to active (black)
        
        StartCoroutine(TestMapButtonTextColorCoroutine());
    }
    
    [ContextMenu("Test Fast Clicking Protection")]
    public void TestFastClickingProtection()
    {
        Debug.Log("[XRUIFlowManager] Testing fast clicking protection - simulating rapid button presses");
        StartCoroutine(TestFastClickingCoroutine());
    }
    
    private System.Collections.IEnumerator TestFastClickingCoroutine()
    {
        Debug.Log("[XRUIFlowManager] Simulating 5 rapid button clicks within 1 second");
        
        for (int i = 0; i < 5; i++)
        {
            bool canInteract = CanInteract($"TestClick{i + 1}");
            if (canInteract)
            {
                RegisterInteraction($"TestClick{i + 1}");
                Debug.Log($"[XRUIFlowManager] Test click {i + 1} - ALLOWED");
            }
            else
            {
                Debug.Log($"[XRUIFlowManager] Test click {i + 1} - BLOCKED");
            }
            
            yield return new WaitForSeconds(0.2f); // Click every 200ms
        }
        
        Debug.Log("[XRUIFlowManager] Fast clicking test complete");
    }
    
    [ContextMenu("Check Interaction Status")]
    public void CheckInteractionStatus()
    {
        float currentTime = Time.time;
        float timeSinceLastInteraction = currentTime - lastInteractionTime;
        
        Debug.Log($"[XRUIFlowManager] === INTERACTION STATUS ===");
        Debug.Log($"[XRUIFlowManager] Is Transitioning: {isTransitioning}");
        Debug.Log($"[XRUIFlowManager] Is Toggling Mode: {isTogglingMode}");
        Debug.Log($"[XRUIFlowManager] Current Time: {currentTime:F2}");
        Debug.Log($"[XRUIFlowManager] Last Interaction Time: {lastInteractionTime:F2}");
        Debug.Log($"[XRUIFlowManager] Time Since Last Interaction: {timeSinceLastInteraction:F2}s");
        Debug.Log($"[XRUIFlowManager] Debounce Time: {interactionDebounceTime:F2}s");
        Debug.Log($"[XRUIFlowManager] Can Interact: {CanInteract("StatusCheck")}");
        Debug.Log($"[XRUIFlowManager] Current State: {currentState}");
        Debug.Log($"[XRUIFlowManager] Current Room Index: {currentRoomIndex}");
        Debug.Log($"[XRUIFlowManager] Active Map Index: {activeMapIndex}");
        Debug.Log($"[XRUIFlowManager] === END INTERACTION STATUS ===");
    }
    
    [ContextMenu("Test Rapid Room Switching")]
    public void TestRapidRoomSwitching()
    {
        Debug.Log("[XRUIFlowManager] Testing rapid room switching - simulating 6 rapid map clicks");
        StartCoroutine(TestRapidRoomSwitchingCoroutine());
    }
    
    private System.Collections.IEnumerator TestRapidRoomSwitchingCoroutine()
    {
        Debug.Log("[XRUIFlowManager] Simulating rapid switching between all 6 map buttons");
        
        for (int i = 0; i < 6; i++)
        {
            Debug.Log($"[XRUIFlowManager] Attempting to select room {i + 1} ({GetMapName(i)})");
            OnRoomSelected(i);
            yield return new WaitForSeconds(0.15f); // Switch every 150ms
        }
        
        Debug.Log("[XRUIFlowManager] Rapid room switching test complete");
    }
    
    [ContextMenu("Kill All UI Animations")]
    public void KillAllUIAnimations()
    {
        Debug.Log("[XRUIFlowManager] Killing all UI animations manually");
        
        // Kill all specific animations
        KillSpecificAnimations("Emergency");
        
        // Reset transition state
        isTransitioning = false;
        isTogglingMode = false;
        
        Debug.Log("[XRUIFlowManager] All animations killed and state reset");
    }
    
    [ContextMenu("Test Basic UI Flow")]
    public void TestBasicUIFlow()
    {
        Debug.Log("[XRUIFlowManager] Testing basic UI flow: Start → MainMenu → MapMain → Back");
        StartCoroutine(TestBasicUIFlowCoroutine());
    }
    
    private System.Collections.IEnumerator TestBasicUIFlowCoroutine()
    {
        Debug.Log("[XRUIFlowManager] Step 1: Go to MainMenu");
        ShowMainMenu();
        yield return new WaitForSeconds(2f);
        
        Debug.Log("[XRUIFlowManager] Step 2: Go to MapView");
        ShowMapView();
        yield return new WaitForSeconds(2f);
        
        Debug.Log("[XRUIFlowManager] Step 3: Go back to MainMenu");
        HideMapView();
        yield return new WaitForSeconds(2f);
        
        Debug.Log("[XRUIFlowManager] Basic UI flow test complete");
    }
    
    private System.Collections.IEnumerator TestMapButtonTextColorCoroutine()
    {
        yield return new WaitForSeconds(2f);
        Debug.Log("[XRUIFlowManager] Setting 2ºA to active, Patio to inactive");
        SetMapButtonTextColor(0, false); // Set Patio to inactive
        SetMapButtonTextColor(3, true);  // Set 2ºA to active
        
        yield return new WaitForSeconds(2f);
        Debug.Log("[XRUIFlowManager] Resetting all map button text colors");
        ResetAllMapButtonTextColors();
    }
    
    // Public methods for external access
    public void SetAnimationDuration(float duration)
    {
        scaleAnimationDuration = duration;
    }
    
    public void OnRoomChanged()
    {
        // Called by RoomManager when the current room changes
        UpdateRoomInfoText();
    }
    
    public UIState GetCurrentState() => currentState;
    public int GetCurrentRoomIndex() => currentRoomIndex;
    public int GetActiveMapIndex() => activeMapIndex;
    public bool IsLightMode() => isLightMode;
    public bool IsTogglingMode() => isTogglingMode;
    
    // Public method for external components (like RoomTeleportButton) to close the map view
    public void CloseMapView()
    {
        if (!CanInteract("ExternalClose")) return;
        
        RegisterInteraction("ExternalClose");
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] CloseMapView called externally (e.g., from teleport button)");
        
        // Reset all button active states
        ResetAllButtonActiveStates();
        
        HideMapView();
    }
} 