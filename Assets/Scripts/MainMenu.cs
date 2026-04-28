using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] CanvasGroup mainMenuPanel;
    [SerializeField] CanvasGroup optionsPanel;
    [SerializeField] CanvasGroup creditsPanel;
    [SerializeField] UIFocusGuard focusGuard;
    [SerializeField] Selectable mainMenuDefaultSelection;
    [SerializeField] Selectable creditsDefaultSelection;

    private bool lastCancelHeld;

    private void Awake()
    {
        EnsureGreyscaleManager();
    }

    private void Start()
    {
        if (focusGuard == null)
        {
            focusGuard = Object.FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
        }

        if (mainMenuDefaultSelection == null)
        {
            mainMenuDefaultSelection = FindFirstSelectable(mainMenuPanel);
        }

        if (creditsDefaultSelection == null)
        {
            creditsDefaultSelection = FindFirstSelectable(creditsPanel);
        }

        if (focusGuard != null)
        {
            focusGuard.SetCurrentFallback(mainMenuDefaultSelection);
            focusGuard.ForceSelectCurrentFallback();
        }
    }

    private void Update()
    {
        bool cancelHeld =
            (Keyboard.current != null && Keyboard.current.escapeKey.isPressed)
            || (Gamepad.current != null && Gamepad.current.buttonEast.isPressed);
        bool cancelPressed = cancelHeld && !lastCancelHeld;
        lastCancelHeld = cancelHeld;

        if (!cancelPressed)
        {
            return;
        }

        bool isOptionsOpen = optionsPanel != null && optionsPanel.interactable;
        bool isCreditsOpen = creditsPanel != null && creditsPanel.interactable;
        if (isOptionsOpen || isCreditsOpen)
        {
            BackToMenu();
        }
    }

    public void PlayGame()
    {
        int level = PlayerPrefs.GetInt("CurrentLevel", 0);

        if (level == 0)
        {
            SceneManager.LoadScene("Tutorial");
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

        if (focusGuard != null)
        {
            focusGuard.ClearSelection();
        }

        OptionsTabManager tabManager = optionsPanel.GetComponent<OptionsTabManager>();
        tabManager.ShowGraphics();

        // Defer selection by one frame so Unity's CanvasGroup interactability
        // propagates through the layout system before the nav graph is queried.
        // Without this, Sliders inside panels that were previously hidden report
        // IsInteractable()=false on the first frame, getting skipped by auto-nav.
        StartCoroutine(SelectAfterFrame(tabManager));
    }

    private IEnumerator SelectAfterFrame(OptionsTabManager tabManager)
    {
        yield return null;

        if (focusGuard != null)
        {
            focusGuard.SetCurrentFallback(tabManager.GetCurrentDefaultSelectable());
            focusGuard.ForceSelectCurrentFallback();
        }
    }

    public void OpenCredits()
    {
        SetPanel(mainMenuPanel, false);
        SetPanel(optionsPanel, false);
        SetPanel(creditsPanel, true);
        if (focusGuard != null)
        {
            focusGuard.SetCurrentFallback(creditsDefaultSelection);
            focusGuard.ForceSelectCurrentFallback();
        }
    }

    public void BackToMenu()
    {
        SetPanel(optionsPanel, false);
        SetPanel(creditsPanel, false);
        SetPanel(mainMenuPanel, true);
        if (focusGuard != null)
        {
            focusGuard.SetCurrentFallback(mainMenuDefaultSelection);
            focusGuard.ForceSelectCurrentFallback();
        }
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

    private static Selectable FindFirstSelectable(CanvasGroup panel)
    {
        if (panel == null)
        {
            return null;
        }

        Selectable[] selectables = panel.GetComponentsInChildren<Selectable>(true);
        foreach (Selectable selectable in selectables)
        {
            if (selectable != null && selectable.IsInteractable() && selectable.gameObject.activeInHierarchy)
            {
                return selectable;
            }
        }

        return selectables.Length > 0 ? selectables[0] : null;
    }
}