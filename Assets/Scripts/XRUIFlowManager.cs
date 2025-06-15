using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using DG.Tweening;

public class XRUIFlowManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject introTestButton;
    [SerializeField] private GameObject mapButton;
    [SerializeField] private GameObject e4DMap;
    
    [Header("Prefab References")]
    [SerializeField] private GameObject mapButtonPrefab; // Assign the original MapButton prefab
    [SerializeField] private GameObject e4DMapPrefab; // Assign the original E-4D-Map prefab
    
    [Header("XR Interactables")]
    [SerializeField] private XRSimpleInteractable startInteractable; // XR interactable in IntroTestButton
    [SerializeField] private XRSimpleInteractable mapInteractable; // XR interactable in MapButton
    [SerializeField] private XRSimpleInteractable backInteractable; // Back interactable in E-4D-Map
    
    [Header("Animation Settings")]
    [SerializeField] private float scaleAnimationDuration = 0.6f;
    [SerializeField] private Ease scaleUpEase = Ease.OutBack;
    [SerializeField] private Ease scaleDownEase = Ease.InBack;
    [SerializeField] private float destroyDelay = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private Vector3 mapButtonOriginalScale;
    private Vector3 e4DMapOriginalScale;
    private Vector3 mapButtonOriginalPosition;
    private Vector3 e4DMapOriginalPosition;
    private bool isTransitioning = false;
    
    private void Awake()
    {
        SetupReferences();
        StoreOriginalValues();
        SetupXRListeners();
        InitializeUI();
    }
    
    private void SetupReferences()
    {
        // Auto-find GameObjects if not assigned
        if (introTestButton == null)
            introTestButton = GameObject.Find("IntroTestButton");
            
        if (mapButton == null)
            mapButton = GameObject.Find("MapButton");
            
        if (e4DMap == null)
            e4DMap = GameObject.Find("E-4D-Map");
        
        // Store the prefab references if not set
        if (mapButtonPrefab == null && mapButton != null)
        {
            mapButtonPrefab = Resources.Load<GameObject>("MapButton");
            if (mapButtonPrefab == null && enableDebugLogs)
                Debug.LogWarning("[XRUIFlowManager] MapButton prefab not found in Resources. Please assign it manually.");
        }
        
        if (e4DMapPrefab == null && e4DMap != null)
        {
            e4DMapPrefab = Resources.Load<GameObject>("E-4D-Map");
            if (e4DMapPrefab == null && enableDebugLogs)
                Debug.LogWarning("[XRUIFlowManager] E-4D-Map prefab not found in Resources. Please assign it manually.");
        }
        
        // Note: No parent needed - will spawn in scene root
        
        // Auto-find XR Interactables
        if (startInteractable == null && introTestButton != null)
            startInteractable = introTestButton.GetComponentInChildren<XRSimpleInteractable>();
            
        if (mapInteractable == null && mapButton != null)
            mapInteractable = mapButton.GetComponentInChildren<XRSimpleInteractable>();
            
        if (backInteractable == null && e4DMap != null)
            backInteractable = e4DMap.GetComponentInChildren<XRSimpleInteractable>();
    }
    
    private void StoreOriginalValues()
    {
        if (mapButton != null)
        {
            mapButtonOriginalScale = mapButton.transform.localScale;
            mapButtonOriginalPosition = mapButton.transform.position;
        }
            
        if (e4DMap != null)
        {
            e4DMapOriginalScale = e4DMap.transform.localScale;
            e4DMapOriginalPosition = e4DMap.transform.position;
        }
    }
    
    private void SetupXRListeners()
    {
        if (startInteractable != null)
        {
            startInteractable.selectEntered.AddListener(OnStartSelected);
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Start interactable listener added");
        }
        
        SetupMapInteractableListener();
        SetupBackInteractableListener();
    }
    
    private void SetupMapInteractableListener()
    {
        if (mapInteractable != null)
        {
            // Remove existing listeners to avoid duplicates
            mapInteractable.selectEntered.RemoveListener(OnMapSelected);
            mapInteractable.selectEntered.AddListener(OnMapSelected);
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Map interactable listener added");
        }
    }
    
    private void SetupBackInteractableListener()
    {
        if (backInteractable != null)
        {
            // Remove existing listeners to avoid duplicates
            backInteractable.selectEntered.RemoveListener(OnBackSelected);
            backInteractable.selectEntered.AddListener(OnBackSelected);
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Back interactable listener added");
        }
    }
    
    private void InitializeUI()
    {
        // Set initial states
        if (mapButton != null)
        {
            mapButton.SetActive(false);
            mapButton.transform.localScale = Vector3.zero;
        }
        
        if (e4DMap != null)
        {
            e4DMap.SetActive(false);
            e4DMap.transform.localScale = Vector3.zero;
        }
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] UI initialized");
    }
    
    private void OnStartSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (isTransitioning) return;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Start interactable selected");
        
        if (mapButton != null)
        {
            ActivateMapButton();
        }
    }
    
    private void OnMapSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (isTransitioning) return;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Map interactable selected");
        
        isTransitioning = true;
        
        // Start both animations simultaneously
        if (e4DMap != null)
        {
            ActivateE4DMap();
        }
        else
        {
            // Recreate E-4D-Map if it was destroyed
            RecreateAndActivateE4DMap();
        }
        
        if (mapButton != null)
        {
            DeactivateMapButton();
        }
    }
    
    private void OnBackSelected(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (isTransitioning) return;
        
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Back interactable selected");
        
        isTransitioning = true;
        
        if (e4DMap != null)
        {
            DeactivateE4DMap();
        }
        
        // Recreate MapButton after a delay
        DOVirtual.DelayedCall(scaleAnimationDuration * 0.5f, RecreateAndActivateMapButton);
    }
    
    private void ActivateMapButton()
    {
        isTransitioning = true;
        
        mapButton.SetActive(true);
        mapButton.transform.localScale = Vector3.zero;
        
        mapButton.transform.DOScale(mapButtonOriginalScale, scaleAnimationDuration)
            .SetEase(scaleUpEase)
            .OnComplete(() =>
            {
                isTransitioning = false;
                if (enableDebugLogs) Debug.Log("[XRUIFlowManager] MapButton activation complete");
            });
    }
    
    private void DeactivateMapButton()
    {
        mapButton.transform.DOScale(Vector3.zero, scaleAnimationDuration)
            .SetEase(scaleDownEase)
            .OnComplete(() =>
            {
                DOVirtual.DelayedCall(destroyDelay, () =>
                {
                    if (mapButton != null)
                    {
                        Destroy(mapButton);
                        mapButton = null;
                        mapInteractable = null;
                        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] MapButton destroyed");
                    }
                });
            });
    }
    
    private void ActivateE4DMap()
    {
        e4DMap.SetActive(true);
        e4DMap.transform.localScale = Vector3.zero;
        
        // Setup back interactable listener when E4DMap is activated
        SetupBackInteractableListener();
        
        e4DMap.transform.DOScale(e4DMapOriginalScale, scaleAnimationDuration)
            .SetEase(scaleUpEase)
            .OnComplete(() =>
            {
                isTransitioning = false;
                if (enableDebugLogs) Debug.Log("[XRUIFlowManager] E-4D-Map activation complete");
            });
    }
    
    private void DeactivateE4DMap()
    {
        e4DMap.transform.DOScale(Vector3.zero, scaleAnimationDuration)
            .SetEase(scaleDownEase)
            .OnComplete(() =>
            {
                DOVirtual.DelayedCall(destroyDelay, () =>
                {
                    if (e4DMap != null)
                    {
                        Destroy(e4DMap);
                        e4DMap = null;
                        backInteractable = null;
                        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] E-4D-Map destroyed");
                    }
                });
            });
    }
    
    private void RecreateAndActivateMapButton()
    {
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Recreating MapButton");
        
        if (mapButtonPrefab != null)
        {
            // Instantiate new MapButton from prefab directly in scene
            mapButton = Instantiate(mapButtonPrefab);
            
            // Restore original position and scale
            mapButton.transform.position = mapButtonOriginalPosition;
            mapButton.transform.localScale = Vector3.zero;
            
            // Get the XR interactable component
            mapInteractable = mapButton.GetComponentInChildren<XRSimpleInteractable>();
            
            // Setup listener
            SetupMapInteractableListener();
            
            // Activate with animation
            ActivateMapButton();
            
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] MapButton recreated successfully");
        }
        else
        {
            isTransitioning = false;
            if (enableDebugLogs) Debug.LogError("[XRUIFlowManager] Cannot recreate MapButton - prefab is null!");
        }
    }
    
    private void RecreateAndActivateE4DMap()
    {
        if (enableDebugLogs) Debug.Log("[XRUIFlowManager] Recreating E-4D-Map");
        
        if (e4DMapPrefab != null)
        {
            // Instantiate new E-4D-Map from prefab directly in scene
            e4DMap = Instantiate(e4DMapPrefab);
            
            // Restore original position and scale
            e4DMap.transform.position = e4DMapOriginalPosition;
            e4DMap.transform.localScale = Vector3.zero;
            
            // Get the XR interactable component for back button
            backInteractable = e4DMap.GetComponentInChildren<XRSimpleInteractable>();
            
            // Setup listener
            SetupBackInteractableListener();
            
            // Activate with animation
            ActivateE4DMap();
            
            if (enableDebugLogs) Debug.Log("[XRUIFlowManager] E-4D-Map recreated successfully");
        }
        else
        {
            isTransitioning = false;
            if (enableDebugLogs) Debug.LogError("[XRUIFlowManager] Cannot recreate E-4D-Map - prefab is null!");
        }
    }
    
    private void OnDestroy()
    {
        DOTween.Kill(this);
        
        // Clean up XR listeners
        if (startInteractable != null)
            startInteractable.selectEntered.RemoveListener(OnStartSelected);
            
        if (mapInteractable != null)
            mapInteractable.selectEntered.RemoveListener(OnMapSelected);
            
        if (backInteractable != null)
            backInteractable.selectEntered.RemoveListener(OnBackSelected);
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
    
    [ContextMenu("Test Back Interaction")]
    public void TestBackInteraction()
    {
        OnBackSelected(null);
    }
    
    public void SetAnimationDuration(float duration)
    {
        scaleAnimationDuration = duration;
    }
    
    public void SetMapButtonPrefab(GameObject prefab)
    {
        mapButtonPrefab = prefab;
    }
    
    public void SetE4DMapPrefab(GameObject prefab)
    {
        e4DMapPrefab = prefab;
    }
    
    // Method to manually refresh interactable references (useful after recreation)
    public void RefreshInteractableReferences()
    {
        SetupReferences();
        SetupXRListeners();
    }
} 