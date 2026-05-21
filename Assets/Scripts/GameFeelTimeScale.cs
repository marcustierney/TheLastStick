using System.Collections;
using UnityEngine;

/// <summary>
/// Coordinates gameplay slow-motion without fighting pause/death (timeScale &lt;= 0).
/// </summary>
public class GameFeelTimeScale : MonoBehaviour
{
    public static GameFeelTimeScale Instance { get; private set; }

    private const float DefaultFixedDeltaTime = 0.02f;

    private Coroutine slowMoCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RequestSlowMo(float scale, float durationRealtime)
    {
        if (durationRealtime <= 0f)
        {
            return;
        }

        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (slowMoCoroutine != null)
        {
            StopCoroutine(slowMoCoroutine);
        }

        slowMoCoroutine = StartCoroutine(SlowMoRoutine(scale, durationRealtime));
    }

    public void CancelSlowMo()
    {
        if (slowMoCoroutine != null)
        {
            StopCoroutine(slowMoCoroutine);
            slowMoCoroutine = null;
        }

        RestoreNormalTime();
    }

    private IEnumerator SlowMoRoutine(float scale, float durationRealtime)
    {
        scale = Mathf.Clamp(scale, 0.05f, 1f);
        Time.timeScale = scale;
        Time.fixedDeltaTime = DefaultFixedDeltaTime * scale;

        float elapsed = 0f;
        while (elapsed < durationRealtime)
        {
            if (Time.timeScale <= 0f)
            {
                slowMoCoroutine = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        RestoreNormalTime();
        slowMoCoroutine = null;
    }

    private static void RestoreNormalTime()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = DefaultFixedDeltaTime;
    }
}
