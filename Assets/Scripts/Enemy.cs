using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 3;   
    private int currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth; 
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("damage " + damage + " cCurrent hp " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("killed");
        Destroy(gameObject); 
    }
}