using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private void Awake()
    {
        EnsureGreyscaleManager();
    }
    /*public void PlayGame()
    {
        if (!PlayerPrefs.HasKey("HasPlayed"))
        {
            SceneManager.LoadScene("Tutorial");
            PlayerPrefs.SetInt("HasPlayed", 1);
            PlayerPrefs.Save();
        } 
        else
        {
            SceneManager.LoadScene("LevelOne");
        }

    }*/
    public void PlayGame()
    {
        SceneManager.LoadScene("Tutorial");
    }

    public void RestartGame()
    {
        CoinManager.ClearSavedProgress();
        SceneManager.LoadScene("Tutorial");
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
        GreyscaleManager manager = FindObjectOfType<GreyscaleManager>(true);
        if (manager != null)
        {
            return manager;
        }

        GameObject managerObject = new GameObject("GreyscaleManager");
        return managerObject.AddComponent<GreyscaleManager>();
    }
}
