using UnityEngine;

/// <summary>
/// Pause uses <see cref="Time.timeScale"/> = 0 while gameplay scripts use their own
/// <see cref="UnityEngine.InputSystem.InputActionAsset"/> instances (not PlayerInput's switched map).
/// Gate gameplay reads off scaled time when the game is paused.
/// </summary>
public static class GameplayInputGate
{
    public static bool BlocksGameplayActions => Time.timeScale <= 0f;
}
