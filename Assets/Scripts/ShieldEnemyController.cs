using System.Collections;
using UnityEngine;

public class ShieldEnemyController : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int IsRunningAttackHash = Animator.StringToHash("isRunningAttack");

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.75f;
    [SerializeField] private float chaseRange = 14f;
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float rushSpeed = 11f;
    [SerializeField] private float rushDuration = 0.7f;
    [SerializeField] private float stunDuration = 1.1f;
    [SerializeField] private float attackCooldown = 3.5f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 8;
    [SerializeField] private int coinsDropped = 2;

    [Header("Damage")]
    [SerializeField] private int rushDamage = 20;
    [SerializeField, Range(0.1f, 1f)] private float frontDamageMultiplier = 0.5f;
    [SerializeField] private float backDamageMultiplier = 1f;

    [Header("Animation")]
    [SerializeField] private string movingBoolName = "isMoving";
    [SerializeField] private string runningAttackBoolName = "isRunningAttack";

    [SerializeField] private AudioSource deathAudioSource;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private int currentHealth;
    private bool isDead;
    private bool isRushing;
    private bool isStunned;
    private bool facingRight;
    private bool hasHitPlayerThisRush;
    private float lastRushTime = -999f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (isDead || player == null)
        {
            return;
        }

        if (isRushing || isStunned)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > chaseRange)
        {
            StopMoving();
            return;
        }

        FacePlayer();

        if (distance <= attackRange && Time.time >= lastRushTime + attackCooldown)
        {
            StartCoroutine(RushRoutine());
            return;
        }

        MoveTowardsPlayer();
    }

    public void TakeDamage(int damage, Vector2 hitSourcePosition)
    {
        if (isDead)
        {
            return;
        }

        bool hitFromFront = IsHitFromFront(hitSourcePosition);
        float multiplier = hitFromFront ? frontDamageMultiplier : backDamageMultiplier;
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));

        currentHealth -= finalDamage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position);
    }

    private IEnumerator RushRoutine()
    {
        lastRushTime = Time.time;
        isRushing = true;
        hasHitPlayerThisRush = false;

        // facingRight mirrors sprite flip convention in FacePlayer.
        Vector2 rushDirection = facingRight ? Vector2.left : Vector2.right;
        rb.linearVelocity = Vector2.zero;
        SetAnimatorState(false, true);

        float rushTimer = 0f;
        while (rushTimer < rushDuration)
        {
            rb.linearVelocity = new Vector2(rushDirection.x * rushSpeed, rb.linearVelocity.y);
            rushTimer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isRushing = false;
        isStunned = true;
        SetAnimatorState(false, false);

        yield return new WaitForSeconds(stunDuration);

        isStunned = false;
    }

    private void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        SetAnimatorState(true, false);
    }

    private void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        SetAnimatorState(false, false);
    }

    private void FacePlayer()
    {
        facingRight = player.position.x < transform.position.x;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = facingRight;
        }
    }

    private bool IsHitFromFront(Vector2 hitSourcePosition)
    {
        Vector2 toHit = ((Vector2)hitSourcePosition - (Vector2)transform.position).normalized;
        Vector2 forward = facingRight ? Vector2.left : Vector2.right;
        return Vector2.Dot(toHit, forward) > 0f;
    }

    private void SetAnimatorState(bool moving, bool runningAttack)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsMovingHash, moving);
        animator.SetBool(IsRunningAttackHash, runningAttack);

        if (!string.IsNullOrEmpty(movingBoolName) && movingBoolName != "isMoving")
        {
            animator.SetBool(movingBoolName, moving);
        }

        if (!string.IsNullOrEmpty(runningAttackBoolName) && runningAttackBoolName != "isRunningAttack")
        {
            animator.SetBool(runningAttackBoolName, runningAttack);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void TryDamagePlayer(GameObject target)
    {
        if (!isRushing || isDead || hasHitPlayerThisRush || !target.CompareTag("Player"))
        {
            return;
        }

        UpdateHealth health = target.GetComponent<UpdateHealth>();
        if (health == null)
        {
            return;
        }

        health.TakeDamage(rushDamage, transform.position);
        hasHitPlayerThisRush = true;
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        rb.linearVelocity = Vector2.zero;
        CoinManager.Instance?.AddCoins(coinsDropped);

        if (deathAudioSource != null && deathAudioSource.clip != null)
        {
            AudioSource.PlayClipAtPoint(deathAudioSource.clip, transform.position);
        }

        Destroy(gameObject);
    }
}