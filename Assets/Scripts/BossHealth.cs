using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour
{
    public float Health;
    public float MaxHealth;

    public HealthBarUI healthBar;

    void Start()
    {
        Health = MaxHealth;
        healthBar.SetMaxHealth(MaxHealth);
        healthBar.SetHealth(Health);
    }

    public void TakeDamage(float damage)
    {
        Health -= damage;
        Health = Mathf.Clamp(Health, 0, MaxHealth);

        healthBar.SetHealth(Health);

        if (Health <= 0)
        {
            //GetComponent<BossController>().Die();
        }
    }
}