using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RacingLineThrobber : MonoBehaviour
{
    [Header("Racing Line Settings")]
    [SerializeField] private Image trackBackgroundImage;
    [SerializeField] private Image racingLineImage;
    [SerializeField] private Texture2D trackTexture;
    [SerializeField] private Texture2D racingLineTexture;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private AnimationType animationType = AnimationType.FillClockwise;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool loop = true;
    
    [Header("Progress Settings")]
    [SerializeField] private bool useManualProgress = false;
    [SerializeField, Range(0f, 1f)] private float manualProgress = 0f;
    
    private bool isAnimating = false;
    private float currentProgress = 0f;
    private Coroutine animationCoroutine;
    
    public enum AnimationType
    {
        FillClockwise,
        FillCounterClockwise,
        FillRadial,
        Pulse,
        Rotate
    }
    
    // Events
    public System.Action OnLoadingComplete;
    public System.Action<float> OnProgressChanged;
    
    // Properties
    public bool IsAnimating => isAnimating;
    public float CurrentProgress => currentProgress;
    
    void Start()
    {
        SetupImages();
    }
    
    void OnEnable()
    {
        if (autoStart)
        {
            StartLoading();
        }
    }
    
    void Update()
    {
        if (useManualProgress && !isAnimating)
        {
            SetProgress(manualProgress);
        }
    }
    
    void SetupImages()
    {
        // Setup track background
        if (trackBackgroundImage != null && trackTexture != null)
        {
            Sprite trackSprite = Sprite.Create(trackTexture, 
                new Rect(0, 0, trackTexture.width, trackTexture.height), 
                new Vector2(0.5f, 0.5f));
            trackBackgroundImage.sprite = trackSprite;
        }
        
        // Setup racing line
        if (racingLineImage != null && racingLineTexture != null)
        {
            Sprite racingLineSprite = Sprite.Create(racingLineTexture, 
                new Rect(0, 0, racingLineTexture.width, racingLineTexture.height), 
                new Vector2(0.5f, 0.5f));
            racingLineImage.sprite = racingLineSprite;
            
            // Set initial state based on animation type
            SetupInitialState();
        }
        else if (racingLineImage != null)
        {
            // If no texture is provided, still setup the initial state
            SetupInitialState();
        }
    }
    
    void SetupInitialState()
    {
        if (racingLineImage == null) return;
        
        switch (animationType)
        {
            case AnimationType.FillClockwise:
            case AnimationType.FillCounterClockwise:
                racingLineImage.type = Image.Type.Filled;
                racingLineImage.fillMethod = Image.FillMethod.Radial360;
                racingLineImage.fillClockwise = (animationType == AnimationType.FillClockwise);
                racingLineImage.fillAmount = 0f;
                break;
                
            case AnimationType.FillRadial:
                racingLineImage.type = Image.Type.Filled;
                racingLineImage.fillMethod = Image.FillMethod.Radial360;
                racingLineImage.fillOrigin = (int)Image.Origin360.Top;
                racingLineImage.fillAmount = 0f;
                break;
                
            case AnimationType.Pulse:
                racingLineImage.type = Image.Type.Simple;
                Color currentColor = racingLineImage.color;
                racingLineImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0f);
                break;
                
            case AnimationType.Rotate:
                racingLineImage.type = Image.Type.Simple;
                racingLineImage.transform.rotation = Quaternion.identity;
                break;
        }
    }
    
    public void StartLoading()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        isAnimating = true;
        animationCoroutine = StartCoroutine(AnimateLoading());
    }
    
    public void StopLoading()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        
        isAnimating = false;
    }
    
    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        currentProgress = progress;
        
        if (!isAnimating || useManualProgress)
        {
            UpdateVisualProgress(progress);
        }
        
        OnProgressChanged?.Invoke(progress);
        
        if (progress >= 1f && !isAnimating)
        {
            OnLoadingComplete?.Invoke();
        }
    }
    
    void UpdateVisualProgress(float progress)
    {
        if (racingLineImage == null) return;
        
        switch (animationType)
        {
            case AnimationType.FillClockwise:
            case AnimationType.FillCounterClockwise:
            case AnimationType.FillRadial:
                if (racingLineImage.type == Image.Type.Filled)
                {
                    racingLineImage.fillAmount = progress;
                }
                break;
                
            case AnimationType.Pulse:
                float alpha = Mathf.PingPong(Time.time * animationSpeed, 1f);
                Color currentColor = racingLineImage.color;
                racingLineImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha * progress);
                break;
                
            case AnimationType.Rotate:
                racingLineImage.transform.rotation = Quaternion.Euler(0, 0, progress * 360f);
                break;
        }
    }
    
    IEnumerator AnimateLoading()
    {
        currentProgress = 0f;
        
        do
        {
            while (currentProgress < 1f)
            {
                if (!useManualProgress)
                {
                    currentProgress += Time.deltaTime * animationSpeed;
                    currentProgress = Mathf.Clamp01(currentProgress);
                    UpdateVisualProgress(currentProgress);
                    OnProgressChanged?.Invoke(currentProgress);
                }
                
                yield return null;
            }
            
            OnLoadingComplete?.Invoke();
            
            if (loop)
            {
                currentProgress = 0f;
                yield return new WaitForSeconds(0.5f); // Brief pause before restarting
            }
            
        } while (loop);
        
        isAnimating = false;
    }
    
    // Public methods for external control
    public void SetTrackTexture(Texture2D texture)
    {
        trackTexture = texture;
        if (trackBackgroundImage != null)
        {
            Sprite trackSprite = Sprite.Create(texture, 
                new Rect(0, 0, texture.width, texture.height), 
                new Vector2(0.5f, 0.5f));
            trackBackgroundImage.sprite = trackSprite;
        }
    }
    
    public void SetRacingLineTexture(Texture2D texture)
    {
        racingLineTexture = texture;
        if (racingLineImage != null)
        {
            Sprite racingLineSprite = Sprite.Create(texture, 
                new Rect(0, 0, texture.width, texture.height), 
                new Vector2(0.5f, 0.5f));
            racingLineImage.sprite = racingLineSprite;
            SetupInitialState();
        }
    }
    
    public void SetAnimationType(AnimationType newType)
    {
        animationType = newType;
        SetupInitialState();
    }
    
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
    }
    
    public void ResetThrobber()
    {
        StopLoading();
        currentProgress = 0f;
        SetupInitialState();
        UpdateVisualProgress(0f);
    }
    
    public void SetLoop(bool shouldLoop)
    {
        loop = shouldLoop;
    }
    
    void OnValidate()
    {
        // Update the animation type in editor when changed
        if (Application.isPlaying && racingLineImage != null)
        {
            SetupInitialState();
        }
    }
} 