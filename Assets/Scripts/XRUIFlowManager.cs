using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.State;
using TMPro;
using DG.Tweening;

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
    
    private Vector3 mapButtonOriginalScale;
    private Vector3 dayNightButtonOriginalScale;
    private Vector3 currentMapInfoDisplayOriginalScale;
    private Vector3 activeRoomInfoDisplayOriginalScale;
    private Vector3 mapMainOriginalScale;
    private Vector3 mapListOriginalScale;
    private Vector3 closeButtonOriginalScale;
    private Vector3[] roomMapOriginalScales = new Vector3[6];
    private bool isTransitioning = false;
    
    // Current UI state
    public enum UIState { Start, MainMenu, MapView }
    private UIState currentState = UIState.Start;
    private int currentRoomIndex = -1; // Remembers which room user was in
    private int activeMapIndex = -1; // Currently active map button
    
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
        
        currentState = UIState.Start;
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] UI initialized with Patio as default room");
    }
    
    private void OnStartSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (isTransitioning) return;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Start button selected - going to MainMenu");
        
        // Set Patio as default room for when user eventually goes to MapMain
        currentRoomIndex = 0; // Set Patio as default
        ShowMainMenu();
    }
    
    private void OnMapSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (isTransitioning) return;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Map button selected - starting ShowMapView");
        
        SetButtonActiveState(mapButton, true);
        ShowMapView();
    }
    
    private void OnDayNightSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (isTransitioning) return;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Day/Night button selected");
        
        SetButtonActiveState(dayNightButton, true);
        // TODO: Implement day/night cycle logic here
    }
    
    // Note: OnCurrentMapInfoSelected and OnActiveRoomInfoSelected methods removed
    // as these are now non-interactive display elements
    
    private void OnRoomSelected(int roomIndex)
    {
        if (isTransitioning || roomIndex < 0 || roomIndex >= maps.Length) 
        {
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Room selection blocked - transitioning: {isTransitioning}, index: {roomIndex}");
            return;
        }
        
        if (maps[roomIndex] == null) 
        {
            if (enableDebugLogs) Debug.LogError($"[XRUIFlowManager] Room {roomIndex + 1} map is null!");
            return;
        }
        
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Room {roomIndex + 1} selected - changing from room {currentRoomIndex} to {roomIndex}");
        
        isTransitioning = true;
        
        // Store the old room index before updating
        int oldRoomIndex = currentRoomIndex;
        
        // Update current room index to new room
        currentRoomIndex = roomIndex;
        
        // Update MapInfo text to show selected map name
        UpdateMapInfoText();
        
        // Set active state for the selected map button (this will reset others and set this one active)
        SetMapButtonActiveState(roomIndex, true);
        
        // Hide old room and show selected room
        HideSpecificRoom(oldRoomIndex, () => {
            if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Old room {oldRoomIndex + 1} hidden, now showing room {roomIndex + 1}");
            ShowRoom(roomIndex);
        });
    }
    
    private void OnCloseSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (isTransitioning) 
        {
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Close button blocked - already transitioning");
            return;
        }
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Close button selected - closing map view");
        
        // Reset all button active states
        ResetAllButtonActiveStates();
        
        HideMapView();
    }
    
    private void ShowMainMenu()
    {
        if (isTransitioning) return;
        
        isTransitioning = true;
        currentState = UIState.MainMenu;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Showing main menu");
        
        // Show all main menu buttons with staggered animation
        ShowAllMainMenuButtons(() => {
            isTransitioning = false;
        });
    }
    
    private void ShowMapView()
    {
        if (isTransitioning) 
        {
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] ShowMapView blocked - already transitioning");
            return;
        }
        
        isTransitioning = true;
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
        if (isTransitioning) 
        {
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] HideMapView blocked - already transitioning");
            return;
        }
        
        isTransitioning = true;
        currentState = UIState.MainMenu;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Hiding map view - starting transition back to main menu");
        
        // Hide map main, then show main menu buttons
        HideMapMain(() => {
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] MapMain hidden - now showing main menu buttons");
            ShowAllMainMenuButtons(() => {
                isTransitioning = false;
                if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Main menu buttons shown - transition complete");
            });
        });
    }
    
    private void ShowAllMainMenuButtons(System.Action onComplete = null)
    {
        int buttonsToShow = 0;
        int buttonsShown = 0;
        
        foreach (var button in mainMenuButtons)
        {
            if (button != null) buttonsToShow++;
        }
        
        if (buttonsToShow == 0)
        {
            onComplete?.Invoke();
            return;
        }
        
        System.Action checkComplete = () => {
            buttonsShown++;
            if (buttonsShown >= buttonsToShow)
            {
                onComplete?.Invoke();
            }
        };
        
        for (int i = 0; i < mainMenuButtons.Length; i++)
        {
            if (mainMenuButtons[i] != null)
            {
                mainMenuButtons[i].SetActive(true);
                mainMenuButtons[i].transform.localScale = Vector3.zero;
                
                // Stagger the animations
                float delay = i * 0.1f;
                mainMenuButtons[i].transform.DOScale(GetButtonOriginalScale(mainMenuButtons[i]), scaleAnimationDuration)
                    .SetEase(scaleUpEase)
                    .SetDelay(delay)
                    .OnComplete(() => checkComplete());
            }
        }
    }
    
    private void HideAllMainMenuButtons(System.Action onComplete = null)
    {
        int buttonsToHide = 0;
        int buttonsHidden = 0;
        
        foreach (var button in mainMenuButtons)
        {
            if (button != null && button.activeInHierarchy) buttonsToHide++;
        }
        
        if (buttonsToHide == 0)
        {
            onComplete?.Invoke();
            return;
        }
        
        System.Action checkComplete = () => {
            buttonsHidden++;
            if (buttonsHidden >= buttonsToHide)
            {
                onComplete?.Invoke();
            }
        };
        
        for (int i = 0; i < mainMenuButtons.Length; i++)
        {
            if (mainMenuButtons[i] != null && mainMenuButtons[i].activeInHierarchy)
            {
                // Capture the current value of i to avoid closure issues
                int currentIndex = i;
                
                // Reverse stagger the animations
                float delay = (mainMenuButtons.Length - i - 1) * 0.1f;
                mainMenuButtons[i].transform.DOScale(Vector3.zero, scaleAnimationDuration)
                    .SetEase(scaleDownEase)
                    .SetDelay(delay)
                    .OnComplete(() => {
                        if (mainMenuButtons != null && currentIndex < mainMenuButtons.Length && mainMenuButtons[currentIndex] != null)
                        {
                            mainMenuButtons[currentIndex].SetActive(false);
                        }
                        checkComplete();
                    });
            }
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
            .OnComplete(() => {
                isTransitioning = false;
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
            .OnComplete(() => {
                isTransitioning = false;
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
        
        activeMapIndex = -1;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] All button active states reset");
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
    
    private void OnDestroy()
    {
        // Clean up DOTween sequences
        DOTween.Kill(transform);
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
    
    // Public methods for external access
    public void SetAnimationDuration(float duration)
    {
        scaleAnimationDuration = duration;
    }
    
    public UIState GetCurrentState() => currentState;
    public int GetCurrentRoomIndex() => currentRoomIndex;
    public int GetActiveMapIndex() => activeMapIndex;
} 