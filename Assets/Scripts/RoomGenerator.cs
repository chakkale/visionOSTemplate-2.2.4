using UnityEngine;
using UnityEditor;
using System.IO;

public class RoomGenerator : MonoBehaviour
{
    [System.Serializable]
    public class RoomData
    {
        public string roomName;
        public string textureGuid;
        public Texture2D texture;
    }

    public RoomData[] roomsToGenerate = new RoomData[]
    {
        new RoomData { roomName = "E-4D-C06", textureGuid = "eeef1d9d7b4954571b8b62a97d21c0a3" },
        new RoomData { roomName = "E-4D-C07", textureGuid = "466ad45d4175f41c395b8fc111bd3c68" },
        new RoomData { roomName = "E-4D-C08", textureGuid = "9afcf7c5bdd6943b5a2d062a684bbf88" },
        new RoomData { roomName = "E-4D-C09", textureGuid = "9410cff303e5a45b98ba42c4e76965a5" },
        new RoomData { roomName = "E-4D-C10", textureGuid = "98140b3a637ce4079ab7164cb2b2dc2b" },
        new RoomData { roomName = "E-4D-C11", textureGuid = "5a97b652d994345c2809ba76cbcfaf4b" },
        new RoomData { roomName = "E-4D-C12", textureGuid = "84cb67c1dcba048959c07e417d804841" }
    };

    public GameObject templatePrefab; // Drag the E-4D-C05 prefab here
    public ScriptableObject templateScriptableObject; // Drag the E-4D-C05 asset here

#if UNITY_EDITOR
    [ContextMenu("Generate Missing Rooms")]
    public void GenerateMissingRooms()
    {
        string basePath = "Assets/Wizio/Rooms/E-4D/";
        
        foreach (var roomData in roomsToGenerate)
        {
            // Load texture by GUID
            string texturePath = AssetDatabase.GUIDToAssetPath(roomData.textureGuid);
            roomData.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            
            if (roomData.texture == null)
            {
                Debug.LogError($"Could not load texture for {roomData.roomName} with GUID: {roomData.textureGuid}");
                continue;
            }

            // Generate ScriptableObject
            GenerateScriptableObject(roomData, basePath);
            
            // Generate Prefab
            GeneratePrefab(roomData, basePath);
        }
        
        AssetDatabase.Refresh();
        Debug.Log("Room generation complete!");
    }

    private void GenerateScriptableObject(RoomData roomData, string basePath)
    {
        string assetPath = basePath + roomData.roomName + ".asset";
        
        // Check if it already exists
        if (File.Exists(assetPath))
        {
            Debug.Log($"ScriptableObject {roomData.roomName} already exists, skipping...");
            return;
        }

        // Create a copy of the template
        var newAsset = Instantiate(templateScriptableObject);
        
        // Use reflection to set the properties
        var serializedObject = new SerializedObject(newAsset);
        serializedObject.FindProperty("roomName").stringValue = roomData.roomName;
        serializedObject.FindProperty("roomTexture").objectReferenceValue = roomData.texture;
        serializedObject.ApplyModifiedProperties();
        
        // Save the asset
        AssetDatabase.CreateAsset(newAsset, assetPath);
        Debug.Log($"Created ScriptableObject: {assetPath}");
    }

    private void GeneratePrefab(RoomData roomData, string basePath)
    {
        string prefabPath = basePath + roomData.roomName + ".prefab";
        
        // Check if it already exists
        if (File.Exists(prefabPath))
        {
            Debug.Log($"Prefab {roomData.roomName} already exists, skipping...");
            return;
        }

        // Instantiate the template prefab
        GameObject newPrefabInstance = Instantiate(templatePrefab);
        newPrefabInstance.name = roomData.roomName;
        
        // Update the room data reference in the prefab
        var roomTeleportButtons = newPrefabInstance.GetComponentsInChildren<RoomTeleportButton>();
        if (roomTeleportButtons != null && roomTeleportButtons.Length > 0)
        {
            // Load the newly created scriptable object
            string assetPath = basePath + roomData.roomName + ".asset";
            var roomScriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            
            foreach (var button in roomTeleportButtons)
            {
                // Use reflection to set the roomData property
                var serializedObject = new SerializedObject(button);
                serializedObject.FindProperty("roomData").objectReferenceValue = roomScriptableObject;
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        // Save as prefab
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(newPrefabInstance, prefabPath);
        DestroyImmediate(newPrefabInstance);
        
        Debug.Log($"Created Prefab: {prefabPath}");
    }
#endif
} 