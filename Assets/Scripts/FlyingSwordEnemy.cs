using UnityEngine;


public class FlyingSwordEnemy : MonoBehaviour
{
    public float speed = 3f;
    public float damage = 1f;

    private Transform player;
    private Rigidbody2D rb;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * speed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rb.rotation = angle + 150f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        print("collide");
        if (collision.gameObject.CompareTag("Player"))
        {
            UpdateHealth health = collision.gameObject.GetComponent<UpdateHealth>();
            if (health != null)
            {
                health.TakeDamage(10, transform.position);
            }
        }
    }
}