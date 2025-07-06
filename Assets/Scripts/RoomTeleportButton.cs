using UnityEngine;

public class RoomTeleportButton : MonoBehaviour
{
    public RoomData roomData;

    // Call this from a UnityEvent or XR interaction event
    public void TeleportToRoom()
    {
        Debug.Log($"[RoomTeleportButton] TeleportToRoom called on {gameObject.name} with RoomData: " + (roomData != null ? roomData.roomName : "null"));
        
        // First teleport to the room
        if (roomData != null && RoomManager.Instance != null)
        {
            RoomManager.Instance.TeleportToRoom(roomData);
        }
        else if (roomData == null)
        {
            Debug.LogWarning($"[RoomTeleportButton] No RoomData assigned on {gameObject.name}!");
        }
        else if (RoomManager.Instance == null)
        {
            Debug.LogWarning("[RoomTeleportButton] No RoomManager instance found in scene!");
        }
        
        // Then close the MapMain UI like the close button does
        CloseMapMainUI();
    }
    
    private void CloseMapMainUI()
    {
        // Only close MapMain UI if this teleport button is part of the MapMain interface
        if (!IsPartOfMapMainUI())
        {
            Debug.Log("[RoomTeleportButton] Teleport button is not part of MapMain UI - skipping UI close");
            return;
        }
        
        // Find the XRUIFlowManager in the scene and close the map view
        XRUIFlowManager uiFlowManager = FindObjectOfType<XRUIFlowManager>();
        if (uiFlowManager != null)
        {
            // Call the same logic as the close button
            uiFlowManager.CloseMapView();
            Debug.Log("[RoomTeleportButton] Closed MapMain UI after teleport");
        }
        else
        {
            Debug.LogWarning("[RoomTeleportButton] XRUIFlowManager not found - cannot close MapMain UI");
        }
    }
    
    private bool IsPartOfMapMainUI()
    {
        // Check if this teleport button or any of its parents is on the UI layer
        // MapMain is on UI layer, but sphere teleport buttons are not
        Transform current = transform;
        while (current != null)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (current.gameObject.layer == uiLayer)
            {
                Debug.Log($"[RoomTeleportButton] Found UI layer on: {current.name} - this is a MapMain UI teleport button");
                return true;
            }
            current = current.parent;
        }
        
        Debug.Log($"[RoomTeleportButton] No UI layer found in hierarchy - this is a sphere/room view teleport button (layer: {LayerMask.LayerToName(gameObject.layer)})");
        return false;
    }
} 