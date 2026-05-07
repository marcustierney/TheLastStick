using System.Collections;
using UnityEngine;

public class BossTwoController : MonoBehaviour, IHittable
{
    public BossSummoningSwords summoningSwords;
    public BossFloatingSwords floatingSwords;
    public BossHealth bossHealth;
    public float moveSpeed = 3f;
    public float dashAttackRange = 8f;      
    public float dashChargeUpDuration = 1.2f;  
    public float dashSpeed = 25f;              
    public float dashDuration = 0.6f;         
    public float dashStunDuration = 1.0f;      
    public float dashCooldown = 4f;            
    public int dashDamage = 20;
    public float summonAttackCooldown = 12f;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Transform player;
    private bool isDead = false;
    private bool isDashing = false;
    private bool isChargingDash = false;
    private bool isDazed = false;
    private bool isDealingDashDamage = false;
    private bool isSummonCharging = false;
    private float lastDashTime = -999f;
    // private bool lockedChargeDirection = false;
    private bool facingRight = false;
    [SerializeField] private SlashFeedback slashFeedback;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Start()
    {
        if (summoningSwords != null)
        {
            summoningSwords.onChargingChanged += OnSummonChargingChanged;
        }

        StartCoroutine(SummonAttackLoop());
    }
    private void OnSummonChargingChanged(bool charging)
    {
        isSummonCharging = charging;
        if (animator != null)
        {
            animator.SetBool("isCharging", charging);
        }
    }
    private void Update()
    {
        if (isDead || player == null) return;
        if (isDashing || isChargingDash || isDazed || isSummonCharging) return; 
        float distance = Vector2.Distance(transform.position, player.position);
        spriteRenderer.flipX = player.position.x > transform.position.x;
        if (distance <= dashAttackRange && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(DashAttack());
        }
        else if (distance > dashAttackRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetBool("isMoving", false);
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        animator.SetBool("isMoving", true);
    }

    private IEnumerator DashAttack()
    {
        lastDashTime = Time.time;
        isChargingDash = true;
        // lockedChargeDirection = true;
        facingRight = player.position.x > transform.position.x;
        spriteRenderer.flipX = facingRight;
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isMoving", false);
        animator.SetBool("isChargingDash", true);
        yield return new WaitForSeconds(dashChargeUpDuration);
        animator.SetBool("isChargingDash", false);
        isChargingDash = false;
        isDashing = true;
        isDealingDashDamage = true;
        animator.SetBool("isDashing", true);
        Vector2 dashDirection = facingRight ? Vector2.right : Vector2.left;
        float dashTimer = 0f;
        while (dashTimer < dashDuration)
        {
            rb.linearVelocity = new Vector2(dashDirection.x * dashSpeed, rb.linearVelocity.y);
            dashTimer += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = Vector2.zero;
        isDashing = false;
        isDealingDashDamage = false;
        animator.SetBool("isDashing", false);
        isDazed = true;
        animator.SetBool("isDazed", true);
        yield return new WaitForSeconds(dashStunDuration);
        isDazed = false;
        animator.SetBool("isDazed", false);
        // lockedChargeDirection = false;
    }

    private IEnumerator SummonAttackLoop()
    {
        yield return new WaitForSeconds(6f);
        while (!isDead)
        {
            if (summoningSwords != null)
            {
                summoningSwords.TriggerSummonAttack();
            }
            yield return new WaitForSeconds(summonAttackCooldown);
        }
    }

    public void ReceiveHit(PlayerMeleeHit hit)
    {
        if (slashFeedback != null)
        {
            slashFeedback.PlaySlash(hit.ComboIndex);
        }

        TakeDamage(hit.Damage);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        bossHealth.TakeDamage(damage);
        if (bossHealth.Health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Boss dead");
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDealingDashDamage) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            UpdateHealth health = collision.gameObject.GetComponent<UpdateHealth>();
            if (health != null)
            {
                health.TakeDamage(dashDamage, transform.position);
            }
        }
    }
}