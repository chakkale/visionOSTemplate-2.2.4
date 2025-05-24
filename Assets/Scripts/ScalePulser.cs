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

    private void Start()
    {
        CreateScaleSequence();
    }

    private void CreateScaleSequence()
    {
        // Kill any existing sequence
        scaleSequence?.Kill();
        
        // Create new sequence
        scaleSequence = DOTween.Sequence();
        
        // Add scale up and down animations to sequence
        scaleSequence.Append(transform.DOScale(maxScale, duration).SetEase(easeType))
                    .Append(transform.DOScale(minScale, duration).SetEase(easeType))
                    .SetLoops(-1); // Infinite loops
    }

    private void OnDestroy()
    {
        // Clean up the sequence when the object is destroyed
        scaleSequence?.Kill();
    }
} 