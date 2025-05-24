using UnityEngine;

namespace StereoVisionOS.Samples
{
    public class SampleUsage : MonoBehaviour
    {
        [SerializeField] private Texture2D stereo360Texture;
        [SerializeField] private bool createOnStart = true;

        private GameObject sphereObject;

        private void Start()
        {
            if (createOnStart)
            {
                CreatePanorama();
            }
        }

        [ContextMenu("Create Panorama")]
        public void CreatePanorama()
        {
            // Check if we have a valid texture
            if (stereo360Texture == null)
            {
                Debug.LogError("No stereo 360 texture assigned! Please assign a texture and try again.");
                return;
            }

            // Create a sphere for the panorama
            sphereObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereObject.name = "Stereo360Panorama";
            sphereObject.transform.SetParent(transform);
            sphereObject.transform.localPosition = Vector3.zero;
            sphereObject.transform.localScale = new Vector3(50f, 50f, 50f);

            // Flip the normals to see the panorama from inside
            FlipSphereNormals(sphereObject);

            // Assign the texture to the sphere's material
            var renderer = sphereObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.SetTexture("_MainTex", stereo360Texture);
            }

            Debug.Log("Created stereo panorama with texture: " + stereo360Texture.name);
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