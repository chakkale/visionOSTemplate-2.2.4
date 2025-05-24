using UnityEngine;
using UnityEngine.XR;

namespace StereoVisionOS
{
    [RequireComponent(typeof(MeshRenderer))]
    public class Stereo360PanoramaController : MonoBehaviour
    {
        [SerializeField] private Texture2D panoramaTexture;
        [SerializeField] private bool leftEyeOnTop = true;
        [SerializeField] private bool debugMode = false;
        
        private Material panoramaMaterial;
        private MeshRenderer meshRenderer;
        
        // Property IDs for faster access
        private int mainTexPropertyID;
        private int leftEyeOnTopPropertyID;
        private int debugModePropertyID;
        private int eyeIndexPropertyID;
        
        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            
            // Create our dynamic material based on the RealityKit compatible shader
            panoramaMaterial = new Material(Shader.Find("Stereoscopic/Stereo360Panorama_VerticalStack_RealityKit"));
            
            // Cache property IDs
            mainTexPropertyID = Shader.PropertyToID("_MainTex");
            leftEyeOnTopPropertyID = Shader.PropertyToID("_LeftEyeOnTop");
            debugModePropertyID = Shader.PropertyToID("_DebugMode");
            eyeIndexPropertyID = Shader.PropertyToID("_EyeIndex");
            
            // Assign the material to the renderer
            meshRenderer.material = panoramaMaterial;
            
            // Set initial properties
            UpdateMaterialProperties();
        }
        
        private void Update()
        {
            // Update eye index if in XR mode
            if (XRSettings.isDeviceActive)
            {
                int currentEye = (int)Camera.current?.stereoTargetEye;
                panoramaMaterial.SetFloat(eyeIndexPropertyID, currentEye);
            }
        }
        
        public void SetPanoramaTexture(Texture2D texture)
        {
            panoramaTexture = texture;
            UpdateMaterialProperties();
        }
        
        public void SetLeftEyeOnTop(bool isOnTop)
        {
            leftEyeOnTop = isOnTop;
            UpdateMaterialProperties();
        }
        
        public void SetDebugMode(bool debug)
        {
            debugMode = debug;
            UpdateMaterialProperties();
        }
        
        private void UpdateMaterialProperties()
        {
            if (panoramaMaterial != null)
            {
                panoramaMaterial.SetTexture(mainTexPropertyID, panoramaTexture);
                panoramaMaterial.SetFloat(leftEyeOnTopPropertyID, leftEyeOnTop ? 1.0f : 0.0f);
                panoramaMaterial.SetFloat(debugModePropertyID, debugMode ? 1.0f : 0.0f);
            }
        }
        
        private void OnValidate()
        {
            UpdateMaterialProperties();
        }
    }
} 