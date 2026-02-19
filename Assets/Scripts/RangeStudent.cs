using System.Collections;
using UnityEngine;

public class ThrowEnemy : MonoBehaviour
{
    public GameObject ballPrefab;
    public Transform throwPoint;
    public float throwCooldown = 1.6f;
    public float throwForceX = 8f;
    public float throwForceY = 6f;
    private Transform player;
    private float moveSpeed = 2f;
    private Rigidbody2D rb;
    public int maxHealth = 3;
    private int currentHealth;
    private float chaseRange = 15f;
    private float attackRange = 8f;
    private bool isAttacking;
    private float lastThrowTime;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lastThrowTime = -throwCooldown;
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Prevent player from pushing the enemy - set to Kinematic
        rb.bodyType = RigidbodyType2D.Kinematic;
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
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Always face the player direction when in range
        if (distance <= chaseRange)
        {
            animator.SetBool("canSeePlayer", true);
            
            // Update facing direction - only flip when not attacking
            if (!isAttacking)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                if (direction.x > 0)
                    spriteRenderer.flipX = true;
                else
                    spriteRenderer.flipX = false;
            }
            
            if (!isAttacking && distance > attackRange)
            {
                MoveTowardsPlayer();
            }
            else if (!isAttacking && distance <= attackRange)
            {
                //check cooldown
                if (Time.time >= lastThrowTime + throwCooldown)
                {
                    StartCoroutine(ThrowBall());
                    lastThrowTime = Time.time;  //reset cooldown
                }
            }
        }
        else
        {
            animator.SetBool("canSeePlayer", false);
            animator.SetBool("isMoving", false);
            // Stop movement when out of range (idle)
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            // Maintain flip when out of range
            spriteRenderer.flipX = false;
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);

        //trigger walking animation
        animator.SetBool("isMoving", true);
    }

    private void Die()
    {
        Debug.Log("killed");
        Destroy(gameObject); 
    }

    IEnumerator ThrowBall()
    {
        isAttacking = true;
        animator.SetBool("isMoving", false);
        animator.SetBool("isThrowing", true);
        rb.linearVelocity = Vector2.zero;
        
        GameObject ball = Instantiate(ballPrefab, throwPoint.position, Quaternion.identity);
        Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();

        float horizontalDirection;
        if (player.position.x > transform.position.x)
        {
            horizontalDirection = 1f; //player is to the right
        }
        else
        {
            horizontalDirection = -1f; //player is to the left
        }

        float verticalForce = throwForceY;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= 6f)
        {
            verticalForce = 3f; //less arc
        }

        Vector2 force = new Vector2(throwForceX * horizontalDirection, verticalForce);
        ballRb.AddForce(force, ForceMode2D.Impulse);
        
        yield return new WaitForSeconds(0.5f); // Animation duration
        isAttacking = false;
        animator.SetBool("isThrowing", false);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("SwordHitBox"))
        {
            TakeDamage(1);
        }

    }
}