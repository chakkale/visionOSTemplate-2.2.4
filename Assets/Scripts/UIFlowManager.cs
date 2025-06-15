using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIFlowManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject introTestButton;
    [SerializeField] private GameObject mapButton;
    [SerializeField] private GameObject e4DMap;
    
    [Header("Buttons")]
    [SerializeField] private Button startButton; // Button inside IntroTestButton
    [SerializeField] private Button mapClickButton; // Button inside MapButton
    [SerializeField] private Button backButton; // Back button inside E-4D-Map
    
    [Header("Animation Settings")]
    [SerializeField] private float scaleAnimationDuration = 0.6f;
    [SerializeField] private Ease scaleUpEase = Ease.OutBack;
    [SerializeField] private Ease scaleDownEase = Ease.InBack;
    [SerializeField] private float destroyDelay = 0.1f; // Small delay before destroying
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private Vector3 mapButtonOriginalScale;
    private Vector3 e4DMapOriginalScale;
    
    private void Awake()
    {
        SetupReferences();
        StoreOriginalScales();
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
        
        // Auto-find buttons if not assigned
        if (startButton == null && introTestButton != null)
            startButton = introTestButton.GetComponentInChildren<Button>();
            
        if (mapClickButton == null && mapButton != null)
            mapClickButton = mapButton.GetComponentInChildren<Button>();
            
        if (backButton == null && e4DMap != null)
            backButton = e4DMap.GetComponentInChildren<Button>();
    }
    
    private void StoreOriginalScales()
    {
        if (mapButton != null)
            mapButtonOriginalScale = mapButton.transform.localScale;
            
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
        else if (enableDebugLogs) Debug.LogWarning("[UIFlowManager] Start button not found!");
        
        if (mapClickButton != null)
        {
            mapClickButton.onClick.AddListener(OnMapButtonClicked);
            if (enableDebugLogs) Debug.Log("[UIFlowManager] Map button listener added");
        }
        else if (enableDebugLogs) Debug.LogWarning("[UIFlowManager] Map click button not found!");
        
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
            if (enableDebugLogs) Debug.Log("[UIFlowManager] Back button listener added");
        }
        else if (enableDebugLogs) Debug.LogWarning("[UIFlowManager] Back button not found!");
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
        
        if (enableDebugLogs) Debug.Log("[UIFlowManager] UI initialized - MapButton and E-4D-Map hidden");
    }
    
    private void OnStartButtonClicked()
    {
        if (enableDebugLogs) Debug.Log("[UIFlowManager] Start button clicked - Activating MapButton");
        
        if (mapButton != null)
        {
            ActivateMapButton();
        }
        else if (enableDebugLogs) Debug.LogError("[UIFlowManager] MapButton reference is null!");
    }
    
    private void OnMapButtonClicked()
    {
        if (enableDebugLogs) Debug.Log("[UIFlowManager] Map button clicked - Opening E-4D-Map");
        
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
        if (enableDebugLogs) Debug.Log("[UIFlowManager] Back button clicked - Returning to MapButton");
        
        // Start both animations simultaneously
        if (e4DMap != null)
        {
            DeactivateE4DMap();
        }
        
        // Recreate and activate MapButton after a short delay
        DOVirtual.DelayedCall(scaleAnimationDuration * 0.5f, () =>
        {
            RecreateAndActivateMapButton();
        });
    }
    
    private void ActivateMapButton()
    {
        mapButton.SetActive(true);
        mapButton.transform.localScale = Vector3.zero;
        
        mapButton.transform.DOScale(mapButtonOriginalScale, scaleAnimationDuration)
            .SetEase(scaleUpEase)
            .OnComplete(() =>
            {
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
                        if (enableDebugLogs) Debug.Log("[UIFlowManager] MapButton destroyed");
                    }
                });
            });
    }
    
    private void ActivateE4DMap()
    {
        e4DMap.SetActive(true);
        e4DMap.transform.localScale = Vector3.zero;
        
        e4DMap.transform.DOScale(e4DMapOriginalScale, scaleAnimationDuration)
            .SetEase(scaleUpEase)
            .OnComplete(() =>
            {
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
                        if (enableDebugLogs) Debug.Log("[UIFlowManager] E-4D-Map destroyed");
                    }
                });
            });
    }
    
    private void RecreateAndActivateMapButton()
    {
        // Find the MapButton prefab in Resources or create a new instance
        // For now, we'll assume you have a way to recreate it
        // You might need to store a reference to the original prefab
        
        if (enableDebugLogs) Debug.Log("[UIFlowManager] Attempting to recreate MapButton");
        
        // Try to find if MapButton still exists (in case it wasn't destroyed)
        GameObject foundMapButton = GameObject.Find("MapButton");
        
        if (foundMapButton != null)
        {
            mapButton = foundMapButton;
            mapClickButton = mapButton.GetComponentInChildren<Button>();
            if (mapClickButton != null)
            {
                mapClickButton.onClick.RemoveAllListeners();
                mapClickButton.onClick.AddListener(OnMapButtonClicked);
            }
            
            ActivateMapButton();
        }
        else
        {
            if (enableDebugLogs) Debug.LogWarning("[UIFlowManager] Could not find MapButton to reactivate. You may need to implement prefab instantiation.");
        }
    }
    
    private void OnDestroy()
    {
        // Clean up DOTween animations
        DOTween.Kill(this);
        
        // Remove button listeners
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartButtonClicked);
            
        if (mapClickButton != null)
            mapClickButton.onClick.RemoveListener(OnMapButtonClicked);
            
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackButtonClicked);
    }
    
    // Public methods for external control
    public void ForceActivateMapButton()
    {
        OnStartButtonClicked();
    }
    
    public void ForceOpenE4DMap()
    {
        OnMapButtonClicked();
    }
    
    public void ForceGoBack()
    {
        OnBackButtonClicked();
    }
    
    // Utility methods
    public void SetAnimationDuration(float duration)
    {
        scaleAnimationDuration = duration;
    }
    
    public void SetScaleEases(Ease upEase, Ease downEase)
    {
        scaleUpEase = upEase;
        scaleDownEase = downEase;
    }
} 