using UnityEngine;

/// <summary>
/// Pause uses <see cref="Time.timeScale"/> = 0 while gameplay scripts use their own
/// <see cref="UnityEngine.InputSystem.InputActionAsset"/> instances (not PlayerInput's switched map).
/// Gate gameplay reads off scaled time when the game is paused.
/// </summary>
public static class GameplayInputGate
{
    private static float blockUntilUnscaledTime = -1f;

    public static bool BlocksGameplayActions =>
        Time.timeScale <= 0f || Time.unscaledTime < blockUntilUnscaledTime;

    public static void BlockForUnscaledSeconds(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        float targetTime = Time.unscaledTime + duration;
        if (targetTime > blockUntilUnscaledTime)
        {
            blockUntilUnscaledTime = targetTime;
        }
    }
}
