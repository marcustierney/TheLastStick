using UnityEngine;
using UnityEngine.EventSystems;

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
        if (playHoverOnSelect)
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