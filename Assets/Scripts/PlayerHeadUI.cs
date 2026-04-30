using UnityEngine;
using UnityEngine.UI;

public class PlayerHeadUI : MonoBehaviour
{
    [SerializeField] private UpdateHealth playerHealth;

    private Image headImage;
    private SpriteRenderer headSpriteRenderer;

    [SerializeField] private Sprite healthyHead;
    [SerializeField] private Sprite hurtHead;
    [SerializeField] private Sprite lowHead;
    [SerializeField] private Sprite criticalHead;

    [SerializeField, Range(0f, 1f)] private float hurtThreshold = 0.75f;
    [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.5f;
    [SerializeField, Range(0f, 1f)] private float criticalThreshold = 0.25f;

    private Sprite currentSprite;

    private void Awake()
    {
        headImage = GetComponent<Image>();
        headSpriteRenderer = GetComponent<SpriteRenderer>();

        RefreshHead();
    }

    private void Update()
    {
        RefreshHead();
    }

    private void RefreshHead()
    {
        if (playerHealth == null || playerHealth.MaxHealth <= 0f)
        {
            return;
        }

        float healthPercent = playerHealth.Health / playerHealth.MaxHealth;
        Sprite nextSprite = GetHeadForPercent(healthPercent);
        if (nextSprite != null && nextSprite != currentSprite)
        {
            currentSprite = nextSprite;
            ApplySprite(currentSprite);
        }
    }

    private void ApplySprite(Sprite sprite)
    {
        if (headImage != null)
        {
            headImage.sprite = sprite;
        }

        if (headSpriteRenderer != null)
        {
            headSpriteRenderer.sprite = sprite;
        }
    }

    private Sprite GetHeadForPercent(float healthPercent)
    {
        if (healthPercent <= criticalThreshold)
        {
            return criticalHead;
        }

        if (healthPercent <= lowThreshold)
        {
            return lowHead;
        }

        if (healthPercent <= hurtThreshold)
        {
            return hurtHead;
        }

        return healthyHead;
    }
}