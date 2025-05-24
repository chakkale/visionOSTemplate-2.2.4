using UnityEngine;
using UnityEngine.InputSystem;

public class HandInputManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    
    // References to specific actions
    private InputAction rightHandPositionAction;
    private InputAction rightHandIsTrackedAction;
    
    // Public properties to access the actions
    public InputAction RightHandPositionAction => rightHandPositionAction;
    public InputAction RightHandIsTrackedAction => rightHandIsTrackedAction;
    
    private void Awake()
    {
        // Get references to the specific actions
        var handTrackingMap = inputActions.FindActionMap("HandTracking");
        
        if (handTrackingMap != null)
        {
            rightHandPositionAction = handTrackingMap.FindAction("RightHandPosition");
            rightHandIsTrackedAction = handTrackingMap.FindAction("RightHandIsTracked");
        }
        else
        {
            Debug.LogError("HandTracking action map not found in the input actions asset!");
        }
    }
    
    private void OnEnable()
    {
        // Enable the action map
        if (inputActions != null)
        {
            var handTrackingMap = inputActions.FindActionMap("HandTracking");
            if (handTrackingMap != null)
            {
                handTrackingMap.Enable();
            }
        }
    }
    
    private void OnDisable()
    {
        // Disable the action map
        if (inputActions != null)
        {
            var handTrackingMap = inputActions.FindActionMap("HandTracking");
            if (handTrackingMap != null)
            {
                handTrackingMap.Disable();
            }
        }
    }
} 