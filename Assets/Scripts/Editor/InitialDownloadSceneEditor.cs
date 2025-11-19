using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Custom editor for InitialDownloadScene to auto-populate RoomData array
/// </summary>
[CustomEditor(typeof(InitialDownloadScene))]
public class InitialDownloadSceneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        InitialDownloadScene script = (InitialDownloadScene)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Room Data Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Auto-Find All RoomData Assets"))
        {
            // Find all RoomData assets in the project
            string[] guids = AssetDatabase.FindAssets("t:RoomData");
            RoomData[] allRooms = new RoomData[guids.Length];
            
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                allRooms[i] = AssetDatabase.LoadAssetAtPath<RoomData>(path);
            }
            
            // Sort by name for consistency
            allRooms = allRooms.OrderBy(r => r.roomName).ToArray();
            
            // Set the array via SerializedObject for proper undo support
            SerializedProperty roomsProp = serializedObject.FindProperty("allRooms");
            roomsProp.arraySize = allRooms.Length;
            
            for (int i = 0; i < allRooms.Length; i++)
            {
                roomsProp.GetArrayElementAtIndex(i).objectReferenceValue = allRooms[i];
            }
            
            serializedObject.ApplyModifiedProperties();
            
            Debug.Log($"[InitialDownloadSceneEditor] Found and assigned {allRooms.Length} RoomData assets");
            EditorUtility.SetDirty(target);
        }
        
        EditorGUILayout.HelpBox(
            "Click the button above to automatically find and assign all RoomData assets in the project. " +
            "This will populate the 'All Rooms' array for texture preloading.",
            MessageType.Info
        );
    }
}
