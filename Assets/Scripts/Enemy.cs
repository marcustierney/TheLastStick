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
    private bool isAttacking;
    public GameObject swordHitbox;
    private float attackDuration = 0.3f;
    private float attackCooldown = 1f;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
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
            StartCoroutine(Attack());
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

    private IEnumerator Attack()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        //position sword in front of enemy
        Vector3 offset;
        if (transform.localScale.x > 0)
        {
            offset = new Vector3(0.8f, 0f, 0f); //facing right
        }
        else
        {
            offset = new Vector3(0.8f, 0f, 0f); //facing left
        }
        swordHitbox.transform.localPosition = offset;
        swordHitbox.SetActive(true);
        yield return new WaitForSeconds(attackDuration);
        swordHitbox.SetActive(false);
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    private void Die()
    {
        Debug.Log("killed");
        Destroy(gameObject); 
    }

///THIS TABBED OUT CODE IS FOR IF WE WANT TO PLAYER TOUCHING THE ENEMEY TO DO DAMAGE OR ONLY THE ENEMY WEAPON
/* 
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
    }*/
}