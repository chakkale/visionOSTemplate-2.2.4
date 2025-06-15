using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

public class Advanced3DTextButtonAnimator : MonoBehaviour
{
    [Header("Text Animation Settings")]
    [SerializeField] private float textFadeDuration = 1.0f;
    [SerializeField] private Ease textEase = Ease.OutQuart;
    
    [Header("Button Animation Settings")]
    [SerializeField] private float buttonDelayAfterText = 0.3f;
    [SerializeField] private float buttonScaleDuration = 0.8f;
    [SerializeField] private Ease buttonScaleEase = Ease.OutBack;
    
    [Header("Components")]
    [SerializeField] private TextMeshPro targetText;
    [SerializeField] private GameObject targetButton;
    
    [Header("Auto Settings")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float initialDelay = 0f;
    
    private string originalText;
    private Color originalTextColor;
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
        if (targetText == null)
            targetText = GetComponentInChildren<TextMeshPro>();
            
        if (targetButton == null)
        {
            // Look for any child with a renderer that's not the text
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.gameObject != targetText?.gameObject)
                {
                    targetButton = renderer.gameObject;
                    break;
                }
            }
        }
    }
    
    private void StoreOriginalValues()
    {
        if (targetText != null)
        {
            originalText = targetText.text;
            originalTextColor = targetText.color;
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
        
        // Reset text
        if (targetText != null)
        {
            targetText.text = "";
            Color transparentColor = originalTextColor;
            transparentColor.a = 0f;
            targetText.color = transparentColor;
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
        
        // Phase 1: Text Animation
        if (targetText != null && !string.IsNullOrEmpty(originalText))
        {
            yield return StartCoroutine(AnimateTextFadeIn());
        }
        
        // Phase 2: Button Animation
        yield return new WaitForSeconds(buttonDelayAfterText);
        
        if (targetButton != null)
        {
            AnimateButtonScale();
        }
        
        isAnimating = false;
    }
    
    private IEnumerator AnimateTextFadeIn()
    {
        // Set the text content and start with transparent color
        targetText.text = originalText;
        Color transparentColor = originalTextColor;
        transparentColor.a = 0f;
        targetText.color = transparentColor;
        
        // Fade in the whole text smoothly
        targetText.DOColor(originalTextColor, textFadeDuration)
            .SetEase(textEase);
        
        // Wait for the fade to complete
        yield return new WaitForSeconds(textFadeDuration);
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
    public void SetText(string newText)
    {
        originalText = newText;
        if (!isAnimating && targetText != null)
            targetText.text = newText;
    }
    
    public void SetAnimationSpeed(float speedMultiplier)
    {
        textFadeDuration /= speedMultiplier;
        buttonScaleDuration /= speedMultiplier;
    }
    
    public void PlayAnimationWithDelay(float delay)
    {
        DOVirtual.DelayedCall(delay, PlayAnimation);
    }
} 