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
        SwitchToUiInputContext();
        EnsurePauseActionEnabled();
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
        CoinManager.ClearSavedProgress();
        SceneManager.LoadScene("Tutorial");
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
        SwitchActionMap(UiActionMap);
        EnsurePauseActionEnabled();

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
        SwitchActionMap(UiActionMap);
        EnsurePauseActionEnabled();
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

        if (pauseTriggeredByGamepad && Gamepad.current != null)
        {
            playerInput.SwitchCurrentControlScheme(GamepadScheme, Gamepad.current);
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