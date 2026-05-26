using UnityEngine;

public class UISfxPlayer : MonoBehaviour
{
    public static UISfxPlayer Instance { get; private set; }

    private AudioSource audioSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (FindAnyObjectByType<UISfxPlayer>(FindObjectsInactive.Include) != null)
            return;

        GameObject go = new GameObject("UISfxPlayer");
        DontDestroyOnLoad(go);
        go.AddComponent<UISfxPlayer>();
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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.ignoreListenerPause = true;
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return;

        float sfxScale = 1f;
        if (AudioSettingsManager.Instance != null)
        {
            sfxScale = AudioSettingsManager.Instance.SfxVolume01;
        }

        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume) * sfxScale);
    }
}
