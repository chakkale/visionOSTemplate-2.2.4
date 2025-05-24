using UnityEngine;

[CreateAssetMenu(fileName = "RoomData", menuName = "Archviz/Room Data", order = 1)]
public class RoomData : ScriptableObject
{
    public string roomName;
    public Texture2D roomTexture;
    public GameObject roomPrefab;
} 