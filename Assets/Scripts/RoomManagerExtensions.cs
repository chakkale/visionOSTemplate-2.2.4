using UnityEngine;

/// <summary>
/// Extension methods and helper class for RoomManager to expose current room information
/// </summary>
public static class RoomManagerExtensions
{
    /// <summary>
    /// Get the name of the currently active room
    /// </summary>
    public static string GetCurrentRoomName(this RoomManager roomManager)
    {
        // This would need to be implemented based on RoomManager's internal state
        // For now, return a placeholder
        return "Current Room"; // TODO: Implement based on RoomManager's current room tracking
    }
    
    /// <summary>
    /// Get the index of the currently active room
    /// </summary>
    public static int GetCurrentRoomIndex(this RoomManager roomManager)
    {
        // This would need to be implemented based on RoomManager's internal state
        return -1; // TODO: Implement based on RoomManager's current room tracking
    }
    
    /// <summary>
    /// Check if RoomManager has an active room
    /// </summary>
    public static bool HasActiveRoom(this RoomManager roomManager)
    {
        return GetCurrentRoomIndex(roomManager) >= 0;
    }
}

/// <summary>
/// Simple data structure to hold room information for UI display
/// </summary>
[System.Serializable]
public class UIRoomInfo
{
    public string roomName;
    public int roomIndex;
    public bool isActive;
    
    public UIRoomInfo(string name, int index, bool active = false)
    {
        roomName = name;
        roomIndex = index;
        isActive = active;
    }
} 