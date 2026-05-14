using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class SceneTransition : MonoBehaviour
{
    public string nextScene;
    public float minimumTime = 5f;

    [SerializeField] private TMP_Text statusText;
    [SerializeField] private float savingToLoadingDelay = 1f;

    private static string pendingNextScene;
    private static bool hasPendingNextScene;
    private static float? pendingMinimumTime;

    public static void SetPendingNextScene(string sceneName, float? minimumTime = null)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"{nameof(SceneTransition)}.{nameof(SetPendingNextScene)}: scene name is empty; ignored.");
            return;
        }

        pendingNextScene = sceneName;
        hasPendingNextScene = true;
        pendingMinimumTime = minimumTime;
    }

    private void Awake()
    {
        if (hasPendingNextScene && !string.IsNullOrEmpty(pendingNextScene))
        {
            nextScene = pendingNextScene;
            pendingNextScene = null;
            hasPendingNextScene = false;
        }

        if (pendingMinimumTime.HasValue)
        {
            minimumTime = Mathf.Max(0f, pendingMinimumTime.Value);
            pendingMinimumTime = null;
        }

        if (statusText != null)
            statusText.text = savingToLoadingDelay > 0f ? "Saving" : "Loading";
    }

    private void Start()
    {
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(nextScene);
        loadOperation.allowSceneActivation = false;

        yield return null;
        float switchToLoadingAt = Time.unscaledTime + savingToLoadingDelay;
        bool labelSwitched = savingToLoadingDelay <= 0f;

        float timer = 0f;
        while (timer < minimumTime || loadOperation.progress < 0.9f)
        {
            timer += Time.unscaledDeltaTime;

            if (!labelSwitched && statusText != null && Time.unscaledTime >= switchToLoadingAt)
            {
                statusText.text = "Loading";
                labelSwitched = true;
            }

            yield return null;
        }

        if (!labelSwitched && statusText != null)
            statusText.text = "Loading";

        GameAnalytics.FlushIfReady();
        loadOperation.allowSceneActivation = true;
    }
}