using UnityEngine;

public class RoomTeleportButton : MonoBehaviour
{
    public RoomData roomData;

    // Call this from a UnityEvent or XR interaction event
    public void TeleportToRoom()
    {
        Debug.Log($"[RoomTeleportButton] TeleportToRoom called on {gameObject.name} with RoomData: " + (roomData != null ? roomData.roomName : "null"));
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
    }
} 