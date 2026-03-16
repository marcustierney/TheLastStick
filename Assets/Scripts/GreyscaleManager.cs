using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class GreyscaleManager : MonoBehaviour
{
    private const string GreyscaleEnabledKey = "tls.greyscaleEnabled";

    private static GreyscaleManager instance;

    [SerializeField] private bool useSavedState = true;
    [SerializeField] private bool defaultEnabled = false;

    private bool isGreyscaleEnabled;

    public bool IsGreyscaleEnabled => isGreyscaleEnabled;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        LoadState();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplyStateToAllSceneVolumes();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyStateToAllSceneVolumes();
    }

    public void ToggleGreyscale()
    {
        SetGreyscale(!isGreyscaleEnabled);
    }

    public void SetGreyscale(bool enabled)
    {
        isGreyscaleEnabled = enabled;
        SaveState();
        ApplyStateToAllSceneVolumes();
    }

    private void LoadState()
    {
        if (useSavedState && PlayerPrefs.HasKey(GreyscaleEnabledKey))
        {
            isGreyscaleEnabled = PlayerPrefs.GetInt(GreyscaleEnabledKey) == 1;
            return;
        }

        isGreyscaleEnabled = defaultEnabled;
        SaveState();
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(GreyscaleEnabledKey, isGreyscaleEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyStateToAllSceneVolumes()
    {
        Volume[] sceneVolumes = FindObjectsOfType<Volume>(true);
        bool appliedAtLeastOnce = false;

        for (int i = 0; i < sceneVolumes.Length; i++)
        {
            Volume volume = sceneVolumes[i];
            if (volume == null || volume.profile == null)
            {
                continue;
            }

            if (!volume.profile.TryGet(out ColorAdjustments colorAdjustments))
            {
                continue;
            }

            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = isGreyscaleEnabled ? -100f : 0f;
            appliedAtLeastOnce = true;
        }

        if (!appliedAtLeastOnce)
        {
            Debug.LogWarning("GreyscaleManager: No Volume with Color Adjustments was found in this scene.");
        }
    }
}
