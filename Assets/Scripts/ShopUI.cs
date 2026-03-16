using TMPro;
using UnityEngine;

public class ShopUI : MonoBehaviour
{
    [Header("Shop Panel")]
    [SerializeField] private GameObject shopPanel;

    [Header("Speed Upgrade")]
    [SerializeField] private TMP_Text speedCostText;
    [SerializeField] private TMP_Text speedLevelText;
    [SerializeField] private float speedIncreasePerUpgrade = 1f;

    [Header("Damage Upgrade")]
    [SerializeField] private TMP_Text damageCostText;
    [SerializeField] private TMP_Text damageLevelText;
    [SerializeField] private int damageIncreasePerUpgrade = 1;

    [Header("Health Upgrade")]
    [SerializeField] private TMP_Text healthCostText;
    [SerializeField] private TMP_Text healthLevelText;
    [SerializeField] private float healthIncreasePerUpgrade = 10f;

    private void Start()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        RefreshUI();
    }

    public void Toggle()
    {
        if (shopPanel == null) return;
        shopPanel.SetActive(!shopPanel.activeSelf);
    }

    public void Close()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    // Called by the Speed upgrade button via OnClick in the Inspector
    public void BuySpeed()
    {
        if (CoinManager.Instance == null)
        {
            Debug.LogWarning("Shop purchase failed: no CoinManager instance found in the scene.");
            return;
        }

        if (!CoinManager.Instance.TryBuySpeedUpgrade(speedIncreasePerUpgrade)) return;

        RefreshUI();
    }

    // Called by the Damage upgrade button via OnClick in the Inspector
    public void BuyDamage()
    {
        if (CoinManager.Instance == null)
        {
            Debug.LogWarning("Shop purchase failed: no CoinManager instance found in the scene.");
            return;
        }

        if (!CoinManager.Instance.TryBuyDamageUpgrade(damageIncreasePerUpgrade)) return;

        RefreshUI();
    }

    // Called by the Health upgrade button via OnClick in the Inspector
    public void BuyHealth()
    {
        if (CoinManager.Instance == null)
        {
            Debug.LogWarning("Shop purchase failed: no CoinManager instance found in the scene.");
            return;
        }

        if (!CoinManager.Instance.TryBuyHealthUpgrade(healthIncreasePerUpgrade)) return;

        RefreshUI();
    }

    private void RefreshUI()
    {
        int speedLevel = CoinManager.Instance != null ? CoinManager.Instance.SpeedLevel : 0;
        int damageLevel = CoinManager.Instance != null ? CoinManager.Instance.DamageLevel : 0;
        int healthLevel = CoinManager.Instance != null ? CoinManager.Instance.HealthLevel : 0;

        int speedCost = CoinManager.Instance != null ? CoinManager.Instance.GetSpeedUpgradeCost() : 5;
        int damageCost = CoinManager.Instance != null ? CoinManager.Instance.GetDamageUpgradeCost() : 5;
        int healthCost = CoinManager.Instance != null ? CoinManager.Instance.GetHealthUpgradeCost() : 5;

        SetUpgradeText(speedCostText, speedLevelText, speedLevel, speedCost);
        SetUpgradeText(damageCostText, damageLevelText, damageLevel, damageCost);
        SetUpgradeText(healthCostText, healthLevelText, healthLevel, healthCost);
    }

    private void SetUpgradeText(TMP_Text costText, TMP_Text levelText, int level, int cost)
    {
        if (costText != null) costText.text = $"Cost: {cost} coins";
        if (levelText != null) levelText.text = $"Level {level}";
    }
}
