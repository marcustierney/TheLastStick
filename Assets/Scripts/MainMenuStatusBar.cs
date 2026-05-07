using TMPro;
using UnityEngine;

/// <summary>
/// Shows saved progress level and coin count on the main menu status bar.
/// Assign two TMP texts: level line (e.g. "Level 2") and coin count.
/// </summary>
public class MainMenuStatusBar : MonoBehaviour
{
    private const string CurrentLevelKey = "CurrentLevel";
    private const string HasPlayedBeforeKey = "HasPlayedBefore";
    // Keep in sync with CoinManager.CoinsKey
    private const string CoinsPrefsKey = "tls.coins";

    [SerializeField] private TMP_Text levelLabelText;
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
        bool hasPlayedBefore = PlayerPrefs.GetInt(HasPlayedBeforeKey, 0) == 1;
        int coins = CoinManager.Instance != null
            ? CoinManager.Instance.Coins
            : PlayerPrefs.GetInt(CoinsPrefsKey, 0);

        // Hide status bar for true first-time players.
        gameObject.SetActive(hasPlayedBefore);

        if (!hasPlayedBefore)
        {
            return;
        }

        if (levelText != null)
        {
            bool isTutorial = levelIndex == 0;
            levelText.gameObject.SetActive(!isTutorial);
            if (!isTutorial)
            {
                levelText.text = $"{levelIndex}";
            }
        }

        if (levelLabelText != null)
        {
            levelLabelText.text = levelIndex == 0 ? "Tutorial" : "Level";
        }

        if (coinsText != null)
        {
            coinsText.text = coins.ToString();
        }
    }
}
