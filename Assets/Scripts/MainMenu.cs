using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
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
        SceneManager.LoadScene("Options");
    }

    public void OpenCredits()
    {
        SceneManager.LoadScene("Credits");
    }
    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
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

    private static GreyscaleManager EnsureGreyscaleManager()
    {
        GreyscaleManager manager = Object.FindAnyObjectByType<GreyscaleManager>(FindObjectsInactive.Include);
        if (manager != null)
        {
            return manager;
        }

        GameObject managerObject = new GameObject("GreyscaleManager");
        return managerObject.AddComponent<GreyscaleManager>();
    }
}
