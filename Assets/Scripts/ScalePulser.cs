using UnityEngine;
using DG.Tweening;

public class ScalePulser : MonoBehaviour
{
    [Header("Scale Settings")]
    [SerializeField] private Vector3 minScale = new Vector3(0.8f, 0.8f, 0.8f);
    [SerializeField] private Vector3 maxScale = new Vector3(1.2f, 1.2f, 1.2f);
    
    [Header("Animation Settings")]
    [SerializeField] private float duration = 1f;
    [SerializeField] private Ease easeType = Ease.InOutSine;
    
    private Sequence scaleSequence;
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void Start()
    {
        CreateScaleSequence();
    }

    private void OnEnable()
    {
        CreateScaleSequence();
    }

    private void OnDisable()
    {
        // Kill the sequence and reset scale to originalScale
        scaleSequence?.Kill();
        transform.localScale = originalScale;
    }

    private void CreateScaleSequence()
    {
        // Kill any existing sequence
        scaleSequence?.Kill();
        
        // Create new sequence
        scaleSequence = DOTween.Sequence();
        
        // Add scale up and down animations to sequence
        scaleSequence.Append(transform.DOScale(Vector3.Scale(originalScale, maxScale), duration).SetEase(easeType))
                    .Append(transform.DOScale(Vector3.Scale(originalScale, minScale), duration).SetEase(easeType))
                    .SetLoops(-1); // Infinite loops
    }

    private void OnDestroy()
    {
        // Clean up the sequence when the object is destroyed
        scaleSequence?.Kill();
    }

    public void RestartPulse()
    {
        scaleSequence?.Kill();
        transform.localScale = originalScale;
        CreateScaleSequence();
    }

    public Vector3 OriginalScale => originalScale;
} 