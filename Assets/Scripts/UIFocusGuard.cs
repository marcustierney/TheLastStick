using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public class UIFocusGuard : MonoBehaviour
{
    [SerializeField] Selectable fallbackSelectable;

    [Header("Focus Restore")]
    [Tooltip("Seconds after losing selection before the fallback is re-selected.")]
    [SerializeField] float restoreDebounceSeconds = 0.08f;

    [Header("Input Source Switching")]
    [Tooltip("Mouse delta below this sqr-magnitude per frame is treated as driver noise.")]
    [SerializeField] float mouseMoveDeadzonePixels = 4f;

    [Tooltip("Minimum seconds between input-source switches. Prevents ghost-delta bounce.")]
    [SerializeField] float inputSwitchDebounceSeconds = 0.25f;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private bool        lastInputWasGamepad  = false;
    private float       lastSelectionLostTime = -1f;
    private float       lastInputSwitchTime   = float.NegativeInfinity;

    // Flag set by onEvent callback; consumed in LateUpdate on the main thread.
    private bool        pendingMouseReengage = false;

    // -------------------------------------------------------------------------
    // Singleton bootstrap
    // -------------------------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include) != null)
            return;

        GameObject go = new GameObject("UIFocusGuard");
        DontDestroyOnLoad(go);
        go.AddComponent<UIFocusGuard>();
    }

    // -------------------------------------------------------------------------
    // Unity messages
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        InputSystem.onEvent      += OnInputSystemEvent;
    }

    private void OnDisable()
    {
        InputSystem.onEvent      -= OnInputSystemEvent;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Always restore cursor on disable so the player is never stuck.
        SetCursorForMode(isGamepad: false);
    }

    private void OnDestroy()
    {
        SetCursorForMode(isGamepad: false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            SetCursorForMode(lastInputWasGamepad);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        fallbackSelectable   = null;
        lastSelectionLostTime = -1f;
        pendingMouseReengage  = false;

        // Re-apply cursor state — new EventSystem was created.
        SetCursorForMode(lastInputWasGamepad);
    }

    private void LateUpdate()
    {
        // Consume the flag raised by the onEvent callback.
        if (pendingMouseReengage)
        {
            pendingMouseReengage = false;
            TrySetInputSource(isGamepad: false);
        }

        TrackGamepadAndKeyboard();
        RestoreFallbackSelectionIfNeeded();
    }

    // -------------------------------------------------------------------------
    // Low-level event listener (runs on input thread, keep it minimal)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Listens at the InputSystem backend level, which fires regardless of
    /// whether the device is enabled.  We only set a flag here; the actual
    /// mode switch happens on the main thread in LateUpdate.
    /// </summary>
    private void OnInputSystemEvent(InputEventPtr eventPtr, InputDevice device)
    {
        // Only interested in mouse re-engagement while in gamepad mode.
        if (!lastInputWasGamepad) return;
        if (!(device is Mouse mouse)) return;
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()) return;

        // Intentional clicks always re-engage mouse regardless of movement.
        bool click =
            mouse.leftButton.ReadValueFromEvent(eventPtr)   > 0.5f
            || mouse.rightButton.ReadValueFromEvent(eventPtr)  > 0.5f
            || mouse.middleButton.ReadValueFromEvent(eventPtr) > 0.5f;

        if (click)
        {
            pendingMouseReengage = true;
            return;
        }

        // Movement re-engages only when delta is above the noise threshold.
        Vector2 delta = mouse.delta.ReadValueFromEvent(eventPtr);
        if (delta.sqrMagnitude > mouseMoveDeadzonePixels * mouseMoveDeadzonePixels)
            pendingMouseReengage = true;
    }

    // -------------------------------------------------------------------------
    // Per-frame tracking (gamepad + keyboard only; mouse is handled by onEvent)
    // -------------------------------------------------------------------------

    private void TrackGamepadAndKeyboard()
    {
        // Prefer scanning all gamepads: Gamepad.current can stay null while input still
        // reaches PlayerInput / gameplay actions on another polled device.
        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (gamepad == null)
            {
                continue;
            }

            bool used =
                gamepad.buttonSouth.wasPressedThisFrame
                || gamepad.buttonNorth.wasPressedThisFrame
                || gamepad.buttonWest.wasPressedThisFrame
                || gamepad.buttonEast.wasPressedThisFrame
                || gamepad.startButton.wasPressedThisFrame
                || gamepad.selectButton.wasPressedThisFrame
                || gamepad.leftShoulder.wasPressedThisFrame
                || gamepad.rightShoulder.wasPressedThisFrame
                || gamepad.leftTrigger.wasPressedThisFrame
                || gamepad.rightTrigger.wasPressedThisFrame
                || gamepad.dpad.ReadValue().sqrMagnitude > 0f
                || gamepad.leftStick.ReadValue().sqrMagnitude  > 0.01f
                || gamepad.rightStick.ReadValue().sqrMagnitude > 0.01f;

            if (used)
            {
                TrySetInputSource(isGamepad: true);
                return;
            }
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            TrySetInputSource(isGamepad: false);
    }

    // -------------------------------------------------------------------------
    // Input source switching
    // -------------------------------------------------------------------------

    private void TrySetInputSource(bool isGamepad)
    {
        if (lastInputWasGamepad == isGamepad) return;

        float now = Time.unscaledTime;
        bool  firstInput = lastInputSwitchTime == float.NegativeInfinity;
        if (!firstInput && now - lastInputSwitchTime < inputSwitchDebounceSeconds)
            return;

        lastInputSwitchTime  = now;
        lastInputWasGamepad  = isGamepad;

        if (isGamepad)
        {
            EnterGamepadMode();
        }
        else
        {
            EnterMouseMode();
        }
    }

    // -------------------------------------------------------------------------
    // Mode transitions
    // -------------------------------------------------------------------------

    private void EnterGamepadMode()
    {
        // 1. Warp the cursor off-screen BEFORE locking.
        //    This forces InputSystemUIInputModule to process one final
        //    pointer-move at an off-screen position, sending OnPointerExit to
        //    whatever element was hovered — clearing its highlight state.
        Mouse mouse = Mouse.current;
        if (mouse != null)
            Mouse.current.WarpCursorPosition(new Vector2(-1f, -1f));

        // 2. Lock + hide.  Locked cursor stops all UI raycasting.
        SetCursorForMode(isGamepad: true);
    }

    private void EnterMouseMode()
    {
        // Unlock and show first so the cursor appears at its last real position.
        SetCursorForMode(isGamepad: false);

        // Drop any lingering gamepad selection so highlight states don't overlap.
        EventSystem es = EventSystem.current;
        if (es != null && es.currentSelectedGameObject != null)
            es.SetSelectedGameObject(null);

        lastSelectionLostTime = -1f;
    }

    // -------------------------------------------------------------------------
    // Cursor state
    // -------------------------------------------------------------------------

    private static void SetCursorForMode(bool isGamepad)
    {
        if (isGamepad)
        {
            Cursor.lockState = CursorLockMode.Locked; // stops UI raycasting
            Cursor.visible   = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public bool IsGamepadInputActive => lastInputWasGamepad;

    public void SetCurrentFallback(Selectable selectable)
    {
        fallbackSelectable = selectable;
    }

    public void ForceSelectCurrentFallback()
    {
        if (!lastInputWasGamepad) return;
        if (fallbackSelectable == null
            || !fallbackSelectable.IsInteractable()
            || !fallbackSelectable.gameObject.activeInHierarchy) return;

        EventSystem es = EventSystem.current;
        if (es == null) return;

        es.SetSelectedGameObject(null);
        es.SetSelectedGameObject(fallbackSelectable.gameObject);
    }

    public void EnterGamepadModeAndSelect(Selectable selectable)
    {
        if (selectable == null
            || !selectable.IsInteractable()
            || !selectable.gameObject.activeInHierarchy)
        {
            return;
        }

        lastInputWasGamepad = true;
        lastInputSwitchTime = Time.unscaledTime;
        EnterGamepadMode();

        fallbackSelectable = selectable;
        lastSelectionLostTime = -1f;

        EventSystem es = EventSystem.current;
        if (es == null)
        {
            return;
        }

        es.SetSelectedGameObject(null);
        es.SetSelectedGameObject(selectable.gameObject);
    }

    /// <summary>Clears the EventSystem selection. Safe to call at any time.</summary>
    public void ClearSelection()
    {
        if (EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
    }

    // -------------------------------------------------------------------------
    // Fallback selection restore
    // -------------------------------------------------------------------------

    private void RestoreFallbackSelectionIfNeeded()
    {
        if (fallbackSelectable == null || EventSystem.current == null) return;

        if (DropdownGamepadSupport.IsAnyExpanded())
        {
            return;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected != null && selected.activeInHierarchy)
        {
            // In mouse mode there should be no persistent selection.
            if (!lastInputWasGamepad)
            {
                EventSystem.current.SetSelectedGameObject(null);
                lastSelectionLostTime = -1f;
            }
            else
            {
                lastSelectionLostTime = -1f;
            }
            return;
        }

        if (!lastInputWasGamepad) return;

        if (lastSelectionLostTime < 0f)
        {
            lastSelectionLostTime = Time.unscaledTime;
            return;
        }

        if (Time.unscaledTime - lastSelectionLostTime < restoreDebounceSeconds)
            return;

        if (!fallbackSelectable.gameObject.activeInHierarchy
            || !fallbackSelectable.IsInteractable()) return;

        EventSystem.current.SetSelectedGameObject(fallbackSelectable.gameObject);
    }
}