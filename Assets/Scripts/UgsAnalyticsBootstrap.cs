using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Initializes Unity Gaming Services + Analytics, emits device_profile and performance_snapshot once per session.
/// </summary>
public class UgsAnalyticsBootstrap : MonoBehaviour
{
    static UgsAnalyticsBootstrap s_instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureBootstrapObject()
    {
        if (s_instance != null)
        {
            return;
        }

        GameObject root = new GameObject(nameof(UgsAnalyticsBootstrap));
        root.AddComponent<LevelRunStats>();
        root.AddComponent<UgsAnalyticsBootstrap>();
    }

    void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    async void Start()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

#if ENABLE_UNITY_CONSENT
            // Developer Data framework (Unity 6.2+): grant analytics consent; SDK activates from this + init.
            UnityEngine.UnityConsent.ConsentState consentState = UnityEngine.UnityConsent.EndUserConsent.GetConsentState();
            consentState.AnalyticsIntent = UnityEngine.UnityConsent.ConsentStatus.Granted;
            UnityEngine.UnityConsent.EndUserConsent.SetConsentState(consentState);
#else
#pragma warning disable CS0618
            AnalyticsService.Instance.StartDataCollection();
#pragma warning restore CS0618
#endif
            GameAnalytics.SetDataCollectionActive(true);
            EmitDeviceProfile();
            StartCoroutine(PerformanceSnapshotRoutine());
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UgsAnalyticsBootstrap] Init failed: {ex.Message}");
        }
    }

    void EmitDeviceProfile()
    {
        Dictionary<string, object> p = new Dictionary<string, object>
        {
            { "device_model", SystemInfo.deviceModel },
            { "device_type", (int)SystemInfo.deviceType },
            { "operating_system", SystemInfo.operatingSystem },
            { "operating_system_family", (int)SystemInfo.operatingSystemFamily },
            { "processor_type", SystemInfo.processorType },
            { "processor_count", SystemInfo.processorCount },
            { "system_memory_size", SystemInfo.systemMemorySize },
            { "graphics_device_name", SystemInfo.graphicsDeviceName },
            { "graphics_device_type", (int)SystemInfo.graphicsDeviceType },
            { "graphics_device_vendor", SystemInfo.graphicsDeviceVendor },
            { "graphics_memory_size", SystemInfo.graphicsMemorySize },
            { "graphics_shader_level", SystemInfo.graphicsShaderLevel },
            { "max_texture_size", SystemInfo.maxTextureSize },
            { "supports_compute_shaders", SystemInfo.supportsComputeShaders },
            { "supports_instancing", SystemInfo.supportsInstancing },
            { "screen_width", Screen.width },
            { "screen_height", Screen.height },
            { "screen_refresh_rate", Screen.currentResolution.refreshRateRatio.value },
            { "unity_version", Application.unityVersion },
            { "application_version", Application.version },
        };

        GameAnalytics.RecordCustom(AnalyticsKeys.EventDeviceProfile, p);
    }

    IEnumerator PerformanceSnapshotRoutine()
    {
        yield return new WaitForSecondsRealtime(1.5f);

        while (Time.timeScale <= 0f)
        {
            yield return null;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        int qualityLevel = QualitySettings.GetQualityLevel();
        string[] names = QualitySettings.names;
        string qualityName = qualityLevel >= 0 && qualityLevel < names.Length ? names[qualityLevel] : string.Empty;

        float sampleSeconds = 5f;
        float elapsed = 0f;
        double sumFps = 0d;
        int frames = 0;
        float minFps = float.MaxValue;

        while (elapsed < sampleSeconds)
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 1e-6f)
            {
                float fps = 1f / dt;
                sumFps += fps;
                frames++;
                if (fps < minFps)
                {
                    minFps = fps;
                }
            }

            elapsed += dt;
            yield return null;
        }

        float fpsAvg = frames > 0 ? (float)(sumFps / frames) : 0f;
        if (minFps >= float.MaxValue * 0.5f)
        {
            minFps = 0f;
        }

        Dictionary<string, object> payload = new Dictionary<string, object>
        {
            { "fps_avg", fpsAvg },
            { "fps_min", minFps },
            { "quality_level", qualityLevel },
            { "quality_name", qualityName },
            { "vsync_count", QualitySettings.vSyncCount },
            { "target_frame_rate", Application.targetFrameRate },
            { "scene_name", sceneName },
        };

        GameAnalytics.RecordCustom(AnalyticsKeys.EventPerformanceSnapshot, payload);
    }
}
