using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(AudioSource))]
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

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.ignoreListenerPause = true;
    }

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
        if (clip == null || audioSource == null)
        {
            return;
        }

        float sfxScale = 1f;
        if (AudioSettingsManager.Instance != null)
        {
            sfxScale = AudioSettingsManager.Instance.SfxVolume01;
        }

        audioSource.PlayOneShot(clip, volume * sfxScale);
    }
}