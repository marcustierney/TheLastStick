using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndScreenActions : MonoBehaviour
{
    [SerializeField] private bool isWinScreen;
    [SerializeField] private Selectable defaultSelectable;

    private void Awake()
    {
        WireButtons();
    }

    private void OnEnable()
    {
        EnsureNonZeroScale();
        ConfigureAnimatorsForUnscaledTime();
        EnsureUiMapActive();
        StartCoroutine(ApplyFocusAfterFrame());
    }

    public void TryAgain()
    {
        if (isWinScreen)
        {
            TryAgainFromWin();
            return;
        }

        TryAgainFromDeath();
    }

    public void GoToMainMenu()
    {
        PauseManager pauseManager = Object.FindAnyObjectByType<PauseManager>();
        if (pauseManager != null)
        {
            pauseManager.GoToMainMenu();
            return;
        }

        ResetRunState();
#if UNITY_EDITOR
        UnityEditor.Selection.activeObject = null;
#endif
        SceneTransition.SetPendingNextScene("MainMenu", 4f);
        SceneManager.LoadScene("LoadingScreen");
    }

    private static void TryAgainFromDeath()
    {
        RestoreGameplayState();
        GameAnalytics.FlushIfReady();
        CoinManager.ClearSavedProgress();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private static void TryAgainFromWin()
    {
        RestoreGameplayState();
        GameAnalytics.FlushIfReady();
        CoinManager.ClearSavedProgress();
        PlayerPrefs.SetInt("CurrentLevel", 1);
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.Selection.activeObject = null;
#endif
        SceneTransition.SetPendingNextScene(AnalyticsKeys.SceneLevelOne, 4f);
        SceneManager.LoadScene("LoadingScreen");
    }

    private static void ResetRunState()
    {
        RestoreGameplayState();
        GameAnalytics.FlushIfReady();
        LevelRunStats.Instance?.ResetSpeedrun();
        CoinManager.ClearSavedProgress();
        PlayerPrefs.SetInt("CurrentLevel", 0);
        PlayerPrefs.Save();
    }

    private static void RestoreGameplayState()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void WireButtons()
    {
        WireButton("TryAgainButton", TryAgain);
        WireButton("MainMenuButton", GoToMainMenu);
    }

    private void WireButton(string buttonName, UnityEngine.Events.UnityAction handler)
    {
        Transform buttonTransform = transform.Find(buttonName);
        if (buttonTransform == null)
        {
            buttonTransform = FindChildRecursive(transform, buttonName);
        }

        if (buttonTransform == null)
        {
            Debug.LogWarning($"EndScreenActions could not find button '{buttonName}' on {name}.", this);
            return;
        }

        Button button = buttonTransform.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning($"EndScreenActions found '{buttonName}' but it has no Button component.", this);
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(handler);
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private void EnsureNonZeroScale()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null)
        {
            return;
        }

        Vector3 localScale = rect.localScale;
        if (Mathf.Abs(localScale.x) < 1e-5f || Mathf.Abs(localScale.y) < 1e-5f)
        {
            rect.localScale = Vector3.one;
        }
    }

    private void ConfigureAnimatorsForUnscaledTime()
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            if (animator != null)
            {
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
        }
    }

    private static void EnsureUiMapActive()
    {
        PlayerInput playerInput = Object.FindAnyObjectByType<PlayerInput>();
        if (playerInput == null || playerInput.actions == null)
        {
            return;
        }

        InputActionMap uiMap = playerInput.actions.FindActionMap("UI", throwIfNotFound: false);
        if (uiMap == null)
        {
            return;
        }

        if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != "UI")
        {
            playerInput.SwitchCurrentActionMap("UI");
        }

        if (!uiMap.enabled)
        {
            uiMap.Enable();
        }
    }

    private IEnumerator ApplyFocusAfterFrame()
    {
        yield return null;

        if (!gameObject.activeInHierarchy)
        {
            yield break;
        }

        Selectable fallback = ResolveDefaultSelectable();
        if (fallback == null || !fallback.gameObject.activeInHierarchy)
        {
            yield break;
        }

        UIFocusGuard focusGuard = Object.FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
        if (focusGuard != null)
        {
            focusGuard.SetCurrentFallback(fallback);
            focusGuard.ForceSelectCurrentFallback();
        }
    }

    private Selectable ResolveDefaultSelectable()
    {
        if (defaultSelectable != null)
        {
            return defaultSelectable;
        }

        Selectable[] selectables = GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable != null && selectable.IsInteractable() && selectable.gameObject.activeInHierarchy)
            {
                return selectable;
            }
        }

        return selectables.Length > 0 ? selectables[0] : null;
    }
}
