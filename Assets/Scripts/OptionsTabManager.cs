using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OptionsTabManager : MonoBehaviour
{
    const int TabCount = 4;

    [SerializeField] CanvasGroup graphicsPanel;
    [SerializeField] CanvasGroup soundPanel;
    [SerializeField] CanvasGroup controlsPanel;
    [SerializeField] CanvasGroup accessPanel;
    [SerializeField] Selectable graphicsDefaultSelectable;
    [SerializeField] Selectable soundDefaultSelectable;
    [SerializeField] Selectable controlsDefaultSelectable;
    [SerializeField] Selectable accessDefaultSelectable;
    [SerializeField] UIFocusGuard focusGuard;

    private Selectable currentDefaultSelectable;
    private int currentTabIndex;
    private Button[] tabButtons;
    private CanvasGroup panelCanvasGroup;

    void Awake()
    {
        panelCanvasGroup = GetComponent<CanvasGroup>();
        CacheTabButtons();
    }

    void OnEnable()
    {
        if (focusGuard == null)
        {
            focusGuard = Object.FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
        }

        ShowTab(graphicsPanel);
    }

    void Update()
    {
        if (!IsOptionsPanelOpen())
        {
            return;
        }

        if (DropdownGamepadSupport.IsAnyExpanded())
        {
            return;
        }

        if (InputRemapper.IsRebindingInProgress)
        {
            return;
        }

        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (gamepad == null)
            {
                continue;
            }

            bool prev = gamepad.leftShoulder.wasPressedThisFrame
                     || gamepad.leftTrigger.wasPressedThisFrame;
            bool next = gamepad.rightShoulder.wasPressedThisFrame
                     || gamepad.rightTrigger.wasPressedThisFrame;

            if (prev)
            {
                CycleTab(-1);
                return;
            }

            if (next)
            {
                CycleTab(1);
                return;
            }
        }
    }

    public void ShowGraphics()  => ShowTab(graphicsPanel);
    public void ShowSound()     => ShowTab(soundPanel);
    public void ShowControls()  => ShowTab(controlsPanel);
    public void ShowAccess()    => ShowTab(accessPanel);

    void ShowTab(CanvasGroup selected)
    {
        SetPanel(graphicsPanel,  selected == graphicsPanel);
        SetPanel(soundPanel,     selected == soundPanel);
        SetPanel(controlsPanel,  selected == controlsPanel);
        SetPanel(accessPanel,    selected == accessPanel);
        EnsureSlidersUseAutomaticNavigation(selected);
        if (selected == graphicsPanel)
        {
            EnsureGraphicsPanelNavigation();
        }

        currentTabIndex = GetTabIndex(selected);
        UpdateTabButtonVisuals(currentTabIndex);
        currentDefaultSelectable = GetDefaultForPanel(selected);

        // Selection is driven by MainMenu.SelectAfterFrame so the nav graph
        // has a full frame for CanvasGroup interactability to propagate.
        // Only force-select here when ShowTab is called directly (tab switching
        // after the panel is already open and stable).
        if (focusGuard != null && currentDefaultSelectable != null && selected.interactable)
        {
            focusGuard.SetCurrentFallback(currentDefaultSelectable);
            focusGuard.ForceSelectCurrentFallback();
        }
    }

    void CycleTab(int direction)
    {
        int nextIndex = (currentTabIndex + direction + TabCount) % TabCount;
        ShowTabByIndex(nextIndex);
    }

    void ShowTabByIndex(int index)
    {
        switch (index)
        {
            case 0: ShowGraphics(); break;
            case 1: ShowSound(); break;
            case 2: ShowControls(); break;
            case 3: ShowAccess(); break;
        }
    }

    int GetTabIndex(CanvasGroup selected)
    {
        if (selected == graphicsPanel) return 0;
        if (selected == soundPanel) return 1;
        if (selected == controlsPanel) return 2;
        if (selected == accessPanel) return 3;
        return 0;
    }

    bool IsOptionsPanelOpen()
    {
        if (panelCanvasGroup != null)
        {
            return panelCanvasGroup.interactable;
        }

        return isActiveAndEnabled;
    }

    void CacheTabButtons()
    {
        Transform tabsRoot = transform.Find("Tabs");
        if (tabsRoot == null)
        {
            tabButtons = new Button[0];
            return;
        }

        tabButtons = new Button[TabCount];
        string[] tabNames = { "Graphics", "Sound", "Controls", "Accesibility" };
        for (int i = 0; i < TabCount; i++)
        {
            tabButtons[i] = tabsRoot.Find(tabNames[i])?.GetComponent<Button>();
        }
    }

    void UpdateTabButtonVisuals(int activeIndex)
    {
        if (tabButtons == null)
        {
            return;
        }

        for (int i = 0; i < tabButtons.Length; i++)
        {
            Button button = tabButtons[i];
            if (button == null)
            {
                continue;
            }

            if (i == activeIndex)
            {
                button.OnSelect(null);
            }
            else
            {
                button.OnDeselect(null);
            }
        }
    }

    public Selectable GetCurrentDefaultSelectable()
    {
        return currentDefaultSelectable != null ? currentDefaultSelectable : graphicsDefaultSelectable;
    }

    void SetPanel(CanvasGroup cg, bool on)
    {
        cg.alpha          = on ? 1f : 0f;
        cg.interactable   = on;
        cg.blocksRaycasts = on;
    }

    private Selectable GetDefaultForPanel(CanvasGroup panel)
    {
        if (panel == graphicsPanel)  return ResolveDefault(graphicsDefaultSelectable,  graphicsPanel);
        if (panel == soundPanel)     return ResolveDefault(soundDefaultSelectable,      soundPanel);
        if (panel == controlsPanel)  return ResolveDefault(controlsDefaultSelectable,   controlsPanel);
        if (panel == accessPanel)    return ResolveDefault(accessDefaultSelectable,      accessPanel);
        return null;
    }

    private static Selectable ResolveDefault(Selectable configured, CanvasGroup panel)
    {
        // If a default is explicitly assigned in the Inspector, always use it.
        // Do NOT filter by IsInteractable() here — CanvasGroup interactability
        // may not have propagated yet on the first frame after a scene reload,
        // causing Sliders to falsely report non-interactable and be skipped.
        if (configured != null)
        {
            return configured;
        }

        if (panel == null)
        {
            return null;
        }

        // Fall back to the first child Selectable regardless of current
        // interactable state — by the time ForceSelectCurrentFallback runs
        // (deferred one frame by MainMenu), the state will be correct.
        Selectable[] selectables = panel.GetComponentsInChildren<Selectable>(true);
        return selectables.Length > 0 ? selectables[0] : null;
    }

    private static void EnsureSlidersUseAutomaticNavigation(CanvasGroup panel)
    {
        if (panel == null)
        {
            return;
        }

        Slider[] sliders = panel.GetComponentsInChildren<Slider>(true);
        foreach (Slider slider in sliders)
        {
            if (slider == null)
            {
                continue;
            }

            Navigation navigation = slider.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            slider.navigation = navigation;
        }
    }

    private void EnsureGraphicsPanelNavigation()
    {
        if (graphicsPanel == null)
        {
            return;
        }

        TMP_Dropdown resolution = graphicsPanel.GetComponentInChildren<ResolutionDropdown>(true)?.dropdown;
        TMP_Dropdown displayMode = graphicsPanel.GetComponentInChildren<DisplayModeDropdown>(true)?.dropdown;
        TMP_Dropdown fpsCap = graphicsPanel.GetComponentInChildren<FPSCapDropdown>(true)?.dropdown;
        Toggle vSync = graphicsPanel.GetComponentInChildren<VSyncToggle>(true)?.toggle;
        Button apply = FindApplyButton(graphicsPanel.transform);

        if (graphicsDefaultSelectable == null && resolution != null)
        {
            graphicsDefaultSelectable = resolution;
        }

        WireVerticalNavigation(resolution, displayMode, fpsCap, vSync, apply);
        DropdownGamepadSupport.EnsureOn(resolution);
        DropdownGamepadSupport.EnsureOn(displayMode);
        DropdownGamepadSupport.EnsureOn(fpsCap);

        Button graphicsTab = transform.Find("Tabs/Graphics")?.GetComponent<Button>();
        if (graphicsTab != null && resolution != null)
        {
            SetExplicitDown(graphicsTab, resolution);

            Navigation resolutionNavigation = resolution.navigation;
            resolutionNavigation.selectOnUp = graphicsTab;
            resolution.navigation = resolutionNavigation;
        }
    }

    private static Button FindApplyButton(Transform graphicsRoot)
    {
        Button[] buttons = graphicsRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.gameObject.name == "ApplyButton")
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

    private static void SetExplicitDown(Selectable selectable, Selectable down)
    {
        if (selectable == null || down == null)
        {
            return;
        }

        Navigation navigation = selectable.navigation;
        navigation.mode = Navigation.Mode.Explicit;
        navigation.selectOnDown = down;
        selectable.navigation = navigation;
    }
}