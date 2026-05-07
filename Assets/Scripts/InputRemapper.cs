using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to a manager GameObject in your scene.
/// Handles all remapping logic — finds RemapButton components automatically.
/// </summary>
public class InputRemapper : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("The PlayerInput component that owns your InputActionAsset.")]
    public PlayerInput playerInput;

    [Header("UI")]
    public Button applyButton;
    public Button resetButton;

    [Header("Listening State Visuals")]
    public string listeningText = "...";

    public InputActionAsset Asset => _asset;

    private InputActionAsset _asset;
    private InputActionRebindingExtensions.RebindingOperation _rebindOp;
    private RemapButton _activeButton;

    private void Awake()
    {
        if (playerInput == null)
        {
            Debug.LogError("[InputRemapper] PlayerInput is not assigned.");
            enabled = false;
            return;
        }

        _asset = playerInput.actions;
        if (_asset == null)
        {
            Debug.LogError("[InputRemapper] PlayerInput has no InputActionAsset.");
            enabled = false;
            return;
        }

        // Snapshot defaults now; reset should always restore this baseline.
        InputBindingOverrides.RebuildDefaultsCache(_asset);

        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplySavedBindings);
        }
        else
        {
            Debug.LogWarning("[InputRemapper] Apply button is not assigned.");
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetAllOverrides);
        }
        else
        {
            Debug.LogWarning("[InputRemapper] Reset button is not assigned.");
        }

        foreach (var rb in GetComponentsInChildren<RemapButton>(includeInactive: true))
            rb.Initialize(this);

    }

    private void OnDestroy()
    {
        _rebindOp?.Dispose();
    }

    public void StartListening(RemapButton button)
    {
        CancelListening();

        _activeButton = button;
        _activeButton.SetListeningVisual(listeningText);

        InputAction action = _asset.FindAction(button.actionName);

        if (action == null)
        {
            Debug.LogError($"[InputRemapper] Could not find action '{button.actionName}'. " +
                           $"Check actionName matches exactly (case-sensitive).");
            _activeButton.RestoreVisual();
            _activeButton = null;
            return;
        }

        action.Disable();

        if (!button.TryResolveBinding(action, out int resolvedBindingIndex, out _))
        {
            Debug.LogError($"[InputRemapper] Invalid bindingIndex {button.bindingIndex} for action '{button.actionName}' on '{button.gameObject.name}'.");
            action.Enable();
            _activeButton.RestoreVisual();
            _activeButton = null;
            return;
        }

        _rebindOp = action.PerformInteractiveRebinding(resolvedBindingIndex)
            .WithCancelingThrough("<Keyboard>/escape");

        if (button.isKeyboard)
        {
            _rebindOp.WithControlsExcluding("<Gamepad>")
                     .WithControlsExcluding("<Joystick>");
        }
        else
        {
            _rebindOp.WithControlsExcluding("<Keyboard>")
                     .WithControlsExcluding("<Mouse>")
                     .WithControlsExcluding("<Pointer>");
        }

        _rebindOp.OnComplete(op => FinishListening(op, cancelled: false))
                 .OnCancel(op  => FinishListening(op, cancelled: true))
                 .Start();
    }

    public void CancelListening()
    {
        if (_rebindOp == null) return;
        _rebindOp.Cancel();
    }

    private void FinishListening(InputActionRebindingExtensions.RebindingOperation op, bool cancelled)
    {
        if (_activeButton == null) return;

        InputAction action = _asset.FindAction(_activeButton.actionName);

        _activeButton.RestoreVisual();

        if (!cancelled)
        {
            string newPath = op.selectedControl?.path;

            if (newPath != null && action != null)
            {
                if (!_activeButton.TryResolveBinding(action, out _, out InputBinding binding))
                {
                    Debug.LogError($"[InputRemapper] Could not resolve binding index {_activeButton.bindingIndex} for action '{_activeButton.actionName}' during apply.");
                    action.Enable();
                    _rebindOp?.Dispose();
                    _rebindOp = null;
                    _activeButton = null;
                    return;
                }

                string bindingId = binding.id.ToString();
                UnbindConflictingBindings(newPath, binding.id);
                action.ApplyBindingOverride(new InputBinding { id = binding.id, overridePath = newPath });
                PlayerPrefs.SetString(InputBindingOverrides.GetOverrideKey(bindingId), newPath);
                PlayerPrefs.Save();

                // Propagate immediately so duplicate unbinds are visible/active
                // as soon as the rebind completes (without waiting for Apply).
                InputBindingOverrides.RefreshAllRegisteredRuntimeAssetsFromPrefs();
                foreach (var rb in FindObjectsByType<RemapButton>(FindObjectsInactive.Include))
                    rb.RefreshLabel(_asset);
            }
            else
            {
                Debug.LogWarning($"[InputRemapper] Rebind completed but selectedControl was null " +
                                 $"for action '{_activeButton?.actionName}'.");
            }
        }

        action?.Enable();

        _rebindOp?.Dispose();
        _rebindOp = null;
        _activeButton = null;
    }

    private void UnbindConflictingBindings(string selectedPath, System.Guid targetBindingId)
    {
        if (string.IsNullOrEmpty(selectedPath) || _asset == null)
        {
            return;
        }

        foreach (InputAction otherAction in _asset)
        {
            var bindings = otherAction.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding otherBinding = bindings[i];
                if (otherBinding.id == targetBindingId)
                {
                    continue;
                }

                if (!string.Equals(otherBinding.effectivePath, selectedPath, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                otherAction.ApplyBindingOverride(new InputBinding
                {
                    id = otherBinding.id,
                    overridePath = string.Empty
                });

                PlayerPrefs.SetString(InputBindingOverrides.GetOverrideKey(otherBinding.id.ToString()), string.Empty);
            }
        }
    }

    private void ApplySavedBindings()
    {
        CancelListening();

        InputBindingOverrides.ApplySavedOverrides(_asset);
        InputBindingOverrides.RefreshAllRegisteredRuntimeAssetsFromPrefs();

        PlayerPrefs.Save();

        foreach (var rb in FindObjectsByType<RemapButton>(FindObjectsInactive.Include))
            rb.RefreshLabel(_asset);

        Debug.Log("[InputRemapper] Applied saved control bindings.");
    }

    private void ResetAllOverrides()
    {
        CancelListening();

        InputBindingOverrides.ResetToCachedDefaults(_asset);
        InputBindingOverrides.ResetAllRegisteredRuntimeAssetsToCachedDefaults();

        PlayerPrefs.Save();

        foreach (var rb in FindObjectsByType<RemapButton>(FindObjectsInactive.Include))
            rb.RefreshLabel(_asset);

        Debug.Log("[InputRemapper] All overrides reset to defaults.");
    }

    public void RebuildDefaultsCache()
    {
        InputBindingOverrides.RebuildDefaultsCache(_asset);
        Debug.Log("[InputRemapper] Rebuilt cached binding defaults from current input asset.");
    }
}