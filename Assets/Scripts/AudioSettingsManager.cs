using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string mixerResourcePath = "Audio/GameAudioMixer";

    [Header("Exposed Parameters")]
    [SerializeField] private string masterParam = "Master";
    [SerializeField] private string musicParam = "Music";
    [SerializeField] private string sfxParam = "SFX";

    private const string MasterKey = "tls.audio.master";
    private const string MusicKey = "tls.audio.music";
    private const string SfxKey = "tls.audio.sfx";
    private const float MinDecibels = -80f;

    public float MasterVolume01 { get; private set; } = 1f;
    public float MusicVolume01 { get; private set; } = 1f;
    public float SfxVolume01 { get; private set; } = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("AudioSettingsManager");
        managerObject.AddComponent<AudioSettingsManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (mixer == null)
        {
            mixer = Resources.Load<AudioMixer>(mixerResourcePath);
        }

        LoadValues();
        ApplyAllVolumes();
    }

    public void SetMasterVolume(float value01)
    {
        MasterVolume01 = Mathf.Clamp01(value01);
        ApplyVolume(masterParam, MasterVolume01);
        SaveValues();
    }

    public void SetMusicVolume(float value01)
    {
        MusicVolume01 = Mathf.Clamp01(value01);
        ApplyVolume(musicParam, MusicVolume01);
        SaveValues();
    }

    public void SetSfxVolume(float value01)
    {
        SfxVolume01 = Mathf.Clamp01(value01);
        ApplyVolume(sfxParam, SfxVolume01);
        SaveValues();
    }

    private void ApplyAllVolumes()
    {
        ApplyVolume(masterParam, MasterVolume01);
        ApplyVolume(musicParam, MusicVolume01);
        ApplyVolume(sfxParam, SfxVolume01);
    }

    private void ApplyVolume(string parameterName, float volume01)
    {
        if (mixer == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        float volumeDb = volume01 <= 0.0001f ? MinDecibels : Mathf.Log10(volume01) * 20f;
        mixer.SetFloat(parameterName, volumeDb);
    }

    private void LoadValues()
    {
        MasterVolume01 = PlayerPrefs.GetFloat(MasterKey, 1f);
        MusicVolume01 = PlayerPrefs.GetFloat(MusicKey, 1f);
        SfxVolume01 = PlayerPrefs.GetFloat(SfxKey, 1f);
    }

    private void SaveValues()
    {
        PlayerPrefs.SetFloat(MasterKey, MasterVolume01);
        PlayerPrefs.SetFloat(MusicKey, MusicVolume01);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume01);
        PlayerPrefs.Save();
    }
}