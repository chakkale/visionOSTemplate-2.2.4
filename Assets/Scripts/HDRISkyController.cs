using UnityEngine;
using UnityEngine.InputSystem;


public class HDRISkyController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 500f;  // Increased base rotation speed
    [SerializeField] private float smoothSpeed = 10f;     // Controls how smooth the rotation transitions are
    [SerializeField] private Transform skyboxSphere;      // Reference to the sphere with reversed normals
    
    // Input System references
    [Header("Input")]
    [SerializeField] private InputActionReference rightHandPositionAction;
    [SerializeField] private InputActionReference rightHandIsTrackedAction;
    
    // XR Interaction Toolkit references
    [Header("XR Interaction")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rightHandRayInteractor;
    
    // Tracking variables
    private Vector3 lastHandPosition;
    private bool wasPinching;
    private float currentRotation;      // Current actual rotation
    private float targetRotation;       // Target rotation we're smoothly moving towards
    
    // Constants
    private const float PINCH_THRESHOLD = 0.03f; // Distance threshold for pinch detection

    private void OnEnable()
    {
        // Enable input actions
        if (rightHandPositionAction != null)
            rightHandPositionAction.action.Enable();
        
        if (rightHandIsTrackedAction != null)
            rightHandIsTrackedAction.action.Enable();
    }

    private void OnDisable()
    {
        // Disable input actions
        if (rightHandPositionAction != null)
            rightHandPositionAction.action.Disable();
        
        if (rightHandIsTrackedAction != null)
            rightHandIsTrackedAction.action.Disable();
    }

    private void Start()
    {
        //Debug.Log("SkyboxController: Starting initialization...");
        
        // If skybox sphere isn't assigned, try to find it on this GameObject
        if (skyboxSphere == null)
        {
            skyboxSphere = transform;
            //Debug.Log("No skybox sphere assigned, using this GameObject's transform");
        }

        // Check if input actions are assigned
        if (rightHandPositionAction == null || rightHandIsTrackedAction == null)
        {
            Debug.LogWarning("Input actions not assigned. Please assign the right hand position and tracking actions.");
        }
        
        // Store initial rotation values
        currentRotation = skyboxSphere.eulerAngles.y;
        targetRotation = currentRotation;
        
        //Debug.Log("SkyboxController: Initialization complete");
    }

    private void Update()
    {
        // Block dome rotation if the right hand ray interactor is actively selecting an object on the UI layer
        if (rightHandRayInteractor != null && rightHandRayInteractor.hasSelection)
        {
            GameObject selectedObject = null;
            if (rightHandRayInteractor.interactablesSelected.Count > 0)
                selectedObject = rightHandRayInteractor.interactablesSelected[0].transform.gameObject;

            if (selectedObject != null && selectedObject.layer == 5) // 5 is the UI layer
            {
                wasPinching = false;
                return;
            }
        }

        // Early exit if required components are missing
        if (skyboxSphere == null || rightHandPositionAction == null || rightHandIsTrackedAction == null) 
            return;

        // Check if the hand is being tracked
        bool isHandTracked = rightHandIsTrackedAction.action.ReadValue<float>() > 0.5f;

        if (isHandTracked)
        {
            // Get the current hand position
            Vector3 currentHandPosition = rightHandPositionAction.action.ReadValue<Vector3>();
            
            // Check for pinch gesture using the distance between thumb and index finger
            // For Vision Pro, we'll use a simpler approach - just check if the hand is tracked
            bool isPinching = true; // In Vision Pro, we'll assume pinching when the hand is tracked
            
            if (isPinching)
            {
                if (!wasPinching)
                {
                    // Starting a new pinch - store the initial position
                    lastHandPosition = currentHandPosition;
                    wasPinching = true;
                    //Debug.Log($"Pinch started at position {currentHandPosition}");
                }
                else
                {
                    // Calculate the horizontal movement since last frame
                    float horizontalDelta = currentHandPosition.x - lastHandPosition.x;
                    
                    // Convert movement to rotation (positive for intuitive control)
                    // Multiply by a larger value for faster rotation
                    float rotationAmount = horizontalDelta * rotationSpeed * Time.deltaTime;
                    
                    // Update target rotation value
                    targetRotation = Mathf.Repeat(targetRotation + rotationAmount, 360f);
                    
                    // Log rotation update for debugging
                    //if (Mathf.Abs(rotationAmount) > 0.01f)
                    //{
                    //    Debug.Log($"Setting Target Rotation - Movement: {horizontalDelta:F3}, " +
                    //            $"Rotation Change: {rotationAmount:F3}, " +
                    //            $"Target: {targetRotation:F1}");
                    //}
                    
                    // Store position for next frame
                    lastHandPosition = currentHandPosition;
                }
            }
            else
            {
                wasPinching = false;
            }
        }
        else
        {
            wasPinching = false;
        }

        // Smoothly interpolate towards target rotation
        if (Mathf.Abs(targetRotation - currentRotation) > 0.01f)
        {
            // Calculate the shortest rotation path
            float delta = Mathf.DeltaAngle(currentRotation, targetRotation);
            currentRotation = Mathf.MoveTowards(currentRotation, currentRotation + delta, Time.deltaTime * smoothSpeed * Mathf.Abs(delta));
            currentRotation = Mathf.Repeat(currentRotation, 360f);
            
            // Apply the smoothed rotation to the skybox sphere
            // We keep the existing X and Z rotation values and only modify Y (horizontal rotation)
            Vector3 currentEuler = skyboxSphere.eulerAngles;
            skyboxSphere.eulerAngles = new Vector3(currentEuler.x, currentRotation, currentEuler.z);
        }
    }

    // Helper method to handle angle wrapping when interpolating rotations
    private float WrapAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            return angle - 360f;
        return angle;
    }

    // Add this method to allow RoomManager to update the sphere reference
    public void SetSkyboxSphere(Transform newSphere)
    {
        skyboxSphere = newSphere;
        if (skyboxSphere != null)
        {
            currentRotation = skyboxSphere.eulerAngles.y;
            targetRotation = currentRotation;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (skyboxSphere == null)
        {
            Debug.LogWarning("No skybox sphere assigned. Please assign the sphere Transform or attach this script to the sphere.");
        }
        
        if (rightHandPositionAction == null || rightHandIsTrackedAction == null)
        {
            Debug.LogWarning("Input actions not assigned. Please assign the right hand position and tracking actions.");
        }
    }
#endif
}