using UnityEngine;
using UnityEngine.XR;

namespace StereoVisionOS
{
    public class Stereo360PanoramaSetup : MonoBehaviour
    {
        [SerializeField] private Texture2D panoramaTexture;
        [SerializeField] private bool leftEyeOnTop = true;
        [SerializeField] private bool debugMode = false;
        [SerializeField] private float sphereRadius = 50f;
        
        [ContextMenu("Create Panorama Sphere")]
        public void CreatePanoramaSphere()
        {
            // Create a sphere for the panorama
            var sphereObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereObject.name = "Stereo360PanoramaSphere";
            sphereObject.transform.SetParent(transform);
            sphereObject.transform.localPosition = Vector3.zero;
            sphereObject.transform.localScale = new Vector3(sphereRadius, sphereRadius, sphereRadius);
            
            // Flip the normals inward so we can see the panorama from inside
            FlipSphereNormals(sphereObject);
            
            // Add the controller component
            var controller = sphereObject.AddComponent<Stereo360PanoramaController>();
            
            // Set properties via reflection to make sure the serialized fields are set
            var textureField = typeof(Stereo360PanoramaController).GetField("panoramaTexture", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var leftEyeOnTopField = typeof(Stereo360PanoramaController).GetField("leftEyeOnTop", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var debugModeField = typeof(Stereo360PanoramaController).GetField("debugMode", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (textureField != null) textureField.SetValue(controller, panoramaTexture);
            if (leftEyeOnTopField != null) leftEyeOnTopField.SetValue(controller, leftEyeOnTop);
            if (debugModeField != null) debugModeField.SetValue(controller, debugMode);
            
            // Call methods for setting properties
            controller.SetPanoramaTexture(panoramaTexture);
            controller.SetLeftEyeOnTop(leftEyeOnTop);
            controller.SetDebugMode(debugMode);
            
            Debug.Log("Created stereo panorama sphere with texture: " + (panoramaTexture != null ? panoramaTexture.name : "null"));
        }
        
        private void FlipSphereNormals(GameObject sphere)
        {
            Mesh mesh = sphere.GetComponent<MeshFilter>().mesh;
            
            // Reverse the order of vertices for each triangle to flip normals
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int temp = triangles[i];
                triangles[i] = triangles[i + 2];
                triangles[i + 2] = temp;
            }
            
            mesh.triangles = triangles;
            
            // Reverse the actual normal directions
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = -normals[i];
            }
            
            mesh.normals = normals;
        }
    }
} 