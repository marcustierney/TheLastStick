using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] CanvasGroup mainMenuPanel;
    [SerializeField] CanvasGroup optionsPanel;
    [SerializeField] CanvasGroup creditsPanel;

    private void Awake()
    {
        EnsureGreyscaleManager();
    }

    public void PlayGame()
    {
        int level = PlayerPrefs.GetInt("CurrentLevel", 0);

        if (level == 0)
        {
            SceneManager.LoadScene("Tutorial");
            //PlayerPrefs.SetInt("CurrentLevel", 1);
        }
        else if (level == 1)
        {
            SceneManager.LoadScene("LevelOne");
        }
        else if (level == 2)
        {
            SceneManager.LoadScene("LevelTwo");
        }

        PlayerPrefs.Save();
    }

    public void RestartGame()
    {
        CoinManager.ClearSavedProgress();
        SceneManager.LoadScene("Tutorial");
        PlayerPrefs.SetInt("CurrentLevel", 0);
        PlayerPrefs.Save();
    }

    public void OpenOptions()
    {
        SetPanel(mainMenuPanel, false);
        SetPanel(creditsPanel, false);
        SetPanel(optionsPanel, true);
        optionsPanel.GetComponent<OptionsTabManager>().ShowGraphics();

    }

    public void OpenCredits()
    {
        SetPanel(mainMenuPanel, false);
        SetPanel(optionsPanel, false);
        SetPanel(creditsPanel, true);
    }

    public void BackToMenu()
    {
        SetPanel(optionsPanel, false);
        SetPanel(creditsPanel, false);
        SetPanel(mainMenuPanel, true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void GreyscaleToggle()
    {
        EnsureGreyscaleManager().ToggleGreyscale();
    }

    public void GreyscaleToggle(bool enabled)
    {
        EnsureGreyscaleManager().SetGreyscale(enabled);
    }

    private void SetPanel(CanvasGroup cg, bool on)
    {
        cg.alpha          = on ? 1f : 0f;
        cg.interactable   = on;
        cg.blocksRaycasts = on;
    }

    private static GreyscaleManager EnsureGreyscaleManager()
    {
        GreyscaleManager manager = Object.FindAnyObjectByType<GreyscaleManager>(FindObjectsInactive.Include);
        if (manager != null) return manager;

        GameObject managerObject = new GameObject("GreyscaleManager");
        return managerObject.AddComponent<GreyscaleManager>();
    }
}