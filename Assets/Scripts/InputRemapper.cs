using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

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
    [Tooltip("Color applied to the button currently awaiting input.")]
    public Color listeningColor = new Color(1f, 0.85f, 0.2f);
    public string listeningText = "...";

    // ── Public accessor for RemapButton ──────────────────────────────
    public InputActionAsset Asset => _asset;

    // ── Runtime state ────────────────────────────────────────────────
    private InputActionAsset _asset;
    private InputActionRebindingExtensions.RebindingOperation _rebindOp;
    private RemapButton _activeButton;

    private readonly Dictionary<string, string> _pendingOverrides = new();

    // ── Lifecycle ────────────────────────────────────────────────────
    private void Awake()
    {
        _asset = playerInput.actions;
        InputBindingOverrides.EnsureDefaultsCached(_asset);

        _asset.Disable();

        applyButton.onClick.AddListener(ApplyAllOverrides);
        resetButton.onClick.AddListener(ResetAllOverrides);

        foreach (var rb in GetComponentsInChildren<RemapButton>(includeInactive: true))
            rb.Initialize(this);

        _asset.Enable();
    }

    private void OnDestroy()
    {
        _rebindOp?.Dispose();
    }

    // ── Public API called by RemapButton ─────────────────────────────

    public void StartListening(RemapButton button)
    {
        CancelListening();

        _activeButton = button;
        _activeButton.SetListeningVisual(listeningColor, listeningText);

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

        _rebindOp = action.PerformInteractiveRebinding(button.bindingIndex)
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

    // ── Private helpers ──────────────────────────────────────────────

    private void FinishListening(InputActionRebindingExtensions.RebindingOperation op, bool cancelled)
    {
        if (_activeButton == null) return;

        InputAction action = _asset.FindAction(_activeButton.actionName);

        // Restore color/listening state FIRST before any label changes.
        _activeButton.RestoreVisual();

        if (!cancelled)
        {
            string newPath = op.selectedControl?.path;

            if (newPath != null && action != null)
            {
                // Use the binding's GUID to apply the override — this works correctly
                // for both regular bindings and composite parts (e.g. WASD move directions).
                InputBinding binding = action.bindings[_activeButton.bindingIndex];
                string bindingId = binding.id.ToString();

                // Store as pending for Apply to persist to PlayerPrefs.
                _pendingOverrides[bindingId] = newPath;

                // Apply immediately so the new key works in-game right away.
                // BindingMask targets by ID so composites are handled correctly.
                action.ApplyBindingOverride(new InputBinding { id = binding.id, overridePath = newPath });

                // Update label AFTER RestoreVisual so it doesn't get overwritten.
                _activeButton.UpdateLabel(newPath);
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

    private void ApplyAllOverrides()
    {
        foreach (InputAction action in _asset)
        {
            ReadOnlyArray<InputBinding> bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                string id = bindings[i].id.ToString();
                if (_pendingOverrides.TryGetValue(id, out string overridePath))
                    PlayerPrefs.SetString("Binding_" + id, overridePath);
            }
        }
        PlayerPrefs.Save();
        _pendingOverrides.Clear();
        Debug.Log("[InputRemapper] All overrides applied and saved.");
    }

    private void ResetAllOverrides()
    {
        CancelListening();
        _pendingOverrides.Clear();

        _asset.Disable();
        InputBindingOverrides.ResetToCachedDefaults(_asset);
        _asset.Enable();

        PlayerPrefs.Save();

        foreach (var rb in GetComponentsInChildren<RemapButton>(includeInactive: true))
            rb.RefreshLabel(_asset);

        Debug.Log("[InputRemapper] All overrides reset to defaults.");
    }

    // Optional dev utility: call from an editor/debug UI button after changing
    // the input asset defaults to refresh the cached baseline in PlayerPrefs.
    public void RebuildDefaultsCache()
    {
        InputBindingOverrides.RebuildDefaultsCache(_asset);
        Debug.Log("[InputRemapper] Rebuilt cached binding defaults from current input asset.");
    }
}