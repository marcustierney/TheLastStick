using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UpdateHealth : MonoBehaviour
{
    public float Health, MaxHealth;

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

    private Coroutine damageSoundCoroutine;
    private Rigidbody2D rb;
    private Movement movement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        healthBar.SetMaxHealth(MaxHealth);
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<Movement>();
        lastDamageTime = Time.time;
        
        if (rb == null)
            Debug.LogError("Rigidbody2D not found on player!");
        if (movement == null)
            Debug.LogError("Movement script not found on player!");
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
        healthBar.SetHealth(Health);
    }

    public void IncreaseMaxHealth(float amount)
    {
        MaxHealth += amount;
        healthBar.SetMaxHealth(MaxHealth);
        SetHealth(amount); // also give the player the new HP
    }

    public void TakeDamage(float damage, Vector2 hitSourcePosition) // Apply damage to the player, trigger knockback, and start I-frames
    {
        if (IsInvulnerable())
            return;

        SetHealth(-damage);
        lastDamageTime = Time.time;
        PlayDamageSound();
        
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

        // TODO: Add visual feedback here (player flash, damage popup, etc.)

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
        TakeDamage(damage, transform.position);
    }

    private void Die() // Placeholder for player death logic
    {
        Debug.Log("Player died");
        SceneManager.LoadScene("DeathScreen");
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
