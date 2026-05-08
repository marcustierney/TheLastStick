using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

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
            if (TryResolveBinding(action, out int resolvedBindingIndex, out InputBinding binding))
            {
                string id = binding.id.ToString();
                string overrideKeyById = InputBindingOverrides.GetOverrideKey(id);
                string overrideKeyByActionIndex = InputBindingOverrides.GetOverrideActionIndexKey(action, resolvedBindingIndex);
                if (PlayerPrefs.HasKey(overrideKeyById) || (!string.IsNullOrEmpty(overrideKeyByActionIndex) && PlayerPrefs.HasKey(overrideKeyByActionIndex)))
                {
                    string savedPath = PlayerPrefs.HasKey(overrideKeyById)
                        ? PlayerPrefs.GetString(overrideKeyById)
                        : PlayerPrefs.GetString(overrideKeyByActionIndex);
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

        if (!TryResolveBinding(action, out int resolvedBindingIndex, out _))
        {
            label.text = "—";
            Debug.LogWarning($"[RemapButton] '{gameObject.name}' has invalid binding index {bindingIndex} for action '{actionName}'.");
            return;
        }

        InputBindingDisplayInfo displayInfo = InputBindingDisplayHelper.Build(action, resolvedBindingIndex);
        label.text = displayInfo.Alias;

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

        InputBindingDisplayInfo displayInfo = InputBindingDisplayHelper.BuildFromPath(path);
        label.text = displayInfo.Alias;
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