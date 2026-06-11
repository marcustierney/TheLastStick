using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;


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

        ConfigureRootLayout();
        CacheLabels();
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

    void CacheLabels()
    {
        Transform confirmRoot = transform.Find("Confirm");
        Transform backRoot = transform.Find("Back");

        confirmLabel = confirmRoot != null
            ? confirmRoot.GetComponentInChildren<TMP_Text>(true)
            : null;
        backLabel = backRoot != null
            ? backRoot.GetComponentInChildren<TMP_Text>(true)
            : null;

        if (confirmLabel == null || backLabel == null)
        {
            TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
            if (confirmLabel == null && tmps.Length > 0)
                confirmLabel = tmps[0];
            if (backLabel == null && tmps.Length > 1)
                backLabel = tmps[1];
        }

        ConfigureHintRoot(confirmRoot, -45f);
        ConfigureHintRoot(backRoot, 45f);
        ConfigureLabelLayout(confirmLabel);
        ConfigureLabelLayout(backLabel);
    }

    void ConfigureRootLayout()
    {
        RectTransform root = transform as RectTransform;
        if (root == null)
            return;

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = Vector2.zero;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
    }

    static void ConfigureHintRoot(Transform hintRoot, float xOffset)
    {
        if (hintRoot == null)
            return;

        RectTransform rect = hintRoot as RectTransform;
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(xOffset, 24f);
    }

    static void ConfigureLabelLayout(TMP_Text label)
    {
        if (label == null)
            return;

        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.enableAutoSizing = false;
        label.margin = Vector4.zero;
        label.horizontalAlignment = HorizontalAlignmentOptions.Center;
        label.verticalAlignment = VerticalAlignmentOptions.Top;

        RectTransform rect = label.rectTransform;
        float parentScale = rect.parent != null ? rect.parent.localScale.x : 1f;
        if (parentScale <= 0f)
            parentScale = 1f;
        rect.localScale = Vector3.one / parentScale;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -44f);
        rect.sizeDelta = new Vector2(320f, 72f);
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

        ApplyLabelText(confirmLabel, "Confirm", LabelFromActionOrFallback(submit, pad, g => g.buttonSouth));
        ApplyLabelText(backLabel, "Back", LabelFromActionOrFallback(cancel, pad, g => g.buttonEast));
    }

    static void ApplyLabelText(TMP_Text label, string title, string button)
    {
        if (label == null)
            return;

        string text = FormatHint(title, button);
        if (label.text == text)
            return;

        label.text = text;
        label.ForceMeshUpdate();

        RectTransform rect = label.rectTransform;
        float width = Mathf.Max(label.preferredWidth + 8f, 120f);
        float height = Mathf.Max(label.preferredHeight + 4f, 36f);
        rect.sizeDelta = new Vector2(width, height);
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
