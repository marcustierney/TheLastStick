using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to a manager GameObject in your scene.
/// Handles all remapping logic — finds RemapButton components automatically.
/// </summary>
public class InputRemapper : MonoBehaviour
{
    public static bool IsRebindingInProgress { get; private set; }

    [Header("Input")]
    [Tooltip("The PlayerInput component that owns your InputActionAsset.")]
    public PlayerInput playerInput;

    [Header("UI")]
    public Button applyButton;
    public Button resetButton;

    [Header("Listening State Visuals")]
    public string listeningText = "...";
    [Header("Debug")]
    public bool logDuplicateResolution = true;

    public InputActionAsset Asset => _asset;

    private InputActionAsset _asset;
    private InputActionRebindingExtensions.RebindingOperation _rebindOp;
    private RemapButton _activeButton;
    private InputActionMap _uiActionMap;
    private bool _uiMapWasEnabledBeforeListening;

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
        IsRebindingInProgress = false;
        GateUiActionsForRebind(enableGate: false);
        _rebindOp?.Dispose();
    }

    public void StartListening(RemapButton button)
    {
        CancelListening();
        IsRebindingInProgress = false;
        GateUiActionsForRebind(enableGate: true);

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
        IsRebindingInProgress = true;
    }

    public void CancelListening()
    {
        if (_rebindOp == null)
        {
            IsRebindingInProgress = false;
            GateUiActionsForRebind(enableGate: false);
            return;
        }

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
                if (!_activeButton.TryResolveBinding(action, out int targetBindingIndex, out InputBinding binding))
                {
                    Debug.LogError($"[InputRemapper] Could not resolve binding index " +
                                   $"{_activeButton.bindingIndex} for action '{_activeButton.actionName}' during apply.");
                    action.Enable();
                    _rebindOp?.Dispose();
                    _rebindOp = null;
                    _activeButton = null;
                    return;
                }

                // Normalize using the TARGET binding's original path as context.
                string normalizedPath = InputBindingOverrides.NormalizeOverridePath(newPath, binding.path);

                // *** FIX: Unbind BEFORE applying the new override so conflict
                //     checks read the pre-rebind state of all other bindings. ***
                UnbindConflictingBindings(action, targetBindingIndex, normalizedPath, binding.id);

                string bindingId       = binding.id.ToString();
                string actionIndexKey  = InputBindingOverrides.GetOverrideActionIndexKey(action, targetBindingIndex);

                action.ApplyBindingOverride(
                    new InputBinding { id = binding.id, overridePath = normalizedPath });

                PlayerPrefs.SetString(InputBindingOverrides.GetOverrideKey(bindingId), normalizedPath);
                if (!string.IsNullOrEmpty(actionIndexKey))
                    PlayerPrefs.SetString(actionIndexKey, normalizedPath);

                PlayerPrefs.Save();

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
        IsRebindingInProgress = false;
        GateUiActionsForRebind(enableGate: false);
    }

    private void UnbindConflictingBindings(
        InputAction targetAction,
        int targetBindingIndex,
        string incomingPath,
        System.Guid targetBindingId)
    {
        if (string.IsNullOrEmpty(incomingPath) || _asset == null) return;

        // Only scan within the same action map to avoid clearing UI bindings etc.
        var actionsToScan = targetAction?.actionMap?.actions
            ?? (System.Collections.Generic.IEnumerable<InputAction>)_asset;

        if (logDuplicateResolution)
            Debug.Log($"[InputRemapper] DuplicateScan START | " +
                      $"target={targetAction?.name}, idx={targetBindingIndex}, " +
                      $"id={targetBindingId}, path={incomingPath}", this);

        foreach (InputAction otherAction in actionsToScan)
        {
            var bindings = otherAction.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding other = bindings[i];

                // ── Skip the binding we are about to assign ──────────────────
                if (other.id == targetBindingId) continue;
                if (otherAction == targetAction && i == targetBindingIndex) continue;

                // ── Skip composite GROUP headers (isPartOfComposite == false
                //    AND isComposite == true means it's the parent row, which
                //    has no path of its own and must not be cleared). ─────────
                if (other.isComposite) continue;

                // ── Build the single authoritative "current" path for this
                //    binding.  Prefer the override if one is set, else fall
                //    back to the default path.  Do NOT use effectivePath here
                //    because Unity may not have flushed it yet. ───────────────
                string effectivePath = string.IsNullOrEmpty(other.overridePath) ? other.path : other.overridePath;

                if (string.IsNullOrEmpty(effectivePath)) continue;

                bool isConflict = string.Equals(effectivePath, incomingPath,
                                      System.StringComparison.OrdinalIgnoreCase);

                if (logDuplicateResolution)
                    Debug.Log($"[InputRemapper] DuplicateScan CANDIDATE | " +
                              $"action={otherAction.name}, i={i}, id={other.id}, " +
                              $"effectivePath={effectivePath}, incoming={incomingPath}, " +
                              $"isConflict={isConflict}", this);

                if (!isConflict) continue;

                // ── Clear the conflict ────────────────────────────────────────
                otherAction.ApplyBindingOverride(i, string.Empty);

                string otherId        = other.id.ToString();
                string otherIdxKey    = InputBindingOverrides.GetOverrideActionIndexKey(otherAction, i);

                PlayerPrefs.SetString(InputBindingOverrides.GetOverrideKey(otherId), string.Empty);
                if (!string.IsNullOrEmpty(otherIdxKey))
                    PlayerPrefs.SetString(otherIdxKey, string.Empty);

                if (logDuplicateResolution)
                    Debug.Log($"[InputRemapper] Clearing conflict: action={otherAction.name} " +
                              $"binding[{i}] path={effectivePath}", this);
            }
        }

        if (logDuplicateResolution)
            Debug.Log("[InputRemapper] DuplicateScan END", this);
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

    private void GateUiActionsForRebind(bool enableGate)
    {
        if (playerInput == null || playerInput.actions == null)
        {
            return;
        }

        if (_uiActionMap == null)
        {
            _uiActionMap = playerInput.actions.FindActionMap("UI", throwIfNotFound: false);
        }

        if (_uiActionMap == null)
        {
            return;
        }

        if (enableGate)
        {
            _uiMapWasEnabledBeforeListening = _uiActionMap.enabled;
            if (_uiActionMap.enabled)
            {
                _uiActionMap.Disable();
            }
            return;
        }

        if (_uiMapWasEnabledBeforeListening && !_uiActionMap.enabled)
        {
            _uiActionMap.Enable();
        }
        _uiMapWasEnabledBeforeListening = false;
    }
}