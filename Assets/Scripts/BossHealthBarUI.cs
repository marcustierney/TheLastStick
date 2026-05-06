using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    public float Health, MaxHealth;

    [SerializeField]
    private Image healthBar;

    private void Awake()
    {
        if (healthBar == null)
        {
            healthBar = GetComponent<Image>();
        }

        if (healthBar != null)
        {
            healthBar.type = Image.Type.Filled;
            healthBar.fillMethod = Image.FillMethod.Horizontal;
            healthBar.fillOrigin = 0;
            healthBar.fillClockwise = false;
            healthBar.fillAmount = 1f;
        }
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

        healthBar.fillAmount = Health / MaxHealth;
    }
}
