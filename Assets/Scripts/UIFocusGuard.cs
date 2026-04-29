using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIFocusGuard : MonoBehaviour
{
    [SerializeField] Selectable fallbackSelectable;
    [SerializeField] float restoreDebounceSeconds = 0.08f;

    private bool lastInputWasGamepad = false;
    private float lastSelectionLostTime = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        UIFocusGuard existingGuard = FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
        if (existingGuard != null)
        {
            return;
        }

        GameObject guardObject = new GameObject("UIFocusGuard");
        DontDestroyOnLoad(guardObject);
        guardObject.AddComponent<UIFocusGuard>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // When a new scene loads, the old fallback reference is invalid and the
    // EventSystem has been replaced. Clear stale state so LateUpdate doesn't
    // attempt to force-select a destroyed object, which corrupts navigation.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        fallbackSelectable = null;
        lastSelectionLostTime = -1f;
    }

    public void SetCurrentFallback(Selectable selectable)
    {
        fallbackSelectable = selectable;
    }

    public void ForceSelectCurrentFallback()
    {
        if (!lastInputWasGamepad)
        {
            return;
        }

        if (fallbackSelectable == null || !fallbackSelectable.IsInteractable() || !fallbackSelectable.gameObject.activeInHierarchy)
        {
            return;
        }

        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem == null)
        {
            return;
        }

        currentEventSystem.SetSelectedGameObject(null);
        currentEventSystem.SetSelectedGameObject(fallbackSelectable.gameObject);
    }

    public void ClearSelection()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
    }

    private void LateUpdate()
    {
        TrackLastInputDevice();

        if (fallbackSelectable == null || EventSystem.current == null)
        {
            return;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject != null && selectedObject.activeInHierarchy)
        {
            if (!lastInputWasGamepad)
            {
                EventSystem.current.SetSelectedGameObject(null);
                lastSelectionLostTime = -1f;
                return;
            }

            lastSelectionLostTime = -1f;
            return;
        }

        if (!lastInputWasGamepad)
        {
            return;
        }

        if (lastSelectionLostTime < 0f)
        {
            lastSelectionLostTime = Time.unscaledTime;
            return;
        }

        if (Time.unscaledTime - lastSelectionLostTime < restoreDebounceSeconds)
        {
            return;
        }

        if (!fallbackSelectable.gameObject.activeInHierarchy || !fallbackSelectable.IsInteractable())
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(fallbackSelectable.gameObject);
    }

    private void TrackLastInputDevice()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            bool mouseUsed =
                mouse.leftButton.wasPressedThisFrame
                || mouse.rightButton.wasPressedThisFrame
                || mouse.middleButton.wasPressedThisFrame
                || mouse.scroll.ReadValue().sqrMagnitude > 0f
                || mouse.delta.ReadValue().sqrMagnitude > 0f;
            if (mouseUsed)
            {
                lastInputWasGamepad = false;
                return;
            }
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
        {
            lastInputWasGamepad = false;
            return;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad == null)
        {
            return;
        }

        bool gamepadUsed =
            gamepad.buttonSouth.wasPressedThisFrame
            || gamepad.buttonNorth.wasPressedThisFrame
            || gamepad.buttonWest.wasPressedThisFrame
            || gamepad.buttonEast.wasPressedThisFrame
            || gamepad.startButton.wasPressedThisFrame
            || gamepad.selectButton.wasPressedThisFrame
            || gamepad.leftShoulder.wasPressedThisFrame
            || gamepad.rightShoulder.wasPressedThisFrame
            || gamepad.dpad.ReadValue().sqrMagnitude > 0f
            || gamepad.leftStick.ReadValue().sqrMagnitude > 0.0001f
            || gamepad.rightStick.ReadValue().sqrMagnitude > 0.0001f;

        if (gamepadUsed)
        {
            lastInputWasGamepad = true;
        }
    }
}