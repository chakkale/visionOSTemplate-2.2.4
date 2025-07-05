using UnityEngine;

#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Editor-only mouse look script for analyzing visionOS projects in Unity play mode.
/// This script only works in the Unity Editor and will not be included in builds.
/// </summary>
[System.Serializable]
public class EditorMouseLook : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private bool invertY = false;
    
    [Header("Rotation Limits")]
    [SerializeField] private float minVerticalAngle = -90f;
    [SerializeField] private float maxVerticalAngle = 90f;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private bool enableMovement = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private Mouse mouse;
    private Keyboard keyboard;
    private Camera cam;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    // Mouse look variables
    private float mouseX = 0f;
    private float mouseY = 0f;
    private float currentMouseX = 0f;
    private float currentMouseY = 0f;
    private float xVelocity = 0f;
    private float yVelocity = 0f;
    
    // State tracking
    private bool isMouseLookActive = false;
    private bool wasMouseLookActive = false;
    
    void Start()
    {
        // Get input devices
        mouse = Mouse.current;
        keyboard = Keyboard.current;
        cam = GetComponent<Camera>();
        
        if (cam == null)
        {
            Debug.LogError("EditorMouseLook: No Camera component found on this GameObject!");
            enabled = false;
            return;
        }
        
        // Store original transform data
        originalParent = transform.parent;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        
        // Initialize mouse look angles
        Vector3 eulerAngles = transform.eulerAngles;
        mouseX = eulerAngles.y;
        mouseY = eulerAngles.x;
        
        if (showDebugInfo)
        {
            Debug.Log("EditorMouseLook: Initialized. Right-click and drag to look around.");
            if (enableMovement)
                Debug.Log("EditorMouseLook: WASD to move, Shift to sprint, Space to go up, Ctrl to go down.");
        }
    }
    
    void Update()
    {
        if (mouse == null || keyboard == null) return;
        
        // Check if right mouse button is pressed AND mouse is within the game view
        bool rightMousePressed = mouse.rightButton.isPressed;
        bool mouseInGameView = IsMouseInGameView();
        
        // Update mouse look state - only activate if mouse is in game view
        isMouseLookActive = rightMousePressed && mouseInGameView;
        
        // Handle cursor lock/unlock
        if (isMouseLookActive && !wasMouseLookActive)
        {
            // Just started mouse look
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (!isMouseLookActive && wasMouseLookActive)
        {
            // Just stopped mouse look
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Perform mouse look
        if (isMouseLookActive)
        {
            HandleMouseLook();
        }
        
        // Handle movement
        if (enableMovement)
        {
            HandleMovement();
        }
        
        wasMouseLookActive = isMouseLookActive;
    }
    
    bool IsMouseInGameView()
    {
        // Get mouse position in screen coordinates
        Vector2 mousePosition = mouse.position.ReadValue();
        
        // Check if mouse is within the game view bounds
        return mousePosition.x >= 0 && mousePosition.x <= Screen.width &&
               mousePosition.y >= 0 && mousePosition.y <= Screen.height;
    }
    
    void HandleMouseLook()
    {
        // Get mouse delta
        Vector2 mouseDelta = mouse.delta.ReadValue();
        
        // Apply sensitivity
        float deltaX = mouseDelta.x * mouseSensitivity * Time.unscaledDeltaTime;
        float deltaY = mouseDelta.y * mouseSensitivity * Time.unscaledDeltaTime;
        
        // Invert Y if needed
        if (invertY)
            deltaY = -deltaY;
        
        // Update target angles
        mouseX += deltaX;
        mouseY -= deltaY;
        
        // Clamp vertical rotation
        mouseY = Mathf.Clamp(mouseY, minVerticalAngle, maxVerticalAngle);
        
        // Smooth the rotation
        currentMouseX = Mathf.SmoothDampAngle(currentMouseX, mouseX, ref xVelocity, smoothTime);
        currentMouseY = Mathf.SmoothDampAngle(currentMouseY, mouseY, ref yVelocity, smoothTime);
        
        // Apply rotation
        transform.rotation = Quaternion.Euler(currentMouseY, currentMouseX, 0f);
    }
    
    void HandleMovement()
    {
        Vector3 moveDirection = Vector3.zero;
        
        // Get input
        if (keyboard.wKey.isPressed) moveDirection += transform.forward;
        if (keyboard.sKey.isPressed) moveDirection -= transform.forward;
        if (keyboard.aKey.isPressed) moveDirection -= transform.right;
        if (keyboard.dKey.isPressed) moveDirection += transform.right;
        if (keyboard.spaceKey.isPressed) moveDirection += transform.up;
        if (keyboard.leftCtrlKey.isPressed) moveDirection -= transform.up;
        
        // Apply sprint multiplier
        float currentMoveSpeed = moveSpeed;
        if (keyboard.leftShiftKey.isPressed)
            currentMoveSpeed *= sprintMultiplier;
        
        // Apply movement
        if (moveDirection != Vector3.zero)
        {
            transform.position += moveDirection.normalized * currentMoveSpeed * Time.unscaledDeltaTime;
        }
    }
    
    void OnDisable()
    {
        // Restore cursor state
        if (Application.isPlaying)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    void OnDestroy()
    {
        // Restore cursor state
        if (Application.isPlaying)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    [ContextMenu("Reset Camera Transform")]
    public void ResetCameraTransform()
    {
        if (Application.isPlaying)
        {
            transform.parent = originalParent;
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            
            // Reset mouse look values
            Vector3 eulerAngles = transform.eulerAngles;
            mouseX = eulerAngles.y;
            mouseY = eulerAngles.x;
            currentMouseX = mouseX;
            currentMouseY = mouseY;
            
            if (showDebugInfo)
                Debug.Log("EditorMouseLook: Camera transform reset to original position and rotation.");
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 140));
        GUILayout.Label("EditorMouseLook (Editor Only)", boldLabel);
        GUILayout.Label($"Mouse Look: {(isMouseLookActive ? "ACTIVE" : "Inactive")}");
        GUILayout.Label($"Mouse in Game View: {(IsMouseInGameView() ? "Yes" : "No")}");
        GUILayout.Label("Right-click + drag to look around");
        if (enableMovement)
        {
            GUILayout.Label("WASD: Move, Shift: Sprint");
            GUILayout.Label("Space: Up, Ctrl: Down");
        }
        GUILayout.EndArea();
    }
    
    // Editor-only styles
    private GUIStyle boldLabel
    {
        get
        {
            if (_boldLabel == null)
            {
                _boldLabel = new GUIStyle(GUI.skin.label);
                _boldLabel.fontSize = 14;
                _boldLabel.fontStyle = FontStyle.Bold;
                _boldLabel.normal.textColor = Color.yellow;
            }
            return _boldLabel;
        }
    }
    
    private GUIStyle _boldLabel;
    
#else
    // In builds, this component does nothing
    void Start()
    {
        // Disable this component in builds
        enabled = false;
    }
#endif
} 