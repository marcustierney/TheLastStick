using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    private const float ResumeInputBlockSeconds = 0.12f;

    [Header("Shop Panel")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private UIFocusGuard focusGuard;
    [SerializeField] private Selectable shopDefaultSelectable;
    [SerializeField] private bool pauseGameplayWhileOpen = true;

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

    [Header("Purchase Sound")]
    [SerializeField] private AudioSource purchaseAudioSource;
    [SerializeField] private AudioClip[] purchaseClips = new AudioClip[5];
    private bool pausedByShop;
    private bool pausedAudioByShop;

    private void Start()
    {
        CachePurchaseAudioSource();
        ConfigurePurchaseAudioSource();
        if (shopPanel != null) shopPanel.SetActive(false);
        if (focusGuard == null)
        {
            focusGuard = Object.FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
        }

        if (shopDefaultSelectable == null)
        {
            shopDefaultSelectable = FindFirstSelectable(shopPanel);
        }
        RefreshUI();
    }

    public void Toggle()
    {
        if (shopPanel == null) return;
        bool shouldOpen = !shopPanel.activeSelf;
        shopPanel.SetActive(shouldOpen);

        if (shouldOpen)
        {
            PauseGameplayForShop();
            StartCoroutine(SelectAfterFrame(shopDefaultSelectable));
            return;
        }

        ResumeGameplayFromShop();

        if (focusGuard != null)
        {
            focusGuard.ClearSelection();
        }
    }

    public void Close()
    {
        if (shopPanel == null)
        {
            return;
        }

        shopPanel.SetActive(false);
        ResumeGameplayFromShop();
        if (focusGuard != null)
        {
            focusGuard.ClearSelection();
        }
    }

    private void PauseGameplayForShop()
    {
        if (!pauseGameplayWhileOpen)
        {
            return;
        }

        if (Time.timeScale > 0f)
        {
            Time.timeScale = 0f;
            pausedByShop = true;
        }

        if (!AudioListener.pause)
        {
            AudioListener.pause = true;
            pausedAudioByShop = true;
        }
    }

    private void ResumeGameplayFromShop()
    {
        if (!pauseGameplayWhileOpen || !pausedByShop)
        {
            if (pausedAudioByShop)
            {
                AudioListener.pause = false;
                pausedAudioByShop = false;
            }
            return;
        }

        Time.timeScale = 1f;
        pausedByShop = false;
        if (pausedAudioByShop)
        {
            AudioListener.pause = false;
            pausedAudioByShop = false;
        }
        GameplayInputGate.BlockForUnscaledSeconds(ResumeInputBlockSeconds);
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

        PlayRandomPurchaseSound();
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

        PlayRandomPurchaseSound();
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

        PlayRandomPurchaseSound();
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

    private void PlayRandomPurchaseSound()
    {
        CachePurchaseAudioSource();

        if (purchaseAudioSource == null || purchaseClips == null || purchaseClips.Length == 0)
        {
            return;
        }

        int clipIndex = Random.Range(0, purchaseClips.Length);
        AudioClip clip = purchaseClips[clipIndex];
        if (clip == null)
        {
            return;
        }

        purchaseAudioSource.PlayOneShot(clip);
    }

    private void CachePurchaseAudioSource()
    {
        if (purchaseAudioSource != null)
        {
            return;
        }

        GameObject purchaseObject = GameObject.Find("Purchase");
        if (purchaseObject != null)
        {
            purchaseAudioSource = purchaseObject.GetComponent<AudioSource>();
            ConfigurePurchaseAudioSource();
        }
    }

    private void ConfigurePurchaseAudioSource()
    {
        if (purchaseAudioSource == null)
        {
            return;
        }

        purchaseAudioSource.ignoreListenerPause = true;
    }

    private IEnumerator SelectAfterFrame(Selectable selectable)
    {
        yield return null;

        if (focusGuard != null && selectable != null && selectable.gameObject.activeInHierarchy)
        {
            focusGuard.SetCurrentFallback(selectable);
            focusGuard.ForceSelectCurrentFallback();
        }
    }

    private static Selectable FindFirstSelectable(GameObject root)
    {
        if (root == null)
        {
            return null;
        }

        Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
        return selectables.Length > 0 ? selectables[0] : null;
    }
}
