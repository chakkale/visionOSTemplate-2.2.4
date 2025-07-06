using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

public class Advanced3DTextButtonAnimator : MonoBehaviour
{
    [Header("Sprite Animation Settings")]
    [SerializeField] private float spriteFadeDuration = 1.0f;
    [SerializeField] private Ease spriteEase = Ease.OutQuart;
    [SerializeField] private float spriteScaleStartMultiplier = 0.8f; // Start at 80% of original size
    [SerializeField] private bool enableSpriteScaleAnimation = true;
    
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
    private Vector3 originalSpriteScale;
    private bool isAnimating = false;
    private Material spriteMaterial;
    private float originalMaterialAlpha;
    
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
            originalSpriteScale = targetSprite.transform.localScale;
            
            // Get the material and store its original alpha
            spriteMaterial = targetSprite.material;
            if (spriteMaterial != null)
            {
                // Check if material has alpha property
                if (spriteMaterial.HasProperty("_Color"))
                {
                    originalMaterialAlpha = spriteMaterial.color.a;
                }
                else if (spriteMaterial.HasProperty("_BaseColor"))
                {
                    originalMaterialAlpha = spriteMaterial.GetColor("_BaseColor").a;
                }
                else
                {
                    originalMaterialAlpha = 1f; // Default to opaque
                }
            }
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
            // Reset material alpha if available
            if (spriteMaterial != null)
            {
                SetMaterialAlpha(0f);
            }
            else
            {
                // Fallback to sprite color alpha
                Color transparentColor = originalSpriteColor;
                transparentColor.a = 0f;
                targetSprite.color = transparentColor;
            }
            
            // Reset sprite scale to a subtle smaller size for elegant scale-up effect
            if (enableSpriteScaleAnimation)
            {
                targetSprite.transform.localScale = originalSpriteScale * spriteScaleStartMultiplier;
            }
            else
            {
                targetSprite.transform.localScale = originalSpriteScale;
            }
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
        // Use material alpha animation if available, otherwise fallback to sprite color
        if (spriteMaterial != null)
        {
            // Start with transparent material
            SetMaterialAlpha(0f);
            
            // Animate material alpha to original value
            DOVirtual.Float(0f, originalMaterialAlpha, spriteFadeDuration, SetMaterialAlpha)
                .SetEase(spriteEase);
        }
        else
        {
            // Fallback to sprite color animation
            Color transparentColor = originalSpriteColor;
            transparentColor.a = 0f;
            targetSprite.color = transparentColor;
            
            // Fade in the sprite smoothly
            targetSprite.DOColor(originalSpriteColor, spriteFadeDuration)
                .SetEase(spriteEase);
        }
        
        // Simultaneously animate sprite scale from smaller to original size for elegant effect
        if (enableSpriteScaleAnimation)
        {
            targetSprite.transform.DOScale(originalSpriteScale, spriteFadeDuration)
                .SetEase(spriteEase)
                .SetTarget(this);
        }
        
        // Wait for both animations to complete
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
    
    private void SetMaterialAlpha(float alpha)
    {
        if (spriteMaterial == null) return;
        
        // Try different material properties depending on shader type
        if (spriteMaterial.HasProperty("_Color"))
        {
            Color color = spriteMaterial.color;
            color.a = alpha;
            spriteMaterial.color = color;
        }
        else if (spriteMaterial.HasProperty("_BaseColor"))
        {
            Color color = spriteMaterial.GetColor("_BaseColor");
            color.a = alpha;
            spriteMaterial.SetColor("_BaseColor", color);
        }
        else if (spriteMaterial.HasProperty("_TintColor"))
        {
            Color color = spriteMaterial.GetColor("_TintColor");
            color.a = alpha;
            spriteMaterial.SetColor("_TintColor", color);
        }
        else if (spriteMaterial.HasProperty("_Alpha"))
        {
            spriteMaterial.SetFloat("_Alpha", alpha);
        }
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
        {
            if (spriteMaterial != null)
            {
                // Update material color
                if (spriteMaterial.HasProperty("_Color"))
                {
                    spriteMaterial.color = newColor;
                }
                else if (spriteMaterial.HasProperty("_BaseColor"))
                {
                    spriteMaterial.SetColor("_BaseColor", newColor);
                }
                // Also update the original material alpha
                originalMaterialAlpha = newColor.a;
            }
            else
            {
                // Fallback to sprite color
                targetSprite.color = newColor;
            }
        }
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
    
    public void SetSpriteScaleAnimation(bool enabled, float startMultiplier = 0.8f)
    {
        enableSpriteScaleAnimation = enabled;
        spriteScaleStartMultiplier = startMultiplier;
    }
} 