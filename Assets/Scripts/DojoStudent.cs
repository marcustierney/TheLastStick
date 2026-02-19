using UnityEngine;
using System.Collections;
public class Enemy : MonoBehaviour
{
    public int maxHealth = 3;   
    private int currentHealth;

    [SerializeField]
    private float damageToPlayer = 10f; //damage dealt to player on contact
    private float moveSpeed = 3f;
    private float chaseRange = 8f;
    private float attackRange = 1.2f;
    private Transform player;
    private Rigidbody2D rb;
    public GameObject swordHitbox;
    public GameObject warningHitBox;
    private float attackDuration = 0.1f;
    private float attackCooldown = .5f;
    private Animator animator;
    private bool isAttacking = false;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Prevent player from pushing the enemy - set to Kinematic
        rb.bodyType = RigidbodyType2D.Kinematic;
        //transform.localScale = new Vector3(1, 1, 1);
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
                StartCoroutine(Attack());
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

    private IEnumerator Attack()
    {
        isAttacking = true;
        animator.SetBool("isMoving", false);
        animator.SetBool("isAttacking", true);
        rb.linearVelocity = Vector2.zero;
        //position sword in front of enemy
        Vector3 offset;
        if (spriteRenderer.flipX)
        {
            offset = new Vector3(0.5f, 0f, 0f); //facing left
        }
        else
        {
            offset = new Vector3(-0.5f, 0f, 0f); //facing right
        }
        
        // Show warning box first
        warningHitBox.transform.localPosition = offset;
        warningHitBox.SetActive(true);
        yield return new WaitForSeconds(0.7f); // 0.7 second warning
        warningHitBox.SetActive(false);
        
        // Then show actual hitbox
        swordHitbox.transform.localPosition = offset;
        swordHitbox.SetActive(true);
        yield return new WaitForSeconds(attackDuration);
        swordHitbox.SetActive(false);
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
        animator.SetBool("isAttacking", false);
    }

    private void Die()
    {
        Debug.Log("killed");
        animator.SetTrigger("Death");
        Destroy(gameObject, 0.5f);
    }

///THIS TABBED OUT CODE IS FOR IF WE WANT TO PLAYER TOUCHING THE ENEMEY TO DO DAMAGE OR ONLY THE ENEMY WEAPON
/* 
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Deal damage to player on contact - but NOT if it's the sword hitbox
        if (collision.gameObject.CompareTag("Player") && collision.gameObject.name != "SwordHitbox")
        {
            DealDamageToPlayer(collision.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Deal damage to player if touching trigger - but NOT if it's the sword hitbox
        if (collision.CompareTag("Player") && collision.gameObject.name != "SwordHitbox")
        {
            DealDamageToPlayer(collision.gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Continue dealing damage while touching, respecting I-frames - but NOT if it's the sword hitbox
        if (collision.CompareTag("Player") && collision.gameObject.name != "SwordHitbox")
        {
            DealDamageToPlayer(collision.gameObject);
        }
    }

    private void DealDamageToPlayer(GameObject player)
    {
        SwordAttack swordAttack = player.GetComponent<SwordAttack>();
        if (swordAttack != null && swordAttack.IsAttacking)
        {
            return;
        }

        UpdateHealth playerHealth = player.GetComponent<UpdateHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageToPlayer);
        }
    }*/
}