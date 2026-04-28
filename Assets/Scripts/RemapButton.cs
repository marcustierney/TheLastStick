using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Add this to every KeyboardButton and ControllerButton prefab.
/// Automatically finds its own Button, TMP label, and the InputRemapper in the scene —
/// no manual wiring needed and no dependency on InputRemapper.Awake() order.
/// </summary>
public class RemapButton : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Exact name of the InputAction in your asset, e.g. 'Jump'.")]
    public string actionName;

    [Tooltip("Index of the binding in that action. " +
             "0 = first binding (usually keyboard), 1 = second (usually gamepad).")]
    public int bindingIndex;

    [Tooltip("True = keyboard binding; False = controller binding.")]
    public bool isKeyboard = true;

    // ── Auto-found references (no Inspector wiring needed) ────────────
    [HideInInspector] public TMP_Text label;
    [HideInInspector] public Button button;

    // ── Private ───────────────────────────────────────────────────────
    private InputRemapper _manager;
    private Color _originalColor;
    private string _originalText;
    private bool _initialized;

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        // Grab Button on this GO — required.
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"[RemapButton] No Button component found on '{gameObject.name}'. " +
                           $"Make sure this script sits on the same GameObject as the Button.");
            return;
        }

        // Grab the first TMP_Text in children (the button label).
        label = GetComponentInChildren<TMP_Text>();
        if (label == null)
            Debug.LogWarning($"[RemapButton] No TMP_Text found in children of '{gameObject.name}'. " +
                             $"Label updates will be skipped.");

        Debug.Log($"[RemapButton] Awake on '{gameObject.name}' — " +
                  $"button: {(button != null ? "found" : "MISSING")} | " +
                  $"label: {(label != null ? $"found ('{label.text}')" : "MISSING")} | " +
                  $"actionName: '{actionName}' | bindingIndex: {bindingIndex} | isKeyboard: {isKeyboard}");

        // Find the manager anywhere in the scene.
        _manager = FindFirstObjectByType<InputRemapper>();
        if (_manager == null)
        {
            Debug.LogError($"[RemapButton] No InputRemapper found in scene. " +
                           $"Add it to a GameObject and make sure it's active.");
            return;
        }

        // Wire the click — self-sufficient, no need for manager to call Initialize().
        button.onClick.AddListener(OnButtonClicked);

        Debug.Log($"[RemapButton] '{gameObject.name}' wired to InputRemapper successfully.");
    }

    private void Start()
    {
        // Start runs after all Awakes, so the asset is ready.
        if (_manager == null || button == null) return;

        // Load any saved override.
        InputAction action = _manager.Asset?.FindAction(actionName);
        if (action != null)
        {
            InputBinding binding = action.bindings[bindingIndex];
            string id = binding.id.ToString();
            if (PlayerPrefs.HasKey("Binding_" + id))
            {
                string savedPath = PlayerPrefs.GetString("Binding_" + id);
                // Use ID-based override so composites load correctly.
                action.ApplyBindingOverride(new InputBinding { id = binding.id, overridePath = savedPath });
            }
        }
        else
        {
            Debug.LogWarning($"[RemapButton] '{gameObject.name}' — action '{actionName}' not found in asset during Start. " +
                             $"Label will not be populated. Double-check actionName spelling.");
        }

        RefreshLabel(_manager.Asset);
        _initialized = true;
    }

    // ── Click handler ─────────────────────────────────────────────────

    private void OnButtonClicked()
    {
        Debug.Log($"[RemapButton] CLICKED — '{gameObject.name}' | " +
                  $"actionName: '{actionName}' | bindingIndex: {bindingIndex} | isKeyboard: {isKeyboard} | " +
                  $"initialized: {_initialized} | manager: {(_manager != null ? "valid" : "NULL")}");

        if (!_initialized || _manager == null)
        {
            Debug.LogError($"[RemapButton] '{gameObject.name}' clicked but not initialized. " +
                           $"Check earlier errors for the cause.");
            return;
        }

        _manager.StartListening(this);
    }

    // ── Called by InputRemapper (kept for compatibility) ──────────────

    public void Initialize(InputRemapper manager)
    {
        // No-op: self-initialization happens in Awake/Start.
        // Kept so InputRemapper.Awake()'s GetComponentsInChildren loop doesn't break.
    }

    // ── Label helpers ─────────────────────────────────────────────────

    public void RefreshLabel(InputActionAsset asset)
    {
        if (asset == null || label == null) return;

        InputAction action = asset.FindAction(actionName);
        if (action == null) return;

        string path = action.bindings[bindingIndex].effectivePath;
        string displayName = InputControlPath.ToHumanReadableString(
            path,
            InputControlPath.HumanReadableStringOptions.OmitDevice);

        label.text = string.IsNullOrEmpty(displayName) ? "—" : displayName;

        Debug.Log($"[RemapButton] '{gameObject.name}' label refreshed → '{label.text}' (path: '{path}')");
    }

    public void UpdateLabel(string path)
    {
        if (label == null)
        {
            Debug.LogWarning($"[RemapButton] '{gameObject.name}' UpdateLabel called but label is NULL.");
            return;
        }

        string displayName = InputControlPath.ToHumanReadableString(
            path,
            InputControlPath.HumanReadableStringOptions.OmitDevice);

        string resolved = string.IsNullOrEmpty(displayName) ? "—" : displayName;
        Debug.Log($"[RemapButton] '{gameObject.name}' UpdateLabel — path: '{path}' → display: '{resolved}'");
        label.text = resolved;
    }

    // ── Visual state ──────────────────────────────────────────────────

    public void SetListeningVisual(Color color, string text)
    {
        if (label)
        {
            _originalText = label.text;
            label.text = text;
        }

        var colors = button.colors;
        _originalColor = colors.normalColor;
        colors.normalColor = color;
        colors.selectedColor = color;
        button.colors = colors;
    }

    public void RestoreVisual()
    {
        if (label) label.text = _originalText;

        var colors = button.colors;
        colors.normalColor = _originalColor;
        colors.selectedColor = _originalColor;
        button.colors = colors;
    }
}