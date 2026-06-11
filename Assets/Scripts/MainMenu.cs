using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private const string UiActionMap = "UI";
    private const string LoadingScreenSceneName = "LoadingScreen";
    private const string ReplayableTutorialSceneName = "Replayable Tutorial";

    [SerializeField] CanvasGroup mainMenuPanel;
    [SerializeField] CanvasGroup optionsPanel;
    [SerializeField] CanvasGroup creditsPanel;
    [SerializeField] UIFocusGuard focusGuard;
    [SerializeField] Selectable mainMenuDefaultSelection;
    [SerializeField] Selectable creditsDefaultSelection;
    [SerializeField] TMP_Text playButtonText;
    [SerializeField] private float fadeOutDuration = 0.35f;

    private bool lastCancelHeld;
    private bool isTransitioning;
    private CanvasGroup fadeOverlayGroup;
    private Image fadeOverlayImage;

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

        RefreshPlayButtonLabel();
    }

    private void RefreshPlayButtonLabel()
    {
        if (playButtonText == null)
        {
            return;
        }

        bool hasPlayedBefore = PlayerPrefs.GetInt("HasPlayedBefore", 0) == 1;
        playButtonText.text = hasPlayedBefore ? "Continue" : "Play Game";
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

        if (DropdownGamepadSupport.TryCloseExpandedDropdown())
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
        if (isTransitioning)
        {
            return;
        }

        PlayerPrefs.SetInt("HasPlayedBefore", 1);
        int level = PlayerPrefs.GetInt("CurrentLevel", 0);
        if (!TryGetSceneForLevel(level, out string nextScene))
        {
            PlayerPrefs.Save();
            return;
        }

        GameAnalytics.FlushIfReady();
        SceneTransition.SetPendingNextScene(nextScene, 4f);
        PlayerPrefs.Save();
        BeginLoadingScreenTransition();
    }

    public void RestartGame()
    {
        if (isTransitioning)
        {
            return;
        }

        PlayerPrefs.SetInt("HasPlayedBefore", 1);
        CoinManager.ClearSavedProgress();
        GameAnalytics.FlushIfReady();
        SceneTransition.SetPendingNextScene("Tutorial", 4f);
        PlayerPrefs.SetInt("CurrentLevel", 0);
        PlayerPrefs.Save();
        BeginLoadingScreenTransition();
    }

    public void LoadReplayableTutorialScene()
    {
        if (isTransitioning)
        {
            return;
        }

        GameAnalytics.FlushIfReady();
        SceneTransition.SetPendingNextScene(ReplayableTutorialSceneName, 4f);
        BeginLoadingScreenTransition();
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

    private static void SetPanelInteractable(CanvasGroup cg, bool on)
    {
        if (cg == null)
        {
            return;
        }

        cg.interactable = on;
        cg.blocksRaycasts = on;
    }

    private void BeginLoadingScreenTransition()
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;
        if (focusGuard != null)
        {
            focusGuard.ClearSelection();
        }

        SetPanelInteractable(mainMenuPanel, false);
        SetPanelInteractable(optionsPanel, false);
        SetPanelInteractable(creditsPanel, false);
        StartCoroutine(FadeOutAndLoadLoadingScreen());
    }

    private IEnumerator FadeOutAndLoadLoadingScreen()
    {
        EnsureFadeOverlay();

        if (fadeOutDuration <= 0f)
        {
            SceneManager.LoadScene(LoadingScreenSceneName);
            yield break;
        }

        SetFadeOverlayAlpha(0f);

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFadeOverlayAlpha(Mathf.Clamp01(elapsed / fadeOutDuration));
            yield return null;
        }

        SetFadeOverlayAlpha(1f);
        SceneManager.LoadScene(LoadingScreenSceneName);
    }

    private void EnsureFadeOverlay()
    {
        if (fadeOverlayGroup != null)
        {
            return;
        }

        GameObject overlayObject = new GameObject("MenuFadeOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(Image));
        Canvas canvas = overlayObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        fadeOverlayGroup = overlayObject.GetComponent<CanvasGroup>();
        fadeOverlayGroup.alpha = 0f;
        fadeOverlayGroup.interactable = false;
        fadeOverlayGroup.blocksRaycasts = true;

        fadeOverlayImage = overlayObject.GetComponent<Image>();
        fadeOverlayImage.color = Color.black;
        fadeOverlayImage.raycastTarget = true;

        RectTransform rectTransform = overlayObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void SetFadeOverlayAlpha(float alpha)
    {
        if (fadeOverlayGroup != null)
        {
            fadeOverlayGroup.alpha = alpha;
        }

        if (fadeOverlayImage != null)
        {
            Color color = fadeOverlayImage.color;
            color.a = alpha;
            fadeOverlayImage.color = color;
        }
    }

    private static bool TryGetSceneForLevel(int level, out string sceneName)
    {
        switch (level)
        {
            case 0:
                sceneName = "Tutorial";
                return true;
            case 1:
                sceneName = "LevelOne";
                return true;
            case 2:
                sceneName = "LevelTwo";
                return true;
            case 3:
                sceneName = "LevelThree";
                return true;
            default:
                sceneName = null;
                return false;
        }
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