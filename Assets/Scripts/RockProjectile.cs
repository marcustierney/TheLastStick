using UnityEngine;

public class BallProjectile : MonoBehaviour
{
    public float speed = 6f;
    public int damage = 10;
    public float lifetime = 5f;
    private Rigidbody2D rb;
    private Vector2 direction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f; 
        }
    }

    public void Launch(Vector2 dir, float projectileSpeed)
    {
        direction = dir.normalized;
        speed = projectileSpeed;
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        //transform.Translate(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            UpdateHealth health = collision.GetComponent<UpdateHealth>();
            if (health != null)
            {
                health.TakeDamage(10, transform.position, AnalyticsKeys.DeathCauseRock);
            }

            Destroy(gameObject);
        }

        if (collision.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}