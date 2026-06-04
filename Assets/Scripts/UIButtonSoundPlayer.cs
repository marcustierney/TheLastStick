using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Uses shared UISfxPlayer singleton; no per-button AudioSource required]
public class UIButtonSoundPlayer : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISelectHandler, ISubmitHandler
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float hoverVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 1f;

    [Header("Behavior")]
    [SerializeField] private bool playHoverOnSelect = true;

    // No local AudioSource required; UISfxPlayer handles playback.

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHoverSound();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (ShouldPlayHoverOnSelect())
        {
            PlayHoverSound();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClickSound();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayClickSound();
    }

    private void PlayHoverSound()
    {
        PlaySound(hoverClip, hoverVolume);
    }

    private void PlayClickSound()
    {
        PlaySound(clickClip, clickVolume);
    }

    private bool ShouldPlayHoverOnSelect()
    {
        if (playHoverOnSelect)
        {
            return true;
        }

        return IsControllerNavigationActive();
    }

    private static bool IsControllerNavigationActive()
    {
        UIFocusGuard focusGuard = Object.FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
        if (focusGuard != null && focusGuard.IsGamepadInputActive)
        {
            return true;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad == null)
        {
            return false;
        }

        if (gamepad.dpad.ReadValue().sqrMagnitude > 0.01f)
        {
            return true;
        }

        if (gamepad.leftStick.ReadValue().sqrMagnitude > 0.01f)
        {
            return true;
        }

        if (gamepad.rightStick.ReadValue().sqrMagnitude > 0.01f)
        {
            return true;
        }

        return gamepad.buttonSouth.isPressed
            || gamepad.buttonNorth.isPressed
            || gamepad.buttonWest.isPressed
            || gamepad.buttonEast.isPressed
            || gamepad.startButton.isPressed
            || gamepad.selectButton.isPressed
            || gamepad.leftShoulder.isPressed
            || gamepad.rightShoulder.isPressed
            || gamepad.leftTrigger.isPressed
            || gamepad.rightTrigger.isPressed;
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        if (UISfxPlayer.Instance != null)
        {
            UISfxPlayer.Instance.PlayOneShot(clip, volume);
        }
    }
}