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
    private PlayerInput playerInput;
    private InputAction pauseAction;
    private InputAction uiCancelAction;
    private bool hasLoggedPauseFallbackWarning;
    private bool pauseTriggeredByGamepad;
    private bool pausedAudioByPauseMenu;
    private const string GameplayActionMap = "Gameplay";
    private const string UiActionMap = "UI";
    private const string GamepadScheme = "Gamepad";
    private const string KeyboardMouseScheme = "Keyboard&Mouse";
    private const float ResumeInputBlockSeconds = 0.12f;

    private void Start()
    {
        playerInput = Object.FindAnyObjectByType<PlayerInput>();
        CachePauseAction();
        EnsurePauseActionEnabled();
        CacheUiCancelAction();
        EnsureUiCancelActionEnabled();

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
        if ((isPaused || isOptionsOpen) && IsUiCancelPressedThisFrame())
        {
            if (DropdownGamepadSupport.TryCloseExpandedDropdown())
            {
                return;
            }

            if (isOptionsOpen)
            {
                CloseOptions();
            }
            else
            {
                ResumeGame();
            }
            return;
        }

        bool pausePressed = IsPausePressedThisFrame();

        if (pausePressed)
        {
            if (DropdownGamepadSupport.TryCloseExpandedDropdown())
            {
                return;
            }

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
        EnsureNonZeroPauseUiScale(pauseMenuUI);
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; //freeze game
        if (!AudioListener.pause)
        {
            AudioListener.pause = true;
            pausedAudioByPauseMenu = true;
        }
        isPaused = true;
        LevelRunStats.Instance?.PauseSpeedrun();
        SwitchToUiInputContext();
        UIInputBootstrap.Refresh();
        EnsurePauseActionEnabled();
        EnsureUiCancelActionEnabled();
        EnsurePauseMenuNavigation();
        if (pauseDefaultSelectable == null || !pauseDefaultSelectable.gameObject.activeInHierarchy)
        {
            pauseDefaultSelectable = FindFirstSelectable(pauseMenuUI);
        }
        StartCoroutine(SelectAfterFrame(pauseDefaultSelectable));
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false); 
        Time.timeScale = 1f; //resume game
        if (pausedAudioByPauseMenu)
        {
            AudioListener.pause = false;
            pausedAudioByPauseMenu = false;
        }
        GameplayInputGate.BlockForUnscaledSeconds(ResumeInputBlockSeconds);
        isPaused = false;
        isOptionsOpen = false;
        pauseTriggeredByGamepad = false;
        LevelRunStats.Instance?.ResumeSpeedrun();
        SwitchActionMap(GameplayActionMap);
        if (focusGuard != null)
        {
            focusGuard.ClearSelection();
        }
    }

    public void GoToMainMenu()
    {
        isPaused = false;
        isOptionsOpen = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        pausedAudioByPauseMenu = false;

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        if (optionsCanvas != null)
        {
            optionsCanvas.SetActive(false);
        }

        SwitchActionMap(UiActionMap);
        EnsurePauseActionEnabled();

        if (focusGuard != null)
        {
            focusGuard.ClearSelection();
        }

        GameAnalytics.FlushIfReady();

#if UNITY_EDITOR
        // Prevent Inspector preview from trying to repaint a scene object that
        // is about to be destroyed by the scene load (editor-only warning).
        UnityEditor.Selection.activeObject = null;
#endif

        SceneTransition.SetPendingNextScene("MainMenu", 4f);
        SceneManager.LoadScene("LoadingScreen");
    }

    public void Restart()
    {
        isPaused = false;
        isOptionsOpen = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        pausedAudioByPauseMenu = false;

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        if (optionsCanvas != null)
        {
            optionsCanvas.SetActive(false);
        }

        GameAnalytics.FlushIfReady();
        LevelRunStats.Instance?.ResetSpeedrun();
        CoinManager.ClearSavedProgress();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        PlayerPrefs.SetInt("CurrentLevel", 0);
        PlayerPrefs.Save();
    }

    public void OpenOptions()
    {
        if (optionsCanvas == null)
        {
            Debug.LogWarning("Options canvas not assigned to PauseManager");
            return;
        }

        pauseMenuUI.SetActive(false);
        EnsureNonZeroPauseUiScale(optionsCanvas);
        optionsCanvas.SetActive(true);
        isOptionsOpen = true;
        SwitchToUiInputContext();
        UIInputBootstrap.Refresh();
        EnsurePauseActionEnabled();
        EnsureUiCancelActionEnabled();

        if (focusGuard == null)
        {
            focusGuard = Object.FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
        }

        if (focusGuard != null)
        {
            focusGuard.ClearSelection();
        }

        OptionsTabManager tabManager = optionsCanvas.GetComponentInChildren<OptionsTabManager>(true);
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
        SwitchToUiInputContext();
        UIInputBootstrap.Refresh();
        EnsurePauseActionEnabled();
        EnsureUiCancelActionEnabled();
        EnsurePauseMenuNavigation();
        StartCoroutine(SelectAfterFrame(pauseDefaultSelectable));
    }

    private void CachePauseAction()
    {
        if (playerInput == null)
        {
            playerInput = Object.FindAnyObjectByType<PlayerInput>();
        }

        if (playerInput == null || playerInput.actions == null)
        {
            pauseAction = null;
            return;
        }

        pauseAction = playerInput.actions.FindAction("Gameplay/Pause", throwIfNotFound: false)
            ?? playerInput.actions.FindAction("Pause", throwIfNotFound: false);
    }

    private void CacheUiCancelAction()
    {
        if (playerInput == null)
        {
            playerInput = Object.FindAnyObjectByType<PlayerInput>();
        }

        if (playerInput == null || playerInput.actions == null)
        {
            uiCancelAction = null;
            return;
        }

        uiCancelAction = playerInput.actions.FindAction("UI/Cancel", throwIfNotFound: false)
            ?? playerInput.actions.FindAction("Cancel", throwIfNotFound: false);
    }

    private void EnsurePauseActionEnabled()
    {
        if (pauseAction != null && !pauseAction.enabled)
        {
            pauseAction.Enable();
        }
    }

    private void EnsureUiCancelActionEnabled()
    {
        if (uiCancelAction != null && !uiCancelAction.enabled)
        {
            uiCancelAction.Enable();
        }
    }

    private bool IsPausePressedThisFrame()
    {
        if (InputRemapper.IsRebindingInProgress)
        {
            return false;
        }

        bool gamepadStartPressed = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        bool keyboardEscapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        if (gamepadStartPressed)
        {
            pauseTriggeredByGamepad = true;
        }
        else if (keyboardEscapePressed)
        {
            pauseTriggeredByGamepad = false;
        }

        if (pauseAction == null)
        {
            CachePauseAction();
            EnsurePauseActionEnabled();
        }

        if (pauseAction != null)
        {
            bool actionPressed = pauseAction.WasPressedThisFrame();
            if (actionPressed && pauseAction.activeControl != null)
            {
                pauseTriggeredByGamepad = pauseAction.activeControl.device is Gamepad;
            }

            // Always honor direct device checks so Start/Escape keep working
            // even when the current action map is UI.
            return actionPressed || gamepadStartPressed || keyboardEscapePressed;
        }

        // Last-resort guard only when action lookup fails entirely.
        if (!hasLoggedPauseFallbackWarning)
        {
            Debug.Log("[PauseManager] No PlayerInput or Gameplay/Pause action; using Escape/Start for pause.");
            hasLoggedPauseFallbackWarning = true;
        }
        return keyboardEscapePressed || gamepadStartPressed;
    }

    private bool IsUiCancelPressedThisFrame()
    {
        if (InputRemapper.IsRebindingInProgress)
        {
            return false;
        }

        if (uiCancelAction == null)
        {
            CacheUiCancelAction();
            EnsureUiCancelActionEnabled();
        }

        return uiCancelAction != null && uiCancelAction.WasPressedThisFrame();
    }

    private void SwitchToUiInputContext()
    {
        SwitchActionMap(UiActionMap);

        if (playerInput == null)
        {
            playerInput = Object.FindAnyObjectByType<PlayerInput>();
        }

        if (playerInput == null)
        {
            return;
        }

        Gamepad gamepad = PickGamepad();
        if (pauseTriggeredByGamepad && gamepad != null)
        {
            playerInput.SwitchCurrentControlScheme(GamepadScheme, gamepad);
            return;
        }

        if (Keyboard.current != null && Mouse.current != null)
        {
            playerInput.SwitchCurrentControlScheme(KeyboardMouseScheme, Keyboard.current, Mouse.current);
            return;
        }

        if (Keyboard.current != null)
        {
            playerInput.SwitchCurrentControlScheme(KeyboardMouseScheme, Keyboard.current);
        }
    }

    private void SwitchActionMap(string mapName)
    {
        if (playerInput == null)
        {
            playerInput = Object.FindAnyObjectByType<PlayerInput>();
        }

        if (playerInput == null || string.IsNullOrEmpty(mapName))
        {
            return;
        }

        if (playerInput.currentActionMap != null && playerInput.currentActionMap.name == mapName)
        {
            return;
        }

        InputActionMap map = playerInput.actions != null
            ? playerInput.actions.FindActionMap(mapName, throwIfNotFound: false)
            : null;
        if (map == null)
        {
            return;
        }

        playerInput.SwitchCurrentActionMap(mapName);
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

        ApplyPauseFocus(next);
    }

    private IEnumerator SelectAfterFrame(Selectable selectable)
    {
        yield return null;

        ApplyPauseFocus(selectable);
    }

    private void ApplyPauseFocus(Selectable selectable)
    {
        if (focusGuard == null)
        {
            focusGuard = Object.FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
        }

        if (focusGuard == null || selectable == null || !selectable.gameObject.activeInHierarchy)
        {
            return;
        }

        focusGuard.SetCurrentFallback(selectable);

        if (pauseTriggeredByGamepad || focusGuard.IsGamepadInputActive)
        {
            focusGuard.EnterGamepadModeAndSelect(selectable);
            return;
        }

        focusGuard.ForceSelectCurrentFallback();
    }

    private void EnsurePauseMenuNavigation()
    {
        if (pauseMenuUI == null)
        {
            return;
        }

        Button resume = FindButton(pauseMenuUI.transform, "ResumeButton");
        Button options = FindButton(pauseMenuUI.transform, "OptionsButton");
        Button mainMenu = FindButton(pauseMenuUI.transform, "MainMenuButton");

        WireVerticalNavigation(resume, options, mainMenu);

        if (pauseDefaultSelectable == null && resume != null)
        {
            pauseDefaultSelectable = resume;
        }
    }

    private static Button FindButton(Transform root, string buttonName)
    {
        if (root == null)
        {
            return null;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.gameObject.name == buttonName)
            {
                return button;
            }
        }

        return null;
    }

    private static void WireVerticalNavigation(params Selectable[] chain)
    {
        Selectable previous = null;
        foreach (Selectable current in chain)
        {
            if (current == null)
            {
                continue;
            }

            Navigation navigation = current.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = previous;
            navigation.selectOnDown = null;
            current.navigation = navigation;

            if (previous != null)
            {
                Navigation previousNavigation = previous.navigation;
                previousNavigation.selectOnDown = current;
                previous.navigation = previousNavigation;
            }

            previous = current;
        }
    }

    private static Gamepad PickGamepad()
    {
        if (Gamepad.current != null)
        {
            return Gamepad.current;
        }

        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (gamepad != null)
            {
                return gamepad;
            }
        }

        return null;
    }

    private static void EnsureNonZeroPauseUiScale(GameObject uiRoot)
    {
        if (uiRoot == null)
        {
            return;
        }

        RectTransform rect = uiRoot.transform as RectTransform;
        if (rect == null)
        {
            return;
        }

        Vector3 ls = rect.localScale;
        if (Mathf.Abs(ls.x) < 1e-5f || Mathf.Abs(ls.y) < 1e-5f)
        {
            rect.localScale = Vector3.one;
        }
    }

    private static Selectable FindFirstSelectable(GameObject root)
    {
        if (root == null)
        {
            return null;
        }

        Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
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