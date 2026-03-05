using System.Collections;
using UnityEngine;

public class ThrowEnemy : MonoBehaviour
{
    public GameObject ballPrefab;
    public GameObject warningHitBox;
    public Transform throwPoint;
    public float throwCooldown = 1.6f;
    public float throwWarningDuration = 0.5f;
    public float projectileSpeed = 10f;
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
    private float damageToPlayer = 20f;
    public float ledgeCheckDistance = 1f; 
    public float ledgeCheckDepth = 1f;   
    public LayerMask groundLayer;           

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
        //rb.bodyType = RigidbodyType2D.Kinematic;
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

    private bool IsGroundAhead(float moveDirectionX)
    {
        Vector2 rayOrigin = new Vector2(transform.position.x + moveDirectionX * ledgeCheckDistance, transform.position.y);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, ledgeCheckDepth, groundLayer);
        return hit.collider != null;
    }

    private void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;

        if (IsGroundAhead(direction.x))  
        {
            rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            animator.SetBool("isMoving", true);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetBool("isMoving", false);
        }
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
        Vector2 targetPosition = player.position;
        if (warningHitBox != null)
        {
            Vector3 offset = spriteRenderer.flipX ? new Vector3(2f, 0f, 0f) : new Vector3(-2f, 0f, 0f);
            warningHitBox.transform.localPosition = offset;
            warningHitBox.SetActive(true);
            yield return new WaitForSeconds(throwWarningDuration);
            warningHitBox.SetActive(false);
        }
        GameObject ball = Instantiate(ballPrefab, throwPoint.position, Quaternion.identity);
        BallProjectile projectile = ball.GetComponent<BallProjectile>();
        if (projectile != null)
        {
            Vector2 aimDirection = (targetPosition - (Vector2)throwPoint.position).normalized;
            projectile.Launch(aimDirection, projectileSpeed);
        }
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
        animator.SetBool("isThrowing", false);
    }


    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("SwordHitBox"))
        {
            TakeDamage(1);
        }

        if (collision.CompareTag("Player") && collision.gameObject.name != "SwordHitbox")
        {
            // DealDamageToPlayer(collision.gameObject);
        }

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && collision.gameObject.name != "SwordHitbox")
        {
            // DealDamageToPlayer(collision.gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.gameObject.name != "SwordHitbox")
        {
            // DealDamageToPlayer(collision.gameObject);
        }
    }

    private void DealDamageToPlayer(GameObject playerObject)
    {
        SwordAttack swordAttack = playerObject.GetComponent<SwordAttack>();
        if (swordAttack != null && swordAttack.IsAttacking)
        {
            return;
        }

        UpdateHealth playerHealth = playerObject.GetComponent<UpdateHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage((int)damageToPlayer);
        }

    }
}