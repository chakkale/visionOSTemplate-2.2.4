using UnityEngine;
using DG.Tweening;

public class AnimateAndDestroy : MonoBehaviour
{
    public Vector3 targetPosition;
    public float moveDuration = 0.5f;
    public Ease moveEase = Ease.InOutSine;

    public void AnimateOutAndDestroy()
    {
        transform.DOMove(targetPosition, moveDuration)
            .SetEase(moveEase)
            .OnComplete(() => Destroy(gameObject));
    }
} 