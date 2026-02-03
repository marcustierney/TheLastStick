using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AfterImage : MonoBehaviour
{
    [SerializeField] private Movement movement;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("After Image Settings")]
    [SerializeField] private float afterImageInterval = 0.05f; // Time between creating after images
    [SerializeField] private int maxAfterImages = 8; // Max number of after images to keep, this line is kind
                                                     // of mid because I feel that we never go above this number
    [SerializeField] private float afterImageDuration = 0.2f; // How long each after image lasts
    [SerializeField] private float afterImageFadeSpeed = 3f; // How fast they fade out
    [SerializeField] private bool onlyDuringDash = true; // Only show after images while dashing
    
    private float afterImageTimer = 0f;
    private Queue<GameObject> afterImagePool = new Queue<GameObject>();
    private Transform afterImageContainer;

    private void Start()
    {
        if (movement == null)
            movement = GetComponent<Movement>();
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Create a container for after images
        afterImageContainer = new GameObject("AfterImageContainer").transform;
        afterImageContainer.SetParent(transform.parent);
    }

    private void Update()
    {
        // Only create after images during dash (or always if disabled)
        if (onlyDuringDash && !movement.IsDashing)
            return;

        afterImageTimer -= Time.deltaTime;

        if (!onlyDuringDash || movement.IsDashing)
        {
            afterImageTimer -= Time.deltaTime;

            if (afterImageTimer <= 0f)
            {
                CreateAfterImage();
                afterImageTimer = afterImageInterval;
            }
        }


        // Update and remove faded out after images
        UpdateAfterImages();
    }

    private void CreateAfterImage()
    {
        // Create a new GameObject for the after image
        GameObject afterImageObj = new GameObject("AfterImage");
        afterImageObj.transform.SetParent(afterImageContainer);
        afterImageObj.transform.position = transform.position;
        afterImageObj.transform.localScale = transform.localScale;

        // Copy the sprite renderer
        SpriteRenderer afterImageRenderer = afterImageObj.AddComponent<SpriteRenderer>();
        afterImageRenderer.sprite = spriteRenderer.sprite;
        afterImageRenderer.color = spriteRenderer.color;
        afterImageRenderer.flipX = spriteRenderer.flipX;
        afterImageRenderer.flipY = spriteRenderer.flipY;
        afterImageRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;

        // Add a component to handle fading
        AfterImageFade fade = afterImageObj.AddComponent<AfterImageFade>();
        fade.Initialize(afterImageDuration, afterImageFadeSpeed);

        afterImagePool.Enqueue(afterImageObj);

        // Remove oldest after image if we exceed max
        if (afterImagePool.Count > maxAfterImages)
        {
            GameObject oldest = afterImagePool.Dequeue();
            Destroy(oldest);
        }
    }

    private void UpdateAfterImages()
    {
        // This is handled by the AfterImageFade component
    }

    public void ClearAllAfterImages()
    {
        while (afterImagePool.Count > 0)
        {
            GameObject afterImage = afterImagePool.Dequeue();
            Destroy(afterImage);
        }
    }
}

// Separate component to handle fading of individual after images
public class AfterImageFade : MonoBehaviour
{
    private float remainingTime;
    private float totalDuration;
    private float fadeSpeed;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    public void Initialize(float duration, float speed)
    {
        remainingTime = duration;
        totalDuration = duration;
        fadeSpeed = speed;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    private void Update()
    {
        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // Fade out the alpha
        Color currentColor = spriteRenderer.color;
        currentColor.a = Mathf.Lerp(0f, originalColor.a, remainingTime / totalDuration);
        spriteRenderer.color = currentColor;
    }
}
