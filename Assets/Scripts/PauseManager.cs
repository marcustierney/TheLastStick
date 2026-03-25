using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject optionsCanvas;
    private bool isPaused = false;
    private bool isOptionsOpen = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If options are open, close them
            if (isOptionsOpen)
            {
                CloseOptions();
            }
            // Otherwise, toggle pause
            else if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; //freeze game
        isPaused = true;
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false); 
        Time.timeScale = 1f; //resume game
        isPaused = false;
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene("MainMenu"); 
    }

    public void OpenOptions()
    {
        if (optionsCanvas == null)
        {
            Debug.LogWarning("Options canvas not assigned to PauseManager");
            return;
        }

        pauseMenuUI.SetActive(false);
        optionsCanvas.SetActive(true);
        isOptionsOpen = true;
    }

    public void CloseOptions()
    {
        if (optionsCanvas == null)
        {
            return;
        }

        optionsCanvas.SetActive(false);
        pauseMenuUI.SetActive(true);
        isOptionsOpen = false;
    }
}