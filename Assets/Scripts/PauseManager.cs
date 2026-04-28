using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject optionsCanvas;
    [SerializeField] private UIFocusGuard focusGuard;
    [SerializeField] private Selectable pauseDefaultSelectable;
    [SerializeField] private Selectable optionsDefaultSelectable;
    private bool isPaused = false;
    private bool isOptionsOpen = false;
    private bool wasPauseHeld;

    private void Start()
    {
        if (focusGuard == null)
        {
            focusGuard = Object.FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
        }

        if (pauseDefaultSelectable == null)
        {
            pauseDefaultSelectable = FindFirstSelectable(pauseMenuUI);
        }

        if (optionsDefaultSelectable == null)
        {
            optionsDefaultSelectable = FindFirstSelectable(optionsCanvas);
        }
    }

    void Update()
    {
        bool pauseHeld = (Keyboard.current != null && Keyboard.current.escapeKey.isPressed)
            || (Gamepad.current != null && Gamepad.current.startButton.isPressed);
        bool pausePressed = pauseHeld && !wasPauseHeld;
        wasPauseHeld = pauseHeld;

        if (pausePressed)
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
        StartCoroutine(SelectAfterFrame(pauseDefaultSelectable));
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false); 
        Time.timeScale = 1f; //resume game
        isPaused = false;
        if (focusGuard != null)
        {
            focusGuard.ClearSelection();
        }
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

        if (focusGuard != null)
        {
            focusGuard.ClearSelection();
        }

        OptionsTabManager tabManager = optionsCanvas.GetComponent<OptionsTabManager>();
        if (tabManager != null)
        {
            tabManager.ShowGraphics();
            StartCoroutine(SelectOptionsAfterFrame(tabManager));
            return;
        }

        StartCoroutine(SelectAfterFrame(optionsDefaultSelectable));
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
        StartCoroutine(SelectAfterFrame(pauseDefaultSelectable));
    }

    private IEnumerator SelectOptionsAfterFrame(OptionsTabManager tabManager)
    {
        yield return null;

        if (tabManager == null)
        {
            yield break;
        }

        Selectable next = tabManager.GetCurrentDefaultSelectable();
        if (next == null)
        {
            next = optionsDefaultSelectable;
        }

        if (focusGuard != null && next != null && next.gameObject.activeInHierarchy)
        {
            focusGuard.SetCurrentFallback(next);
            focusGuard.ForceSelectCurrentFallback();
        }
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