using TMPro;
using UnityEngine;

/// <summary>
/// Shows saved progress level and coin count on the main menu status bar.
/// Assign two TMP texts: level line (e.g. "Level 2") and coin count.
/// </summary>
public class MainMenuStatusBar : MonoBehaviour
{
    private const string CurrentLevelKey = "CurrentLevel";
    // Keep in sync with CoinManager.CoinsKey
    private const string CoinsPrefsKey = "tls.coins";

    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text coinsText;

    private void OnEnable()
    {
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        int levelIndex = PlayerPrefs.GetInt(CurrentLevelKey, 0);
        int coins = CoinManager.Instance != null
            ? CoinManager.Instance.Coins
            : PlayerPrefs.GetInt(CoinsPrefsKey, 0);

        if (levelText != null)
        {
            levelText.text = $"{levelIndex}";
        }

        if (coinsText != null)
        {
            coinsText.text = coins.ToString();
        }
    }
}
