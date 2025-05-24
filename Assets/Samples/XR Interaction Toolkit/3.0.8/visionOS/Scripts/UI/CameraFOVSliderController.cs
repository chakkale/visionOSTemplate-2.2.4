using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.VisionOS;

public class CameraFOVSliderController : MonoBehaviour
{
    [Header("References")]
    public SliderComponent slider; // Assign in inspector
    public Camera targetCamera;    // Assign in inspector

    [Header("FOV Range")]
    public float minFOV = 30f;
    public float maxFOV = 90f;

    void Start()
    {
        if (slider != null && targetCamera != null)
        {
            slider.OnSliderValueChanged.AddListener(OnSliderValueChanged);
            // Set initial FOV based on slider's initial value
            OnSliderValueChanged(1f - slider.InitialSliderPercent);
        }
    }

    void OnSliderValueChanged(float value)
    {
        // value is between 0 and 1; map to FOV range
        float fov = Mathf.Lerp(maxFOV, minFOV, value);
        targetCamera.fieldOfView = fov;
    }
} 