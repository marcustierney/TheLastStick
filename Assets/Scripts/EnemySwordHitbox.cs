using UnityEngine;
public class EnemySwordHitbox : MonoBehaviour
{
    private float damage = 10f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            print("test");
            UpdateHealth playerHealth = collision.GetComponent<UpdateHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, transform.position); // Pass the position of the hit for knockback direction
            }
        }
    }
}