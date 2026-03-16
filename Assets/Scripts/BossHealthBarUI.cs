using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    public float Health, MaxHealth, Width, Height;

    [SerializeField]
    private RectTransform healthBar;

    [SerializeField]
    private float widthPadding = 16f;

    private float initialCenterX;
    private float initialPivotX;

    private void Awake()
    {
        if (healthBar == null)
        {
            return;
        }

        initialPivotX = healthBar.pivot.x;

        float startingWidth = Mathf.Max(0f, Width - widthPadding);
        initialCenterX = healthBar.anchoredPosition.x + (0.5f - initialPivotX) * startingWidth;
    }

    public void SetMaxHealth(float maxHealth)
    {
        MaxHealth = Mathf.Max(0f, maxHealth);
    }

    public void SetHealth(float health)
    {
        Health = Mathf.Clamp(health, 0f, MaxHealth);

        if (healthBar == null || MaxHealth <= 0f)
        {
            return;
        }

        float normalizedHealth = Health / MaxHealth;
        float newWidth = Mathf.Max(0f, normalizedHealth * Width - widthPadding);

        healthBar.sizeDelta = new Vector2(newWidth, Height);

        // Keep the bar center fixed so HP drains inward from both ends.
        float anchoredX = initialCenterX - (0.5f - initialPivotX) * newWidth;
        healthBar.anchoredPosition = new Vector2(anchoredX, healthBar.anchoredPosition.y);
    }
}
