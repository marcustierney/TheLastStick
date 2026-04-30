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
    private bool hasLoggedPauseFallbackWarning;
    private const string GameplayActionMap = "Gameplay";
    private const string UiActionMap = "UI";

    private void Start()
    {
        playerInput = Object.FindAnyObjectByType<PlayerInput>();
        CachePauseAction();
        EnsurePauseActionEnabled();

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
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; //freeze game
        isPaused = true;
        SwitchActionMap(UiActionMap);
        EnsurePauseActionEnabled();
        StartCoroutine(SelectAfterFrame(pauseDefaultSelectable));
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false); 
        Time.timeScale = 1f; //resume game
        isPaused = false;
        isOptionsOpen = false;
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

#if UNITY_EDITOR
        // Prevent Inspector preview from trying to repaint a scene object that
        // is about to be destroyed by the scene load (editor-only warning).
        UnityEditor.Selection.activeObject = null;
#endif

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
        SwitchActionMap(UiActionMap);
        EnsurePauseActionEnabled();

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

    private void EnsurePauseActionEnabled()
    {
        if (pauseAction != null && !pauseAction.enabled)
        {
            pauseAction.Enable();
        }
    }

    private bool IsPausePressedThisFrame()
    {
        if (pauseAction == null)
        {
            CachePauseAction();
            EnsurePauseActionEnabled();
        }

        if (pauseAction != null)
        {
            return pauseAction.WasPressedThisFrame();
        }

        // Fallback only if action lookup failed; keeps pause usable during setup issues.
        if (!hasLoggedPauseFallbackWarning)
        {
            Debug.Log("[PauseManager] No PlayerInput or Gameplay/Pause action; using Escape/Start for pause.");
            hasLoggedPauseFallbackWarning = true;
        }

        return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
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