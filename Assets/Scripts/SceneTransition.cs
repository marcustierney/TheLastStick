using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class SceneTransition : MonoBehaviour
{
    public string nextScene;
    public float minimumTime = 5f;
    //public TMP_Text progressText;

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