using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Globalization;

/// <summary>
/// Add this to every KeyboardButton and ControllerButton prefab.
/// Finds its own Button/TMP label and connects to an InputRemapper.
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

    [HideInInspector] public TMP_Text label;
    [HideInInspector] public Button button;

    private InputRemapper _manager;
    private string _originalText;
    private bool _isWired;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"[RemapButton] No Button component found on '{gameObject.name}'. " +
                           $"Make sure this script sits on the same GameObject as the Button.");
            return;
        }

        // Automatic controller navigation uses 3D positions; bogus non-zero local Z
        // (often from bad prefab/scene overrides) breaks FindSelectableOnDown/Up.
        var rect = transform as RectTransform;
        if (rect != null)
        {
            Vector3 local = rect.localPosition;
            if (local.z != 0f)
            {
                rect.localPosition = new Vector3(local.x, local.y, 0f);
            }
        }

        label = GetComponentInChildren<TMP_Text>();
        if (label == null)
        {
            Debug.LogWarning($"[RemapButton] No TMP_Text found in children of '{gameObject.name}'. Label updates will be skipped.");
        }
    }

    private void Start()
    {
        if (_manager == null)
        {
            _manager = FindAnyObjectByType<InputRemapper>();
        }
        EnsureWired();

        if (_manager == null) return;

        InputAction action = _manager.Asset?.FindAction(actionName);
        if (action != null)
        {
            if (TryResolveBinding(action, out _, out InputBinding binding))
            {
                string id = binding.id.ToString();
                string overrideKey = InputBindingOverrides.GetOverrideKey(id);
                if (PlayerPrefs.HasKey(overrideKey))
                {
                    string savedPath = PlayerPrefs.GetString(overrideKey);
                    action.ApplyBindingOverride(new InputBinding { id = binding.id, overridePath = savedPath });
                }
            }
        }

        RefreshLabel(_manager.Asset);
    }

    private void OnButtonClicked()
    {
        if (_manager == null)
        {
            _manager = FindAnyObjectByType<InputRemapper>();
            EnsureWired();
        }

        if (_manager == null)
        {
            Debug.LogError($"[RemapButton] Cannot start rebind for '{gameObject.name}' because no InputRemapper was found.");
            return;
        }

        _manager.StartListening(this);
    }

    public void Initialize(InputRemapper manager)
    {
        _manager = manager;
        EnsureWired();
    }

    private void EnsureWired()
    {
        if (_isWired || button == null)
        {
            return;
        }

        button.onClick.AddListener(OnButtonClicked);
        _isWired = true;
    }

    public void RefreshLabel(InputActionAsset asset)
    {
        if (asset == null || label == null) return;

        InputAction action = asset.FindAction(actionName);
        if (action == null) return;

        if (!TryResolveBinding(action, out _, out InputBinding binding))
        {
            label.text = "—";
            Debug.LogWarning($"[RemapButton] '{gameObject.name}' has invalid binding index {bindingIndex} for action '{actionName}'.");
            return;
        }

        label.text = FormatBindingDisplay(binding.effectivePath);

    }

    /// <summary>
    /// Resolves configured bindingIndex to an action-local index.
    /// Supports both action-local indices and global action-map indices.
    /// </summary>
    public bool TryResolveBinding(InputAction action, out int resolvedBindingIndex, out InputBinding resolvedBinding)
    {
        resolvedBindingIndex = -1;
        resolvedBinding = default;

        if (action == null || bindingIndex < 0)
        {
            return false;
        }

        // Preferred: index is already action-local.
        if (bindingIndex < action.bindings.Count)
        {
            resolvedBindingIndex = bindingIndex;
            resolvedBinding = action.bindings[bindingIndex];
            return true;
        }

        // Fallback: index is global within the action map.
        InputActionMap map = action.actionMap;
        if (map == null || bindingIndex >= map.bindings.Count)
        {
            return false;
        }

        if (map.bindings[bindingIndex].action != action.name)
        {
            return false;
        }

        int localIndex = 0;
        for (int mapIndex = 0; mapIndex < map.bindings.Count; mapIndex++)
        {
            if (map.bindings[mapIndex].action != action.name)
            {
                continue;
            }

            if (mapIndex == bindingIndex)
            {
                if (localIndex >= 0 && localIndex < action.bindings.Count)
                {
                    resolvedBindingIndex = localIndex;
                    resolvedBinding = action.bindings[localIndex];
                    return true;
                }

                return false;
            }

            localIndex++;
        }

        return false;
    }

    public void UpdateLabel(string path)
    {
        if (label == null)
        {
            return;
        }

        label.text = FormatBindingDisplay(path);
    }

    private static string FormatBindingDisplay(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "—";
        }

        string normalizedPath = path.ToLowerInvariant();
        if (normalizedPath.Contains("/dpad/"))
        {
            string direction = FormatDirection(path);
            return string.IsNullOrEmpty(direction) ? "D-Pad" : $"D-Pad {direction}";
        }

        if (normalizedPath.Contains("/leftstick/"))
        {
            string direction = FormatDirection(path);
            return string.IsNullOrEmpty(direction) ? "Left Stick" : $"Left Stick {direction}";
        }

        if (normalizedPath.EndsWith("/leftstick"))
        {
            return "Left Stick";
        }

        if (normalizedPath.Contains("/rightstick/"))
        {
            string direction = FormatDirection(path);
            return string.IsNullOrEmpty(direction) ? "Right Stick" : $"Right Stick {direction}";
        }

        if (normalizedPath.EndsWith("/rightstick"))
        {
            return "Right Stick";
        }

        string displayName = InputControlPath.ToHumanReadableString(
            path,
            InputControlPath.HumanReadableStringOptions.OmitDevice);

        return string.IsNullOrEmpty(displayName) ? "—" : CapitalizeWords(displayName);
    }

    private static string FormatDirection(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string[] segments = path.Split('/');
        if (segments.Length == 0)
        {
            return string.Empty;
        }

        string direction = segments[segments.Length - 1].ToLowerInvariant();
        return direction switch
        {
            "left" => "Left",
            "right" => "Right",
            "up" => "Up",
            "down" => "Down",
            _ => string.Empty
        };
    }

    private static string CapitalizeWords(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
    }

    public void SetListeningVisual(string text)
    {
        if (label)
        {
            _originalText = label.text;
            label.text = text;
        }
    }

    public void RestoreVisual()
    {
        if (label) label.text = _originalText;
    }
}