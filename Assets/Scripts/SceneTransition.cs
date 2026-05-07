using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class SceneTransition : MonoBehaviour
{
    public string nextScene;
    public float minimumTime = 5f;
    //public TMP_Text progressText;

    private static string pendingNextScene;
    private static bool hasPendingNextScene;
    private static float? pendingMinimumTime;

    /// <summary>
    /// Call before <see cref="SceneManager.LoadScene"/> for the loading screen scene.
    /// On awake, <see cref="SceneTransition"/> applies pending <c>nextScene</c> and optional <c>minimumTime</c> once, then clears them.
    /// </summary>
    /// <param name="sceneName">Destination scene after the loading screen.</param>
    /// <param name="minimumTime">
    /// If set, replaces inspector <see cref="minimumTime"/> for this load only. Omit to keep the scene default.
    /// </param>
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
    }

    void Start()
    {
        StartCoroutine(LoadNextScene());
    }

    IEnumerator LoadNextScene()
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(nextScene);
        loadOperation.allowSceneActivation = false;

        float timer = 0f;
        while (timer < minimumTime || loadOperation.progress < 0.9f)
        {
            timer += Time.deltaTime;
            //float progress = loadOperation.progress / 0.9f;
            //progressText.text = "Loading " + Mathf.RoundToInt(progress * 100f) + "%";
            yield return null;
        }

        loadOperation.allowSceneActivation = true;
    }
}