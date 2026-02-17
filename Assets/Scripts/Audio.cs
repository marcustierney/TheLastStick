using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicPersistence : MonoBehaviour
{
    private static MusicPersistence instance;

    [SerializeField] private string[] menuSceneNames = { "MainMenu", "Options", "Credits" };

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instance != this)
        {
            return;
        }

        if (!IsMenuScene(scene.name))
        {
            instance = null;
            Destroy(gameObject);
        }
    }

    private bool IsMenuScene(string sceneName)
    {
        if (menuSceneNames == null)
        {
            return false;
        }

        for (int i = 0; i < menuSceneNames.Length; i++)
        {
            if (string.Equals(sceneName, menuSceneNames[i]))
            {
                return true;
            }
        }

        return false;
    }
}
