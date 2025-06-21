using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using DG.Tweening;

public class XRUIFlowManager : MonoBehaviour
{
    [Header("Scene GameObjects")]
    [SerializeField] private GameObject introTestButton;
    [SerializeField] private GameObject mapButton;
    [SerializeField] private GameObject mapMain;
    [SerializeField] private GameObject roomList; // Child of MapMain
    [SerializeField] private GameObject backButton; // Child of MapMain
    [SerializeField] private GameObject[] roomMaps = new GameObject[6]; // Children of MapMain
    
    [Header("XR Interactables")]
    [SerializeField] private XRSimpleInteractable startInteractable; // In IntroTestButton
    [SerializeField] private XRSimpleInteractable mapInteractable; // In MapButton
    [SerializeField] private XRSimpleInteractable closeInteractable; // In RoomList
    [SerializeField] private XRSimpleInteractable backInteractable; // BackButton
    
    [Header("Animation Settings")]
    [SerializeField] private float scaleAnimationDuration = 0.6f;
    [SerializeField] private Ease scaleUpEase = Ease.OutBack;
    [SerializeField] private Ease scaleDownEase = Ease.InBack;
    [SerializeField] private float animationDelay = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private Vector3 mapButtonOriginalScale;
    private Vector3 mapMainOriginalScale;
    private Vector3 roomListOriginalScale;
    private Vector3 backButtonOriginalScale;
    private Vector3[] roomMapOriginalScales = new Vector3[6];
    private bool isTransitioning = false;
    
    // Current UI state
    public enum UIState { Start, MapButton, RoomList, RoomMap }
    private UIState currentState = UIState.Start;
    private int currentRoomIndex = -1; // Remembers which room user was in
    
    private void Awake()
    {
        FindSceneReferences();
        StoreOriginalValues();
        SetupXRListeners();
        InitializeUI();
    }
    
    private void FindSceneReferences()
    {
        // Find scene GameObjects if not assigned
        if (introTestButton == null)
            introTestButton = GameObject.Find("IntroTestButton");
            
        if (mapButton == null)
            mapButton = GameObject.Find("MapButton");
            
        if (mapMain == null)
            mapMain = GameObject.Find("MapMain");
        
        if (mapMain != null)
        {
            // Find RoomList within MapMain
            Transform roomListTransform = FindChildRecursive(mapMain.transform, "RoomList");
            if (roomListTransform != null)
                roomList = roomListTransform.gameObject;
            
            // Find BackButton within MapMain
            Transform backButtonTransform = FindChildRecursive(mapMain.transform, "BackButton");
            if (backButtonTransform != null)
                backButton = backButtonTransform.gameObject;
            
            // Find all room maps within MapMain
            for (int i = 0; i < 6; i++)
            {
                string roomName = GetRoomName(i);
                Transform roomTransform = FindChildRecursive(mapMain.transform, roomName);
                if (roomTransform != null)
                {
                    roomMaps[i] = roomTransform.gameObject;
                    if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Found {roomName} in MapMain");
                }
            }
        }
        
        // Find XR Interactables
        if (startInteractable == null && introTestButton != null)
            startInteractable = introTestButton.GetComponentInChildren<XRSimpleInteractable>();
            
        if (mapInteractable == null && mapButton != null)
            mapInteractable = mapButton.GetComponentInChildren<XRSimpleInteractable>();
            
        if (closeInteractable == null && roomList != null)
            closeInteractable = FindChildRecursive(roomList.transform, "CloseButton")?.GetComponent<XRSimpleInteractable>();
            
        if (backInteractable == null && backButton != null)
            backInteractable = backButton.GetComponent<XRSimpleInteractable>();
    }
    
    private string GetRoomName(int index)
    {
        switch (index)
        {
            case 0: return "C-2D";
            case 1: return "D-3D";
            case 2: return "E-4D";
            case 3: return "M-2D";
            case 4: return "Patio";
            case 5: return "Rooftop";
            default: return $"Room{index + 1}";
        }
    }
    
    private void StoreOriginalValues()
    {
        if (mapButton != null)
            mapButtonOriginalScale = mapButton.transform.localScale;
            
        if (mapMain != null)
            mapMainOriginalScale = mapMain.transform.localScale;
            
        if (roomList != null)
            roomListOriginalScale = roomList.transform.localScale;
            
        if (backButton != null)
            backButtonOriginalScale = backButton.transform.localScale;
        
        for (int i = 0; i < roomMaps.Length; i++)
        {
            if (roomMaps[i] != null)
                roomMapOriginalScales[i] = roomMaps[i].transform.localScale;
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
        
        if (closeInteractable != null)
        {
            closeInteractable.selectEntered.AddListener(OnCloseSelected);
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Close interactable listener added");
        }
        
        if (backInteractable != null)
        {
            backInteractable.selectEntered.AddListener(OnBackSelected);
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Back interactable listener added");
        }
        
        // Setup room button listeners  
        SetupRoomButtonListeners();
    }
    
    private void SetupRoomButtonListeners()
    {
        if (roomList == null) return;
        
        for (int i = 0; i < 6; i++)
        {
            string buttonName = $"Room{i + 1}Button";
            Transform buttonTransform = FindChildRecursive(roomList.transform, buttonName);
            
            if (buttonTransform != null)
            {
                XRSimpleInteractable roomButton = buttonTransform.GetComponent<XRSimpleInteractable>();
                if (roomButton != null)
                {
                    int roomIndex = i; // Capture for closure
                    roomButton.selectEntered.AddListener((args) => OnRoomSelected(roomIndex));
                    if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Room {i + 1} button listener added");
                }
            }
        }
    }
    
    private void InitializeUI()
    {
        // Set initial states
        if (mapButton != null)
            mapButton.SetActive(false);
            
        if (mapMain != null)
            mapMain.SetActive(false);
        
        currentState = UIState.Start;
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] UI initialized");
    }
    
    private void OnStartSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (isTransitioning) return;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Start button selected");
        
        ShowMapMainWithRoomList();
    }
    
    private void OnMapSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (isTransitioning) return;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Map button selected");
        
        HideMapButton(() => 
        {
            if (currentRoomIndex >= 0)
            {
                // Return to the room they were in
                ShowMapMainWithRoom(currentRoomIndex);
            }
            else
            {
                // First time, show room list
                ShowMapMainWithRoomList();
            }
        });
    }
    
    private void OnRoomSelected(int roomIndex)
    {
        if (isTransitioning || roomIndex < 0 || roomIndex >= roomMaps.Length) return;
        if (roomMaps[roomIndex] == null) return;
        
        if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Room {roomIndex + 1} selected");
        
        isTransitioning = true;
        currentRoomIndex = roomIndex;
        currentState = UIState.RoomMap;
        
        // Hide room list and show selected room
        HideRoomList(() => ShowRoom(roomIndex));
    }
    
    private void OnBackSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (isTransitioning) return;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Back button selected");
        
        isTransitioning = true;
        currentState = UIState.RoomList;
        
        // Hide current room and back button, show room list
        // CloseButton stays visible since MapMain remains active
        HideCurrentRoom(() => 
        {
            HideBackButton(() => ShowRoomList());
        });
    }
    
    private void OnCloseSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (isTransitioning) return;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Close button selected");
        
        isTransitioning = true;
        currentState = UIState.MapButton;
        
        // Hide entire MapMain (including CloseButton) and show MapButton
        HideMapMain(() => ShowMapButton());
    }
    
    private void ShowMapMainWithRoomList()
    {
        if (isTransitioning) return;
        
        isTransitioning = true;
        currentState = UIState.RoomList;
        
        if (mapMain != null)
        {
            mapMain.SetActive(true);
            mapMain.transform.localScale = Vector3.zero;
            
            // Ensure room list is visible and rooms are hidden
            HideAllRooms();
            HideBackButtonImmediate(); // BackButton should be hidden in RoomList
            
            mapMain.transform.DOScale(mapMainOriginalScale, scaleAnimationDuration)
                .SetEase(scaleUpEase)
                .OnComplete(() => ShowRoomList());
        }
    }
    
    private void ShowMapMainWithRoom(int roomIndex)
    {
        if (isTransitioning) return;
        
        isTransitioning = true;
        currentState = UIState.RoomMap;
        
        if (mapMain != null)
        {
            mapMain.SetActive(true);
            mapMain.transform.localScale = Vector3.zero;
            
            // Ensure room list is hidden and show the specific room
            HideRoomListImmediate();
            HideAllRooms();
            
            mapMain.transform.DOScale(mapMainOriginalScale, scaleAnimationDuration)
                .SetEase(scaleUpEase)
                .OnComplete(() => 
                {
                    ShowRoom(roomIndex);
                    ShowBackButton(); // BackButton appears when room is shown
                });
        }
    }
    
    private void ShowRoomList()
    {
        if (roomList == null) return;
        
        roomList.SetActive(true);
        roomList.transform.localScale = Vector3.zero;
        
        roomList.transform.DOScale(roomListOriginalScale, scaleAnimationDuration)
            .SetEase(scaleUpEase)
            .OnComplete(() =>
            {
                isTransitioning = false;
                if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Room list shown");
            });
    }
    
    private void ShowRoom(int roomIndex)
    {
        if (roomMaps[roomIndex] == null) return;
        
        roomMaps[roomIndex].SetActive(true);
        roomMaps[roomIndex].transform.localScale = Vector3.zero;
        
        roomMaps[roomIndex].transform.DOScale(roomMapOriginalScales[roomIndex], scaleAnimationDuration)
            .SetEase(scaleUpEase)
            .OnComplete(() => 
            {
                ShowBackButton();
                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Room {roomIndex + 1} shown");
            });
    }
    
    private void ShowBackButton()
    {
        if (backButton == null) return;
        
        backButton.SetActive(true);
        backButton.transform.localScale = Vector3.zero;
        
        backButton.transform.DOScale(backButtonOriginalScale, scaleAnimationDuration * 0.5f)
            .SetEase(scaleUpEase)
            .OnComplete(() =>
            {
                isTransitioning = false;
                if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Back button shown");
            });
    }
    
    private void ShowMapButton()
    {
        if (mapButton == null) return;
        
        mapButton.SetActive(true);
        mapButton.transform.localScale = Vector3.zero;
        
        mapButton.transform.DOScale(mapButtonOriginalScale, scaleAnimationDuration)
            .SetEase(scaleUpEase)
            .OnComplete(() =>
            {
                isTransitioning = false;
                if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Map button shown");
            });
    }
    
    private void HideMapButton(System.Action onComplete = null)
    {
        if (mapButton == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        mapButton.transform.DOScale(Vector3.zero, scaleAnimationDuration)
            .SetEase(scaleDownEase)
            .OnComplete(() =>
            {
                mapButton.SetActive(false);
                if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Map button hidden");
                onComplete?.Invoke();
            });
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
            .OnComplete(() =>
            {
                mapMain.SetActive(false);
                if (enableDebugLogs) Debug.Log("[XRUIFlowManager] MapMain hidden");
                onComplete?.Invoke();
            });
    }
    
    private void HideRoomList(System.Action onComplete = null)
    {
        if (roomList == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        roomList.transform.DOScale(Vector3.zero, scaleAnimationDuration)
            .SetEase(scaleDownEase)
            .OnComplete(() =>
            {
                roomList.SetActive(false);
                if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Room list hidden");
                onComplete?.Invoke();
            });
    }
    
    private void HideRoomListImmediate()
    {
        if (roomList != null)
            roomList.SetActive(false);
    }
    
    private void HideBackButtonImmediate()
    {
        if (backButton != null)
            backButton.SetActive(false);
    }
    
    private void HideCurrentRoom(System.Action onComplete = null)
    {
        if (currentRoomIndex < 0 || currentRoomIndex >= roomMaps.Length || roomMaps[currentRoomIndex] == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        roomMaps[currentRoomIndex].transform.DOScale(Vector3.zero, scaleAnimationDuration)
            .SetEase(scaleDownEase)
            .OnComplete(() =>
            {
                roomMaps[currentRoomIndex].SetActive(false);
                if (enableDebugLogs) Debug.Log($"[XRUIFlowManager] Room {currentRoomIndex + 1} hidden");
                onComplete?.Invoke();
            });
    }
    
    private void HideBackButton(System.Action onComplete = null)
    {
        if (backButton == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        backButton.transform.DOScale(Vector3.zero, scaleAnimationDuration * 0.5f)
            .SetEase(scaleDownEase)
            .OnComplete(() =>
            {
                backButton.SetActive(false);
                if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Back button hidden");
                onComplete?.Invoke();
            });
    }
    
    private void HideAllRooms()
    {
        for (int i = 0; i < roomMaps.Length; i++)
        {
            if (roomMaps[i] != null)
                roomMaps[i].SetActive(false);
        }
    }
    
    // Helper method to find child by name recursively
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
            
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }
    
    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
    
    // Public utility methods for testing
    [ContextMenu("Test Start Interaction")]
    public void TestStartInteraction()
    {
        OnStartSelected(null);
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
    
    [ContextMenu("Test Back Interaction")]
    public void TestBackInteraction()
    {
        OnBackSelected(null);
    }
    
    [ContextMenu("Test Close Interaction")]
    public void TestCloseInteraction()
    {
        OnCloseSelected(null);
    }
    
    public void SetAnimationDuration(float duration)
    {
        scaleAnimationDuration = duration;
    }
    
    // Get current state for external scripts
    public UIState GetCurrentState() => currentState;
    public int GetCurrentRoomIndex() => currentRoomIndex;
} 