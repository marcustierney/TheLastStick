using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// On same object as the assist UI: toggles <see cref="CanvasGroup"/> (adds if missing) from
/// <see cref="UIFocusGuard"/> gamepad mode; fills first two TMP labels from UI Submit/Cancel.
/// </summary>
[DefaultExecutionOrder(10)]
public sealed class ControlAssist : MonoBehaviour
{
    UIFocusGuard focusGuard;
    CanvasGroup canvasGroup;
    TMP_Text confirmLabel;
    TMP_Text backLabel;
    InputSystem_Actions ownedActions;
    bool ownsActions;
    bool lastShown;
    InputAction cachedSubmit;
    InputAction cachedCancel;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        if (tmps.Length > 0)
            confirmLabel = tmps[0];
        if (tmps.Length > 1)
            backLabel = tmps[1];
    }

    void OnEnable()
    {
        focusGuard = FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
        cachedSubmit = null;
        cachedCancel = null;
        ApplyPanel(false);
        lastShown = false;
    }

    void OnDestroy()
    {
        if (ownsActions && ownedActions != null)
        {
            ownedActions.Dispose();
            ownedActions = null;
            ownsActions = false;
        }
    }

    void LateUpdate()
    {
        if (focusGuard == null)
            focusGuard = FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);

        bool show = focusGuard != null && focusGuard.IsGamepadInputActive;
        if (show != lastShown)
            ApplyPanel(show);

        if (!show)
            return;

        RefreshLabels();
    }

    void ApplyPanel(bool show)
    {
        lastShown = show;
        canvasGroup.alpha = show ? 1f : 0f;
        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;
    }

    void RefreshLabels()
    {
        Gamepad pad = PickGamepad();
        ResolveUiActions(out InputAction submit, out InputAction cancel);

        if (confirmLabel != null)
            confirmLabel.text = FormatHint("Confirm", LabelFromActionOrFallback(submit, pad, g => g.buttonSouth));

        if (backLabel != null)
            backLabel.text = FormatHint("Back", LabelFromActionOrFallback(cancel, pad, g => g.buttonEast));
    }

    static string FormatHint(string title, string button)
    {
        if (string.IsNullOrEmpty(button))
            return title;
        return $"{title}\n{button}";
    }

    void ResolveUiActions(out InputAction submit, out InputAction cancel)
    {
        if (cachedSubmit != null && cachedCancel != null)
        {
            submit = cachedSubmit;
            cancel = cachedCancel;
            return;
        }

        submit = cancel = null;
        PlayerInput pi = FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Include);
        InputActionAsset asset = pi != null ? pi.actions : null;
        if (asset != null)
        {
            InputActionMap ui = asset.FindActionMap("UI", false);
            if (ui != null)
            {
                submit = ui.FindAction("Submit", false);
                cancel = ui.FindAction("Cancel", false);
            }
        }

        if (submit == null || cancel == null)
        {
            if (!ownsActions)
            {
                ownedActions = new InputSystem_Actions();
                ownsActions = true;
            }

            if (submit == null)
                submit = ownedActions.UI.Submit;
            if (cancel == null)
                cancel = ownedActions.UI.Cancel;
        }

        cachedSubmit = submit;
        cachedCancel = cancel;
    }

    static Gamepad PickGamepad()
    {
        if (Gamepad.current != null)
            return Gamepad.current;
        foreach (Gamepad g in Gamepad.all)
        {
            if (g != null)
                return g;
        }

        return null;
    }

    static string LabelFromActionOrFallback(InputAction action, Gamepad pad, System.Func<Gamepad, ButtonControl> fallback)
    {
        if (pad == null)
            return "";

        if (action != null)
        {
            foreach (InputControl c in action.controls)
            {
                if (c.device != pad)
                    continue;
                if (c is ButtonControl btn)
                {
                    if (!string.IsNullOrEmpty(btn.shortDisplayName))
                        return btn.shortDisplayName;
                    if (!string.IsNullOrEmpty(btn.displayName))
                        return btn.displayName;
                }
            }
        }

        ButtonControl fb = fallback(pad);
        if (fb != null)
        {
            if (!string.IsNullOrEmpty(fb.shortDisplayName))
                return fb.shortDisplayName;
            if (!string.IsNullOrEmpty(fb.displayName))
                return fb.displayName;
        }

        return "";
    }
}
