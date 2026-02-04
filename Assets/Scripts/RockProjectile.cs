using UnityEngine;

public class BallProjectile : MonoBehaviour
{
    public float speed = 6f;
    public int damage = 2;
    public float lifetime = 5f;

    private Vector2 direction;

    public void Launch(Vector2 dir)
    {
        direction = dir.normalized;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            UpdateHealth health = collision.GetComponent<UpdateHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }

            Destroy(gameObject);
        }

        if (collision.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}