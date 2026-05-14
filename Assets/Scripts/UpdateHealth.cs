using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class UpdateHealth : MonoBehaviour
{
    public float Health, MaxHealth;
    private float baseMaxHealth;

    [SerializeField]
    private HealthBarUI healthBar;

    [SerializeField]
    private float iFrameDuration = 0.5f; // Duration of invulnerability frames in seconds... Obv
    private float iFrameCounter = 0f; // Tracks remaining I-frame time... Again Obv

    [SerializeField]
    private float regenDelay = 5f; // Seconds without damage before regen starts
    [SerializeField]
    private float regenRate = 2f; // HP per second
    private float lastDamageTime = 0f;

    [SerializeField]
    private float knockbackForce = 10f; // Horizontal force to knock player back
    [SerializeField]
    private float knockbackUpForce = 10f; // Upward force to knock player up
    [SerializeField]
    private AudioSource damageAudioSource;
    [SerializeField]
    private float damageSoundDuration = 0.5f;
    [SerializeField]
    private GameObject deathScreen;
    [SerializeField]
    private GameObject hud;
    [SerializeField]
    private UIFocusGuard focusGuard;
    [SerializeField]
    private Selectable deathDefaultSelectable;

    [SerializeField]
    [Tooltip("death screen delay in seconds")]
    private float deathScreenDelaySeconds = 1.5f;

    private Coroutine damageSoundCoroutine;
    private Coroutine deathSequenceCoroutine;
    private Rigidbody2D rb;
    private Movement movement;
    private bool isDead;
    private string lastDeathCauseForAnalytics;

    private void Awake()
    {
        baseMaxHealth = MaxHealth;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RefreshHealthBar();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<Movement>();
        lastDamageTime = Time.time;
        EnsureFocusGuard();
        
        if (rb == null)
            Debug.LogError("Rigidbody2D not found on player!");
        if (movement == null)
            Debug.LogError("Movement script not found on player!");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (deathSequenceCoroutine != null)
        {
            StopCoroutine(deathSequenceCoroutine);
            deathSequenceCoroutine = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // I-frame countdown
        if (iFrameCounter > 0f)
        {
            iFrameCounter -= Time.deltaTime;
            if (iFrameCounter <= 0f && movement != null)
            {
                movement.EndIFrameIgnoreCollisions();
            }
        }

        // Passive regeneration after delay without taking damage
        if (Time.time - lastDamageTime >= regenDelay && Health < MaxHealth)
        {
            SetHealth(regenRate * Time.deltaTime);
        }

    }

    public void SetHealth(float healthChange) // Adjust health by a specified amount and update the health bar
    {
        Health += healthChange;
        Health = Mathf.Clamp(Health, 0, MaxHealth);
        if (healthBar != null)
        {
            healthBar.SetHealth(Health);
        }
    }

    public void IncreaseMaxHealth(float amount)
    {
        MaxHealth += amount;
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(MaxHealth);
        }
        SetHealth(amount); // also give the player the new HP
    }

    public void ApplyHealthPercent(float percent)
    {
        float previousMaxHealth = MaxHealth;
        float multiplier = 1f + (percent / 100f);
        MaxHealth = baseMaxHealth * multiplier;

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(MaxHealth);
        }

        float gainedHealth = Mathf.Max(0f, MaxHealth - previousMaxHealth);
        SetHealth(gainedHealth);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        lastDeathCauseForAnalytics = null;
        RefreshHealthBar();
    }

    private void RefreshHealthBar()
    {
        if (healthBar == null)
        {
            return;
        }

        healthBar.SetMaxHealth(MaxHealth);
        healthBar.SetHealth(Mathf.Clamp(Health, 0f, MaxHealth));
    }

    public void TakeDamage(float damage, Vector2 hitSourcePosition, string deathCause = null) // Apply damage to the player, trigger knockback, and start I-frames
    {
        if (isDead || IsInvulnerable())
            return;

        if (!string.IsNullOrEmpty(deathCause))
        {
            lastDeathCauseForAnalytics = deathCause;
        }

        SetHealth(-damage);
        lastDamageTime = Time.time;
        PlayDamageSound();

        // Make the player face the source of the hit
        if (movement != null)
        {
            movement.FaceTowards(hitSourcePosition);
        }

        if (rb != null && movement != null)
        {
            // Reset velocity first to ensure knockback isn't affected by current movement
            rb.linearVelocity = Vector2.zero;

            float knockbackDirection = Mathf.Sign(transform.position.x - hitSourcePosition.x);
            if (knockbackDirection == 0f)
            {
                knockbackDirection = movement.IsFacingRight ? -1f : 1f;
            }
            Vector2 knockback = new Vector2(knockbackDirection * knockbackForce, knockbackUpForce);
            rb.linearVelocity = knockback; // Directly set velocity for immediate effect
            
            // Tell movement to ignore input during knockback
            movement.ApplyKnockback(0.1f);
        }

        iFrameCounter = iFrameDuration;
        if (movement != null)
        {
            movement.StartIFrameIgnoreCollisions();
        }

        // Visual feedback: trigger player damage animation if available
        if (movement != null)
        {
            movement.PlayDamageAnimation();
        }

        if (Health <= 0f)
        {
            Die();
        }
    }

    public bool IsInvulnerable() // Check if player is currently invulnerable due to I-frames
    {
        return iFrameCounter > 0f;
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position, null);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        Debug.Log("Player died");

        if (LevelRunStats.Instance != null)
        {
            LevelRunStats.Instance.RegisterDeath();
            string cause = string.IsNullOrEmpty(lastDeathCauseForAnalytics)
                ? AnalyticsKeys.DeathCauseUnknown
                : lastDeathCauseForAnalytics;
            LevelRunStats.Instance.EmitPlayerDeathAndRunExit(this, cause);
        }

        if (deathSequenceCoroutine != null)
        {
            StopCoroutine(deathSequenceCoroutine);
        }

        deathSequenceCoroutine = StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        TriggerDeathAnimation();

        if (deathScreenDelaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(deathScreenDelaySeconds);
        }

        Time.timeScale = 0f;

        if (deathScreen != null)
        {
            ConfigureDeathScreenAnimators();
            deathScreen.SetActive(true);
            EnsureUiMapActiveForDeathScreen();
            StartCoroutine(ApplyDeathScreenFocusAfterFrame());
        }
        else
        {
            Debug.LogWarning("Death screen is not assigned on UpdateHealth.");
        }

        if (hud != null)
        {
            hud.SetActive(false);
        }
        else
        {
            Debug.LogWarning("HUD is not assigned on UpdateHealth.");
        }

        deathSequenceCoroutine = null;
    }

    private void TriggerDeathAnimation()
    {
        if (movement == null)
        {
            return;
        }

        // Use reflection-only invocation to avoid compile-time dependency on
        // Movement.PlayDeathAnimation (prevents CS1061 if editor compile order
        // temporarily doesn't see the method).
        var mi = movement.GetType().GetMethod("PlayDeathAnimation",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (mi != null)
        {
            mi.Invoke(movement, null);
        }
        else
        {
            Debug.LogWarning("PlayDeathAnimation not found on Movement. Falling back to animator trigger.");
            Animator playerAnimator = movement.GetComponent<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                playerAnimator.SetTrigger("Death");
            }
            else
            {
                Debug.LogWarning("No Animator found on player to play death animation.");
            }
        }
    }

    private void ConfigureDeathScreenAnimators()
    {
        if (deathScreen == null)
        {
            return;
        }

        Animator[] animators = deathScreen.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            if (animator != null)
            {
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
        }
    }

    private static void EnsureUiMapActiveForDeathScreen()
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

    private IEnumerator ApplyDeathScreenFocusAfterFrame()
    {
        yield return null;

        if (deathScreen == null || !deathScreen.activeInHierarchy)
        {
            yield break;
        }

        EnsureFocusGuard();
        Selectable fallback = ResolveDeathDefaultSelectable();
        if (focusGuard != null && fallback != null && fallback.gameObject.activeInHierarchy)
        {
            focusGuard.SetCurrentFallback(fallback);
            focusGuard.ForceSelectCurrentFallback();
        }
    }

    private void EnsureFocusGuard()
    {
        if (focusGuard == null)
        {
            focusGuard = Object.FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
        }
    }

    private Selectable ResolveDeathDefaultSelectable()
    {
        if (deathDefaultSelectable != null)
        {
            return deathDefaultSelectable;
        }

        if (deathScreen == null)
        {
            return null;
        }

        Selectable[] selectables = deathScreen.GetComponentsInChildren<Selectable>(true);
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

    private void PlayDamageSound()
    {
        if (damageAudioSource == null)
        {
            return;
        }

        if (damageSoundCoroutine != null)
        {
            StopCoroutine(damageSoundCoroutine);
        }

        damageAudioSource.Play();
        damageSoundCoroutine = StartCoroutine(StopDamageSoundAfterDelay());
    }

    private IEnumerator StopDamageSoundAfterDelay()
    {
        yield return new WaitForSeconds(damageSoundDuration);

        if (damageAudioSource != null && damageAudioSource.isPlaying)
        {
            damageAudioSource.Stop();
        }

        damageSoundCoroutine = null;
    }
}
