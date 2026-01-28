using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateHealth : MonoBehaviour
{
    public float Health, MaxHealth;

    [SerializeField]
    private HealthBarUI healthBar;

    [SerializeField]
    private float iFrameDuration = 1f; // Duration of invulnerability frames in seconds... Obv
    private float iFrameCounter = 0f; // Tracks remaining I-frame time... Again Obv

    [SerializeField]
    private float knockbackForce = 20f; // Horizontal force to knock player back
    [SerializeField]
    private float knockbackUpForce = 20f; // Upward force to knock player up
    private Rigidbody2D rb;
    private Movement movement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        healthBar.SetMaxHealth(MaxHealth);
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<Movement>();
        
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
        }

        // if (input.GetKeyDown("d"))
        // {
        //     SetHealth(-10f);
        // }
        // if (Input.GetKeyDown("a"))
        // {
        //     SetHealth(10f);
        // }
    }

    public void SetHealth(float healthChange)
    {
        Health += healthChange;
        Health = Mathf.Clamp(Health, 0, MaxHealth);
        healthBar.SetHealth(Health);
    }

    public void TakeDamage(float damage)
    {
        if (IsInvulnerable())
            return;

        SetHealth(-damage);
        
        if (rb != null && movement != null)
        {
            // Reset velocity first to ensure knockback isn't affected by current movement
            rb.linearVelocity = Vector2.zero;
            
            float knockbackDirection = movement.IsFacingRight ? -1f : 1f;
            Vector2 knockback = new Vector2(knockbackDirection * knockbackForce, knockbackUpForce);
            rb.linearVelocity = knockback; // Directly set velocity for immediate effect
            
            // Tell movement to ignore input during knockback
            movement.ApplyKnockback(0.2f);
        }

        iFrameCounter = iFrameDuration;

        // TODO: Add visual feedback here (player flash, damage popup, etc.)

        if (Health <= 0f)
        {
            Die();
        }
    }

    public bool IsInvulnerable()
    {
        return iFrameCounter > 0f;
    }

    private void Die()
    {
        Debug.Log("Player died");
        // TODO: Handle game over logic here
    }
}
