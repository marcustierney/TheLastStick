using UnityEngine;
using UnityEngine.UI;

public class OptionsAudioUI : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private bool isInitializing;

    private void Start()
    {
        EnsureManagerExists();
        BindSliderValuesFromSettings();
        RegisterSliderListeners();
    }

    private void OnDestroy()
    {
        UnregisterSliderListeners();
    }

    private void EnsureManagerExists()
    {
        if (AudioSettingsManager.Instance == null)
        {
            Debug.LogWarning("AudioSettingsManager is missing. A runtime instance should be created automatically before scene load.");
        }
    }

    private void BindSliderValuesFromSettings()
    {
        if (AudioSettingsManager.Instance == null)
        {
            return;
        }

        isInitializing = true;

        if (masterSlider != null)
        {
            masterSlider.value = AudioSettingsManager.Instance.MasterVolume01;
        }

        if (musicSlider != null)
        {
            musicSlider.value = AudioSettingsManager.Instance.MusicVolume01;
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = AudioSettingsManager.Instance.SfxVolume01;
        }

        isInitializing = false;
    }

    private void RegisterSliderListeners()
    {
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }
    }

    private void UnregisterSliderListeners()
    {
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        }
    }

    private void OnMasterChanged(float value)
    {
        if (isInitializing || AudioSettingsManager.Instance == null)
        {
            return;
        }

        AudioSettingsManager.Instance.SetMasterVolume(value);
    }

    private void OnMusicChanged(float value)
    {
        if (isInitializing || AudioSettingsManager.Instance == null)
        {
            return;
        }

        AudioSettingsManager.Instance.SetMusicVolume(value);
    }

    private void OnSfxChanged(float value)
    {
        if (isInitializing || AudioSettingsManager.Instance == null)
        {
            return;
        }

        AudioSettingsManager.Instance.SetSfxVolume(value);
    }
}