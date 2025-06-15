using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIFlowManagerWithPrefabs : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject introTestButton;
    [SerializeField] private GameObject mapButton;
    [SerializeField] private GameObject e4DMap;
    
    [Header("Prefab References")]
    [SerializeField] private GameObject mapButtonPrefab; // Assign the original MapButton prefab
    [SerializeField] private Transform mapButtonParent; // Parent transform for MapButton
    
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button mapClickButton;
    [SerializeField] private Button backButton;
    
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
    private bool isTransitioning = false;
    
    private void Awake()
    {
        SetupReferences();
        StoreOriginalValues();
        SetupButtonListeners();
        InitializeUI();
    }
    
    private void SetupReferences()
    {
        // Auto-find components if not assigned
        if (introTestButton == null)
            introTestButton = GameObject.Find("IntroTestButton");
            
        if (mapButton == null)
            mapButton = GameObject.Find("MapButton");
            
        if (e4DMap == null)
            e4DMap = GameObject.Find("E-4D-Map");
        
        // Store the prefab reference if not set
        if (mapButtonPrefab == null && mapButton != null)
        {
            // Try to find it in Resources folder
            mapButtonPrefab = Resources.Load<GameObject>("MapButton");
            if (mapButtonPrefab == null && enableDebugLogs)
                Debug.LogWarning("[UIFlowManager] MapButton prefab not found in Resources. Please assign it manually.");
        }
        
        // Set parent if not assigned
        if (mapButtonParent == null && mapButton != null)
            mapButtonParent = mapButton.transform.parent;
        
        // Auto-find buttons
        if (startButton == null && introTestButton != null)
            startButton = introTestButton.GetComponentInChildren<Button>();
            
        if (mapClickButton == null && mapButton != null)
            mapClickButton = mapButton.GetComponentInChildren<Button>();
            
        if (backButton == null && e4DMap != null)
            backButton = e4DMap.GetComponentInChildren<Button>();
    }
    
    private void StoreOriginalValues()
    {
        if (mapButton != null)
        {
            mapButtonOriginalScale = mapButton.transform.localScale;
            mapButtonOriginalPosition = mapButton.transform.localPosition;
        }
            
        if (e4DMap != null)
            e4DMapOriginalScale = e4DMap.transform.localScale;
    }
    
    private void SetupButtonListeners()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
            if (enableDebugLogs) Debug.Log("[UIFlowManager] Start button listener added");
        }
        
        SetupMapButtonListener();
        SetupBackButtonListener();
    }
    
    private void SetupMapButtonListener()
    {
        if (mapClickButton != null)
        {
            mapClickButton.onClick.RemoveAllListeners();
            mapClickButton.onClick.AddListener(OnMapButtonClicked);
            if (enableDebugLogs) Debug.Log("[UIFlowManager] Map button listener added");
        }
    }
    
    private void SetupBackButtonListener()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
            if (enableDebugLogs) Debug.Log("[UIFlowManager] Back button listener added");
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
        
        if (enableDebugLogs) Debug.Log("[UIFlowManager] UI initialized");
    }
    
    private void OnStartButtonClicked()
    {
        if (isTransitioning) return;
        
        if (enableDebugLogs) Debug.Log("[UIFlowManager] Start button clicked");
        
        if (mapButton != null)
        {
            ActivateMapButton();
        }
    }
    
    private void OnMapButtonClicked()
    {
        if (isTransitioning) return;
        
        if (enableDebugLogs) Debug.Log("[UIFlowManager] Map button clicked");
        
        isTransitioning = true;
        
        // Start both animations simultaneously
        if (e4DMap != null)
        {
            ActivateE4DMap();
        }
        
        if (mapButton != null)
        {
            DeactivateMapButton();
        }
    }
    
    private void OnBackButtonClicked()
    {
        if (isTransitioning) return;
        
        if (enableDebugLogs) Debug.Log("[UIFlowManager] Back button clicked");
        
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
                if (enableDebugLogs) Debug.Log("[UIFlowManager] MapButton activation complete");
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
                        mapClickButton = null;
                        if (enableDebugLogs) Debug.Log("[UIFlowManager] MapButton destroyed");
                    }
                });
            });
    }
    
    private void ActivateE4DMap()
    {
        e4DMap.SetActive(true);
        e4DMap.transform.localScale = Vector3.zero;
        
        // Setup back button listener when E4DMap is activated
        SetupBackButtonListener();
        
        e4DMap.transform.DOScale(e4DMapOriginalScale, scaleAnimationDuration)
            .SetEase(scaleUpEase)
            .OnComplete(() =>
            {
                isTransitioning = false;
                if (enableDebugLogs) Debug.Log("[UIFlowManager] E-4D-Map activation complete");
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
                        backButton = null;
                        if (enableDebugLogs) Debug.Log("[UIFlowManager] E-4D-Map destroyed");
                    }
                });
            });
    }
    
    private void RecreateAndActivateMapButton()
    {
        if (enableDebugLogs) Debug.Log("[UIFlowManager] Recreating MapButton");
        
        if (mapButtonPrefab != null && mapButtonParent != null)
        {
            // Instantiate new MapButton from prefab
            mapButton = Instantiate(mapButtonPrefab, mapButtonParent);
            
            // Restore original position and scale
            mapButton.transform.localPosition = mapButtonOriginalPosition;
            mapButton.transform.localScale = Vector3.zero;
            
            // Get the button component
            mapClickButton = mapButton.GetComponentInChildren<Button>();
            
            // Setup listener
            SetupMapButtonListener();
            
            // Activate with animation
            ActivateMapButton();
            
            if (enableDebugLogs) Debug.Log("[UIFlowManager] MapButton recreated successfully");
        }
        else
        {
            isTransitioning = false;
            if (enableDebugLogs) Debug.LogError("[UIFlowManager] Cannot recreate MapButton - prefab or parent is null!");
        }
    }
    
    private void OnDestroy()
    {
        DOTween.Kill(this);
        
        // Clean up listeners
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartButtonClicked);
            
        if (mapClickButton != null)
            mapClickButton.onClick.RemoveListener(OnMapButtonClicked);
            
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackButtonClicked);
    }
    
    // Public utility methods
    [ContextMenu("Test Start Button")]
    public void TestStartButton()
    {
        OnStartButtonClicked();
    }
    
    [ContextMenu("Test Map Button")]
    public void TestMapButton()
    {
        OnMapButtonClicked();
    }
    
    [ContextMenu("Test Back Button")]
    public void TestBackButton()
    {
        OnBackButtonClicked();
    }
    
    public void SetAnimationDuration(float duration)
    {
        scaleAnimationDuration = duration;
    }
    
    public void SetMapButtonPrefab(GameObject prefab)
    {
        mapButtonPrefab = prefab;
    }
} 