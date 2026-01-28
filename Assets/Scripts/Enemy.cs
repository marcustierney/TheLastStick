using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 3;   
    private int currentHealth;

    [SerializeField]
    private float damageToPlayer = 10f; // Damage dealt to player on contact

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Deal damage to player on contact
        if (collision.gameObject.CompareTag("Player"))
        {
            DealDamageToPlayer(collision.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Deal damage to player if touching trigger
        if (collision.CompareTag("Player"))
        {
            DealDamageToPlayer(collision.gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Continue dealing damage while touching, respecting I-frames
        if (collision.CompareTag("Player"))
        {
            DealDamageToPlayer(collision.gameObject);
        }
    }

    private void DealDamageToPlayer(GameObject player)
    {
        UpdateHealth playerHealth = player.GetComponent<UpdateHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageToPlayer);
        }
    }
}