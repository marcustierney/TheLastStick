using System.Collections;
using UnityEngine;

public class ThrowEnemy : MonoBehaviour
{
    public GameObject ballPrefab;
    public Transform throwPoint;
    public float throwCooldown = 3f;
    public float throwForceX = 8f;
    public float throwForceY = 6f;
    private Transform player;
    private float moveSpeed = 2f;
    private Rigidbody2D rb;
    public int maxHealth = 3;
    private int currentHealth;
    private float chaseRange = 9f;
    private float attackRange = 8f;
    private bool isAttacking;
    private float lastThrowTime;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lastThrowTime = -throwCooldown;
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

    private void Update()
    {
        if (player == null || isAttacking) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= chaseRange && distance > attackRange)
        {
            MoveTowardsPlayer();
        }
        else if (distance <= attackRange)
        {
            //check cooldown
            if (Time.time >= lastThrowTime + throwCooldown)
            {
                ThrowBall();
                lastThrowTime = Time.time;  //reset cooldown
            }
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);

        //flip sprite
        if (direction.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);
    }

    private void Die()
    {
        Debug.Log("killed");
        Destroy(gameObject);
    }

    void ThrowBall()
    {
        GameObject ball = Instantiate(ballPrefab, throwPoint.position, Quaternion.identity);
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();

        float horizontalDirection;
        if (player.position.x > transform.position.x)
        {
            horizontalDirection = 1f; //player is to the right
        }
        else
        {
            horizontalDirection = -1f; //player is to the left
        }
        transform.localScale = new Vector3(horizontalDirection, 1, 1); //flip enemy to player

        float verticalForce = throwForceY;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= 6f)
        {
            verticalForce = 3f; //less arc
        }

        Vector2 force = new Vector2(throwForceX * horizontalDirection, verticalForce);

        rb.AddForce(force, ForceMode2D.Impulse);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("SwordHitBox"))
        {
            TakeDamage(1);
        }

    }
}