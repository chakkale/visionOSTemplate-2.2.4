using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

public class Advanced3DTextButtonAnimator : MonoBehaviour
{
    [Header("Sprite Animation Settings")]
    [SerializeField] private float spriteFadeDuration = 1.0f;
    [SerializeField] private Ease spriteEase = Ease.OutQuart;
    
    [Header("Button Animation Settings")]
    [SerializeField] private float buttonDelayAfterSprite = 0.3f;
    [SerializeField] private float buttonScaleDuration = 0.8f;
    [SerializeField] private Ease buttonScaleEase = Ease.OutBack;
    
    [Header("Components")]
    [SerializeField] private SpriteRenderer targetSprite;
    [SerializeField] private GameObject targetButton;
    
    [Header("Auto Settings")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float initialDelay = 0f;
    
    private Color originalSpriteColor;
    private Vector3 originalButtonScale;
    private bool isAnimating = false;
    
    private void Awake()
    {
        SetupComponents();
        StoreOriginalValues();
    }
    
    private void Start()
    {
        // Initialize to hidden state immediately
        ResetToInitialState();
        
        if (playOnStart)
        {
            if (initialDelay > 0)
                DOVirtual.DelayedCall(initialDelay, PlayAnimation);
            else
                PlayAnimation();
        }
    }
    
    private void SetupComponents()
    {
        // Auto-find components
        if (targetSprite == null)
            targetSprite = GetComponentInChildren<SpriteRenderer>();
            
        if (targetButton == null)
        {
            // Look for any child with a renderer that's not the sprite
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.gameObject != targetSprite?.gameObject)
                {
                    targetButton = renderer.gameObject;
                    break;
                }
            }
        }
    }
    
    private void StoreOriginalValues()
    {
        if (targetSprite != null)
        {
            originalSpriteColor = targetSprite.color;
        }
        
        if (targetButton != null)
        {
            originalButtonScale = targetButton.transform.localScale;
        }
    }
    
    [ContextMenu("Play Animation")]
    public void PlayAnimation()
    {
        if (isAnimating)
        {
            StopAnimation();
        }
        
        StartCoroutine(ExecuteAnimationSequence());
    }
    
    [ContextMenu("Stop Animation")]
    public void StopAnimation()
    {
        DOTween.Kill(this);
        isAnimating = false;
    }
    
    [ContextMenu("Reset to Initial State")]
    public void ResetToInitialState()
    {
        StopAnimation();
        
        // Reset sprite
        if (targetSprite != null)
        {
            Color transparentColor = originalSpriteColor;
            transparentColor.a = 0f;
            targetSprite.color = transparentColor;
        }
        
        // Reset button
        if (targetButton != null)
        {
            targetButton.transform.localScale = Vector3.zero;
        }
    }
    
    private IEnumerator ExecuteAnimationSequence()
    {
        isAnimating = true;
        ResetToInitialState();
        
        // Phase 1: Sprite Animation
        if (targetSprite != null)
        {
            yield return StartCoroutine(AnimateSpriteFadeIn());
        }
        
        // Phase 2: Button Animation
        yield return new WaitForSeconds(buttonDelayAfterSprite);
        
        if (targetButton != null)
        {
            AnimateButtonScale();
        }
        
        isAnimating = false;
    }
    
    private IEnumerator AnimateSpriteFadeIn()
    {
        // Start with transparent color
        Color transparentColor = originalSpriteColor;
        transparentColor.a = 0f;
        targetSprite.color = transparentColor;
        
        // Fade in the sprite smoothly
        targetSprite.DOColor(originalSpriteColor, spriteFadeDuration)
            .SetEase(spriteEase);
        
        // Wait for the fade to complete
        yield return new WaitForSeconds(spriteFadeDuration);
    }
    
    private void AnimateButtonScale()
    {
        if (targetButton == null) return;
        
        // Start from scale zero
        targetButton.transform.localScale = Vector3.zero;
        
        // Elegantly scale up to original size
        targetButton.transform.DOScale(originalButtonScale, buttonScaleDuration)
            .SetEase(buttonScaleEase)
            .SetTarget(this);
    }
    
    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
    
    // Public API methods
    public void SetSprite(Sprite newSprite)
    {
        if (targetSprite != null)
        {
            targetSprite.sprite = newSprite;
        }
    }
    
    public void SetSpriteColor(Color newColor)
    {
        originalSpriteColor = newColor;
        if (!isAnimating && targetSprite != null)
            targetSprite.color = newColor;
    }
    
    public void SetAnimationSpeed(float speedMultiplier)
    {
        spriteFadeDuration /= speedMultiplier;
        buttonScaleDuration /= speedMultiplier;
    }
    
    public void PlayAnimationWithDelay(float delay)
    {
        DOVirtual.DelayedCall(delay, PlayAnimation);
    }
} 