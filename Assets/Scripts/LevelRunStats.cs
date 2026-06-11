using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// DontDestroyOnLoad per-run counters for whitelisted gameplay scenes.
/// </summary>
public class LevelRunStats : MonoBehaviour
{
    public static LevelRunStats Instance { get; private set; }

    static readonly HashSet<string> TrackedGameplayScenes = new HashSet<string>(StringComparer.Ordinal)
    {
        AnalyticsKeys.SceneTutorial,
        AnalyticsKeys.SceneLevelOne,
        AnalyticsKeys.SceneLevelOneBoss,
        AnalyticsKeys.SceneLevelTwo,
        AnalyticsKeys.SceneLevelTwoBoss,
        AnalyticsKeys.SceneLevelThree,
    };

    static readonly HashSet<string> SpeedrunScenes = new HashSet<string>(StringComparer.Ordinal)
    {
        AnalyticsKeys.SceneLevelOne,
        AnalyticsKeys.SceneLevelOneBoss,
        AnalyticsKeys.SceneLevelTwo,
        AnalyticsKeys.SceneLevelTwoBoss,
    };

    string _lastSceneName = string.Empty;
    float _levelStartRealtime;
    int _deathsThisRun;
    int _coinsThisRun;
    int _levelTwoFallRestarts;
    int _snapshotLevelTwoFallsWhenLeftLevelTwo;

    float _speedrunAccumulated;
    float _speedrunSegmentStart = -1f;
    bool _speedrunActive;
    bool _speedrunFinished;
    float _speedrunFinalSeconds;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string cur = scene.name;

        if (string.Equals(cur, AnalyticsKeys.SceneTutorial, StringComparison.Ordinal))
        {
            TutorialSignProgress.Reset();
        }

        string prev = _lastSceneName;

        if (string.Equals(prev, AnalyticsKeys.SceneLevelTwo, StringComparison.Ordinal)
            && !string.Equals(cur, AnalyticsKeys.SceneLevelTwo, StringComparison.Ordinal))
        {
            _snapshotLevelTwoFallsWhenLeftLevelTwo = _levelTwoFallRestarts;
        }

        if (string.Equals(prev, AnalyticsKeys.SceneTutorial, StringComparison.Ordinal)
            && !string.Equals(cur, AnalyticsKeys.SceneTutorial, StringComparison.Ordinal))
        {
            TutorialSignProgress.EmitSkippedAndClear();
        }

        if (TrackedGameplayScenes.Contains(cur))
        {
            _levelStartRealtime = Time.realtimeSinceStartup;
            _deathsThisRun = 0;
            _coinsThisRun = 0;
            _levelTwoFallRestarts = 0;
        }

        HandleSpeedrunSceneTransition(prev, cur);

        _lastSceneName = cur;
    }

    static bool IsSpeedrunScene(string sceneName)
    {
        return SpeedrunScenes.Contains(sceneName);
    }

    void HandleSpeedrunSceneTransition(string prev, string cur)
    {
        if (IsSpeedrunScene(prev))
        {
            PauseSpeedrunSegment();
        }

        if (string.Equals(cur, AnalyticsKeys.SceneTutorial, StringComparison.Ordinal)
            || string.Equals(cur, "MainMenu", StringComparison.Ordinal))
        {
            ResetSpeedrun();
            return;
        }

        if (string.Equals(cur, AnalyticsKeys.SceneLevelOne, StringComparison.Ordinal))
        {
            ResetSpeedrun();
            BeginSpeedrun();
            return;
        }

        if (IsSpeedrunScene(cur) && _speedrunActive && !_speedrunFinished)
        {
            ResumeSpeedrunSegment();
        }
    }

    void PauseSpeedrunSegment()
    {
        if (_speedrunSegmentStart < 0f)
        {
            return;
        }

        _speedrunAccumulated += Time.realtimeSinceStartup - _speedrunSegmentStart;
        _speedrunSegmentStart = -1f;
    }

    void ResumeSpeedrunSegment()
    {
        if (_speedrunFinished || _speedrunSegmentStart >= 0f)
        {
            return;
        }

        _speedrunSegmentStart = Time.realtimeSinceStartup;
    }

    void BeginSpeedrun()
    {
        _speedrunActive = true;
        _speedrunFinished = false;
        _speedrunAccumulated = 0f;
        _speedrunFinalSeconds = 0f;
        ResumeSpeedrunSegment();
    }

    public void ResetSpeedrun()
    {
        _speedrunActive = false;
        _speedrunFinished = false;
        _speedrunAccumulated = 0f;
        _speedrunFinalSeconds = 0f;
        _speedrunSegmentStart = -1f;
    }

    public void FinishSpeedrun()
    {
        if (!_speedrunActive || _speedrunFinished)
        {
            return;
        }

        PauseSpeedrunSegment();
        _speedrunFinished = true;
        _speedrunFinalSeconds = _speedrunAccumulated;
        _speedrunActive = false;
    }

    public bool IsSpeedrunActive => _speedrunActive;

    public bool IsSpeedrunFinished => _speedrunFinished;

    public float GetSpeedrunSeconds()
    {
        if (_speedrunFinished)
        {
            return _speedrunFinalSeconds;
        }

        if (_speedrunSegmentStart >= 0f)
        {
            return _speedrunAccumulated + (Time.realtimeSinceStartup - _speedrunSegmentStart);
        }

        return _speedrunAccumulated;
    }

    public static string FormatMmSs(float seconds)
    {
        int total = Mathf.FloorToInt(Mathf.Max(0f, seconds));
        return $"{total / 60}:{total % 60:00}";
    }

    public bool IsCurrentSceneTracked()
    {
        return TrackedGameplayScenes.Contains(SceneManager.GetActiveScene().name);
    }

    public void RegisterCoinPickup(int amount)
    {
        if (amount <= 0 || !IsCurrentSceneTracked())
        {
            return;
        }

        _coinsThisRun += amount;
    }

    public void RegisterDeath()
    {
        if (!IsCurrentSceneTracked())
        {
            return;
        }

        _deathsThisRun++;
    }

    public void RegisterLevelTwoFallReset()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, AnalyticsKeys.SceneLevelTwo, StringComparison.Ordinal))
        {
            return;
        }

        _levelTwoFallRestarts++;
    }

    public float GetElapsedSeconds()
    {
        return Mathf.Max(0f, Time.realtimeSinceStartup - _levelStartRealtime);
    }

    public int CurrentDeaths => _deathsThisRun;
    public int CurrentCoinsThisRun => _coinsThisRun;
    public int CurrentLevelTwoFallRestarts => _levelTwoFallRestarts;
    public int SnapshotLevelTwoFallsAfterLeavingLevelTwo => _snapshotLevelTwoFallsWhenLeftLevelTwo;

    public void EmitPlayerDeathAndRunExit(UpdateHealth playerHealth, string deathCause)
    {
        if (!IsCurrentSceneTracked())
        {
            return;
        }

        string levelName = SceneManager.GetActiveScene().name;
        float elapsed = GetElapsedSeconds();
        int coins = _coinsThisRun;
        int deaths = _deathsThisRun;

        Dictionary<string, object> common = new Dictionary<string, object>
        {
            { AnalyticsKeys.ParamLevelName, levelName },
            { AnalyticsKeys.ParamTimeSeconds, elapsed },
            { AnalyticsKeys.ParamDeathsThisRun, deaths },
            { AnalyticsKeys.ParamCoinsThisRun, coins },
        };

        if (_speedrunFinished)
        {
            common[AnalyticsKeys.ParamSpeedrunTimeSeconds] = GetSpeedrunSeconds();
        }

        if (string.Equals(levelName, AnalyticsKeys.SceneLevelTwo, StringComparison.Ordinal))
        {
            common[AnalyticsKeys.ParamLevelTwoFallRestarts] = _levelTwoFallRestarts;
        }

        Dictionary<string, object> deathPayload = new Dictionary<string, object>(common)
        {
            { AnalyticsKeys.ParamDeathCause, string.IsNullOrEmpty(deathCause) ? AnalyticsKeys.DeathCauseUnknown : deathCause },
        };

        if (playerHealth != null)
        {
            deathPayload[AnalyticsKeys.ParamFinalHealth] = (float)playerHealth.Health;
        }

        if (AnalyticsKeys.IsBossFightScene(levelName))
        {
            BossHealth bossHealth = UnityEngine.Object.FindAnyObjectByType<BossHealth>();
            if (bossHealth != null)
            {
                deathPayload[AnalyticsKeys.ParamBossHealth] = (float)bossHealth.Health;
                deathPayload[AnalyticsKeys.ParamBossMaxHealth] = (float)bossHealth.MaxHealth;
            }
        }

        GameAnalytics.RecordCustom(AnalyticsKeys.EventPlayerDeath, deathPayload);
        GameAnalytics.RecordCustom(AnalyticsKeys.EventLevelTimeSpent, new Dictionary<string, object>(common));
        GameAnalytics.RecordCustom(AnalyticsKeys.EventCoinsCollected, new Dictionary<string, object>(common));
    }

    public void EmitLevelCompleted(UpdateHealth playerHealth, string levelNameOverride = null)
    {
        if (!IsCurrentSceneTracked())
        {
            return;
        }

        string levelName = string.IsNullOrEmpty(levelNameOverride)
            ? SceneManager.GetActiveScene().name
            : levelNameOverride;

        float elapsed = GetElapsedSeconds();
        int coins = _coinsThisRun;
        int deaths = _deathsThisRun;
        float finalHealth = playerHealth != null ? playerHealth.Health : 0f;

        Dictionary<string, object> payload = new Dictionary<string, object>
        {
            { AnalyticsKeys.ParamLevelName, levelName },
            { AnalyticsKeys.ParamTimeSeconds, elapsed },
            { AnalyticsKeys.ParamDeathsThisRun, deaths },
            { AnalyticsKeys.ParamCoinsThisRun, coins },
            { AnalyticsKeys.ParamFinalHealth, finalHealth },
        };

        if (string.Equals(levelName, AnalyticsKeys.SceneLevelTwo, StringComparison.Ordinal))
        {
            payload[AnalyticsKeys.ParamLevelTwoFallRestarts] = _levelTwoFallRestarts;
        }
        else if (string.Equals(levelName, AnalyticsKeys.SceneLevelTwoBoss, StringComparison.Ordinal))
        {
            payload[AnalyticsKeys.ParamLevelTwoFallRestarts] = _snapshotLevelTwoFallsWhenLeftLevelTwo;
        }

        GameAnalytics.RecordCustom(AnalyticsKeys.EventLevelCompleted, payload);
        GameAnalytics.RecordCustom(AnalyticsKeys.EventLevelTimeSpent, new Dictionary<string, object>
        {
            { AnalyticsKeys.ParamLevelName, levelName },
            { AnalyticsKeys.ParamTimeSeconds, elapsed },
            { AnalyticsKeys.ParamDeathsThisRun, deaths },
            { AnalyticsKeys.ParamCoinsThisRun, coins },
        });
        GameAnalytics.RecordCustom(AnalyticsKeys.EventCoinsCollected, new Dictionary<string, object>
        {
            { AnalyticsKeys.ParamLevelName, levelName },
            { AnalyticsKeys.ParamTimeSeconds, elapsed },
            { AnalyticsKeys.ParamDeathsThisRun, deaths },
            { AnalyticsKeys.ParamCoinsThisRun, coins },
        });
    }

    public void EmitUpgradeTaken(string upgradeId, string upgradeName)
    {
        string levelName = SceneManager.GetActiveScene().name;
        Dictionary<string, object> payload = new Dictionary<string, object>
        {
            { AnalyticsKeys.ParamUpgradeId, upgradeId },
            { AnalyticsKeys.ParamUpgradeName, upgradeName },
            { AnalyticsKeys.ParamLevelName, levelName },
        };

        GameAnalytics.RecordCustom(AnalyticsKeys.EventUpgradeTaken, payload);
    }
}

/// <summary>
/// Tracks which tutorial signs were read so we can emit tutorial_sign_skipped on exit.
/// </summary>
public static class TutorialSignProgress
{
    static readonly Dictionary<string, bool> SignRead = new Dictionary<string, bool>(StringComparer.Ordinal);

    public static void Reset()
    {
        SignRead.Clear();
    }

    public static void Register(string signId)
    {
        if (string.IsNullOrEmpty(signId))
        {
            return;
        }

        if (!SignRead.ContainsKey(signId))
        {
            SignRead[signId] = false;
        }
    }

    public static void MarkRead(string signId)
    {
        if (string.IsNullOrEmpty(signId))
        {
            return;
        }

        SignRead[signId] = true;
    }

    public static void EmitSkippedAndClear()
    {
        foreach (KeyValuePair<string, bool> kv in SignRead)
        {
            if (kv.Value)
            {
                continue;
            }

            GameAnalytics.RecordCustom(AnalyticsKeys.EventTutorialSignSkipped, new Dictionary<string, object>
            {
                { AnalyticsKeys.ParamSignId, kv.Key },
            });
        }

        SignRead.Clear();
    }
}
