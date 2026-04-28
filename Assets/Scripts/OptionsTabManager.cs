using UnityEngine;
using UnityEngine.UI;

public class OptionsTabManager : MonoBehaviour
{
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

    void OnEnable()
    {
        if (focusGuard == null)
        {
            focusGuard = Object.FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
        }

        ShowTab(graphicsPanel);
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
}