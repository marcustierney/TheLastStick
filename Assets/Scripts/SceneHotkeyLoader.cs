using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneHotkeyLoader : MonoBehaviour
{
    private static readonly (Key key, string sceneName)[] SceneHotkeys =
    {
        (Key.Digit1, "Tutorial"),
        (Key.Digit2, "LevelOne"),
        (Key.Digit3, "LevelOneBoss"),
        (Key.Digit4, "LevelTwo"),
        (Key.Digit5, "LevelTwoBoss")
    };

    private static SceneHotkeyLoader instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject hotkeyObject = new GameObject(nameof(SceneHotkeyLoader));
        instance = hotkeyObject.AddComponent<SceneHotkeyLoader>();
        DontDestroyOnLoad(hotkeyObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        for (int i = 0; i < SceneHotkeys.Length; i++)
        {
            if (keyboard[SceneHotkeys[i].key].wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneHotkeys[i].sceneName);
                return;
            }
        }
    }
}