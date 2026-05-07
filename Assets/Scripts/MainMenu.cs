using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private const string UiActionMap = "UI";

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
        // Ensure menu scene is always interactive even when loaded from paused gameplay.
        Time.timeScale = 1f;
        EnsureUiActionMapActive();
        EnsureUiInputModuleReady();

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
            SceneTransition.SetPendingNextScene("Tutorial", 3f);
            SceneManager.LoadScene("LoadingScreen");
        }
        else if (level == 1)
        {
            SceneTransition.SetPendingNextScene("LevelOne", 3f);
            SceneManager.LoadScene("LoadingScreen");
        }
        else if (level == 2)
        {
            SceneTransition.SetPendingNextScene("LevelTwo", 3f);
            SceneManager.LoadScene("LoadingScreen");
        }
        else if (level == 3)
        {
            SceneTransition.SetPendingNextScene("LevelThree", 3f);
            SceneManager.LoadScene("LoadingScreen");
        }

        PlayerPrefs.Save();
    }

    public void RestartGame()
    {
        CoinManager.ClearSavedProgress();
        SceneTransition.SetPendingNextScene("Tutorial", 3f);
        SceneManager.LoadScene("LoadingScreen");
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

    private static void EnsureUiActionMapActive()
    {
        PlayerInput playerInput = Object.FindAnyObjectByType<PlayerInput>();
        if (playerInput == null)
        {
            return;
        }

        InputActionMap uiMap = playerInput.actions != null
            ? playerInput.actions.FindActionMap(UiActionMap, throwIfNotFound: false)
            : null;
        if (uiMap == null)
        {
            return;
        }

        if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != UiActionMap)
        {
            playerInput.SwitchCurrentActionMap(UiActionMap);
        }

        if (!uiMap.enabled)
        {
            uiMap.Enable();
        }
    }

    private static void EnsureUiInputModuleReady()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        InputSystemUIInputModule uiModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (uiModule == null)
        {
            return;
        }

        PlayerInput playerInput = Object.FindAnyObjectByType<PlayerInput>();
        InputActionAsset asset = playerInput != null ? playerInput.actions : uiModule.actionsAsset;
        if (asset == null)
        {
            return;
        }

        uiModule.actionsAsset = asset;
        uiModule.move = ActionRef(asset, "UI/Navigate");
        uiModule.submit = ActionRef(asset, "UI/Submit");
        uiModule.cancel = ActionRef(asset, "UI/Cancel");
        uiModule.point = ActionRef(asset, "UI/Point");
        uiModule.leftClick = ActionRef(asset, "UI/Click");
        uiModule.rightClick = ActionRef(asset, "UI/RightClick");
        uiModule.middleClick = ActionRef(asset, "UI/MiddleClick");
        uiModule.scrollWheel = ActionRef(asset, "UI/ScrollWheel");
        uiModule.trackedDevicePosition = ActionRef(asset, "UI/TrackedDevicePosition");
        uiModule.trackedDeviceOrientation = ActionRef(asset, "UI/TrackedDeviceOrientation");

        EnableActionReference(uiModule.move);
        EnableActionReference(uiModule.submit);
        EnableActionReference(uiModule.cancel);
        EnableActionReference(uiModule.point);
        EnableActionReference(uiModule.leftClick);
        EnableActionReference(uiModule.rightClick);
        EnableActionReference(uiModule.middleClick);
        EnableActionReference(uiModule.scrollWheel);
    }

    private static InputActionReference ActionRef(InputActionAsset asset, string actionPath)
    {
        InputAction action = asset.FindAction(actionPath, throwIfNotFound: false);
        return action != null ? InputActionReference.Create(action) : null;
    }

    private static void EnableActionReference(InputActionReference actionReference)
    {
        if (actionReference?.action != null && !actionReference.action.enabled)
        {
            actionReference.action.Enable();
        }
    }
}