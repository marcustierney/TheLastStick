using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CoinManager : MonoBehaviour
{
    private const string CoinsKey = "tls.coins";
    private const string SpeedLevelKey = "tls.speedLevel";
    private const string DamageLevelKey = "tls.damageLevel";
    private const string HealthLevelKey = "tls.healthLevel";
    private const string SpeedBonusKey = "tls.speedBonus";
    private const string DamageBonusKey = "tls.damageBonus";
    private const string HealthBonusKey = "tls.healthBonus";

    public static CoinManager Instance { get; private set; }

    [SerializeField] private TMP_Text coinText;
    [SerializeField] private int baseUpgradeCost = 5;

    public int Coins { get; private set; }
    public int SpeedLevel { get; private set; }
    public int DamageLevel { get; private set; }
    public int HealthLevel { get; private set; }

    private float totalSpeedBonus;
    private int totalDamageBonus;
    private float totalHealthBonus;
    private GameObject lastAppliedPlayer;

    private void Awake()
    {
        if (coinText == null)
        {
            coinText = GetComponent<TMP_Text>();
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (transform.parent != null)
        {
            // DontDestroyOnLoad only works on root objects.
            transform.SetParent(null, true);
        }
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        LoadState();
        RefreshText();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void Update()
    {
        // Some scenes instantiate the player shortly after load.
        // Keep trying until we find and apply to the current scene player once.
        if (lastAppliedPlayer == null)
        {
            ApplyStoredUpgradesToCurrentPlayer();
        }
    }

    public void AddCoins(int amount)
    {
        Coins += amount;
        Debug.Log($"Coins added: {amount}. Total coins: {Coins}");
        SaveState();
        RefreshText();
    }

    public bool SpendCoins(int amount)
    {
        if (Coins < amount)
        {
            Debug.Log($"Not enough coins. Need {amount}, have {Coins}");
            return false;
        }

        Coins = Mathf.Max(0, Coins - amount);
        Debug.Log($"Coins spent: {amount}. Total coins: {Coins}");
        SaveState();
        RefreshText();
        return true;
    }

    public int GetSpeedUpgradeCost()
    {
        return GetUpgradeCost(SpeedLevel);
    }

    public int GetDamageUpgradeCost()
    {
        return GetUpgradeCost(DamageLevel);
    }

    public int GetHealthUpgradeCost()
    {
        return GetUpgradeCost(HealthLevel);
    }

    public bool TryBuySpeedUpgrade(float amount)
    {
        int cost = GetSpeedUpgradeCost();
        if (!SpendCoins(cost))
        {
            return false;
        }

        SpeedLevel++;
        totalSpeedBonus += amount;
        SaveState();
        ApplyUpgradePurchaseToCurrentPlayer(amount, 0, 0f);
        return true;
    }

    public bool TryBuyDamageUpgrade(int amount)
    {
        int cost = GetDamageUpgradeCost();
        if (!SpendCoins(cost))
        {
            return false;
        }

        DamageLevel++;
        totalDamageBonus += amount;
        SaveState();
        ApplyUpgradePurchaseToCurrentPlayer(0f, amount, 0f);
        return true;
    }

    public bool TryBuyHealthUpgrade(float amount)
    {
        int cost = GetHealthUpgradeCost();
        if (!SpendCoins(cost))
        {
            return false;
        }

        HealthLevel++;
        totalHealthBonus += amount;
        SaveState();
        ApplyUpgradePurchaseToCurrentPlayer(0f, 0, amount);
        return true;
    }

    public void SetCoinText(TMP_Text textTarget)
    {
        coinText = textTarget;
        RefreshText();
    }

    public void ResetProgress()
    {
        Coins = 0;
        SpeedLevel = 0;
        DamageLevel = 0;
        HealthLevel = 0;
        totalSpeedBonus = 0f;
        totalDamageBonus = 0;
        totalHealthBonus = 0f;
        lastAppliedPlayer = null;
        SaveState();
        RefreshText();
    }

    public static void ClearSavedProgress()
    {
        if (Instance != null)
        {
            Instance.ResetProgress();
            return;
        }

        PlayerPrefs.SetInt(CoinsKey, 0);
        PlayerPrefs.SetInt(SpeedLevelKey, 0);
        PlayerPrefs.SetInt(DamageLevelKey, 0);
        PlayerPrefs.SetInt(HealthLevelKey, 0);
        PlayerPrefs.SetFloat(SpeedBonusKey, 0f);
        PlayerPrefs.SetInt(DamageBonusKey, 0);
        PlayerPrefs.SetFloat(HealthBonusKey, 0f);
        PlayerPrefs.Save();
    }

    private int GetUpgradeCost(int currentLevel)
    {
        return baseUpgradeCost * (currentLevel + 1);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        lastAppliedPlayer = null;
        RefreshText();
        ApplyStoredUpgradesToCurrentPlayer();
        Debug.Log($"Scene loaded: {scene.name}. Stored upgrades - Speed:{SpeedLevel} Damage:{DamageLevel} Health:{HealthLevel}");
    }

    private void ApplyStoredUpgradesToCurrentPlayer()
    {
        if (!TryGetUpgradeablePlayer(out GameObject player, out Movement movement, out UpdateHealth health, out SwordHitbox[] swordHitboxes))
        {
            return;
        }

        if (player == lastAppliedPlayer)
        {
            return;
        }

        if (movement != null && totalSpeedBonus > 0f)
        {
            movement.AddSpeed(totalSpeedBonus);
        }

        if (health != null && totalHealthBonus > 0f)
        {
            health.IncreaseMaxHealth(totalHealthBonus);
        }

        if (swordHitboxes != null && totalDamageBonus > 0)
        {
            foreach (SwordHitbox hitbox in swordHitboxes)
            {
                if (hitbox != null)
                {
                    hitbox.damage += totalDamageBonus;
                }
            }
        }

        lastAppliedPlayer = player;
        Debug.Log($"Applied upgrades to player in scene {SceneManager.GetActiveScene().name}: +Speed {totalSpeedBonus}, +Damage {totalDamageBonus}, +Health {totalHealthBonus}");
    }

    private void ApplyUpgradePurchaseToCurrentPlayer(float speedAmount, int damageAmount, float healthAmount)
    {
        if (!TryGetUpgradeablePlayer(out GameObject player, out Movement movement, out UpdateHealth health, out SwordHitbox[] swordHitboxes))
        {
            return;
        }

        if (movement != null && speedAmount > 0f)
        {
            movement.AddSpeed(speedAmount);
        }

        if (health != null && healthAmount > 0f)
        {
            health.IncreaseMaxHealth(healthAmount);
        }

        if (swordHitboxes != null && damageAmount > 0)
        {
            foreach (SwordHitbox hitbox in swordHitboxes)
            {
                if (hitbox != null)
                {
                    hitbox.damage += damageAmount;
                }
            }
        }

        lastAppliedPlayer = player;
    }

    private bool TryGetUpgradeablePlayer(out GameObject player, out Movement movement, out UpdateHealth health, out SwordHitbox[] swordHitboxes)
    {
        player = null;
        movement = null;
        health = null;
        swordHitboxes = null;

        GameObject[] playerObjects;
        try
        {
            playerObjects = GameObject.FindGameObjectsWithTag("Player");
        }
        catch
        {
            return false;
        }

        for (int i = 0; i < playerObjects.Length; i++)
        {
            GameObject candidate = playerObjects[i];
            if (candidate == null)
            {
                continue;
            }

            Movement candidateMovement = candidate.GetComponent<Movement>();
            UpdateHealth candidateHealth = candidate.GetComponent<UpdateHealth>();
            SwordHitbox[] candidateHitboxes = candidate.GetComponentsInChildren<SwordHitbox>(true);

            bool hasUpgradeTargets = candidateMovement != null || candidateHealth != null || (candidateHitboxes != null && candidateHitboxes.Length > 0);
            if (!hasUpgradeTargets)
            {
                continue;
            }

            player = candidate;
            movement = candidateMovement;
            health = candidateHealth;
            swordHitboxes = candidateHitboxes;
            return true;
        }

        return false;
    }

    private void RefreshText()
    {
        if (coinText != null)
        {
            coinText.text = Coins.ToString();
        }
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(CoinsKey, Coins);
        PlayerPrefs.SetInt(SpeedLevelKey, SpeedLevel);
        PlayerPrefs.SetInt(DamageLevelKey, DamageLevel);
        PlayerPrefs.SetInt(HealthLevelKey, HealthLevel);
        PlayerPrefs.SetFloat(SpeedBonusKey, totalSpeedBonus);
        PlayerPrefs.SetInt(DamageBonusKey, totalDamageBonus);
        PlayerPrefs.SetFloat(HealthBonusKey, totalHealthBonus);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        Coins = PlayerPrefs.GetInt(CoinsKey, 0);
        SpeedLevel = PlayerPrefs.GetInt(SpeedLevelKey, 0);
        DamageLevel = PlayerPrefs.GetInt(DamageLevelKey, 0);
        HealthLevel = PlayerPrefs.GetInt(HealthLevelKey, 0);
        totalSpeedBonus = PlayerPrefs.GetFloat(SpeedBonusKey, 0f);
        totalDamageBonus = PlayerPrefs.GetInt(DamageBonusKey, 0);
        totalHealthBonus = PlayerPrefs.GetFloat(HealthBonusKey, 0f);
    }
}
