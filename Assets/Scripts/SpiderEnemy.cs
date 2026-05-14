using UnityEngine;
using System.Collections;
public class SpiderEnemy : MonoBehaviour, IHittable
{
    public int maxHealth = 5;   
    private int currentHealth;

    // [SerializeField]
    // private float damageToPlayer = 20f;
    protected float moveSpeed = 10f;
    protected float chaseRange = 10f;
    protected float attackRange = 1.6f;  // Increased so sword hits before body does
    protected Transform player;
    protected Rigidbody2D rb;
    public GameObject swordHitbox;
    public GameObject warningHitBox;
    [Header("Attack")]
    [SerializeField] private float swordOffsetX = 0.5f;  // Adjust if needed for wider sprite
    private float attackDuration = 0.1f;
    private float attackCooldown = .5f;
    protected Animator animator;
    private bool isAttacking = false;
    protected SpriteRenderer spriteRenderer;
    [Header("Hit Flash")]
    [SerializeField] private float hitFlashDuration = 0.6f;
    [SerializeField] private float fatalHitFlashDuration = 0.08f;
    [SerializeField] private Material whiteFlashMaterial;
    private SpriteRenderer[] flashRenderers;
    private Material[] originalMaterials;
    private Coroutine hitFlashRoutine;
    private bool isDying;
    [Header("Ledge Detection")]
    [SerializeField] public float ledgeCheckDistance = 0.8f;  // Wider body detection
    [SerializeField] public float ledgeCheckDepth = 0.6f;    // Reduced for shorter body
    public LayerMask groundLayer;
    [Header("Death Sound")]
    [SerializeField] private AudioSource deathAudioSource;
    [SerializeField] private AudioClip[] deathClips = new AudioClip[5];

    private SlashFeedback slashFeedback;

    private void Awake()
    {
        slashFeedback = GetComponent<SlashFeedback>();
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        animator = GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            SpriteRenderer srOnAnimator = animator.GetComponent<SpriteRenderer>();
            spriteRenderer = srOnAnimator != null ? srOnAnimator : GetComponent<SpriteRenderer>();
        }
        else
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        flashRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if ((flashRenderers == null || flashRenderers.Length == 0) && spriteRenderer != null)
        {
            flashRenderers = new SpriteRenderer[] { spriteRenderer };
        }

        if (flashRenderers != null && flashRenderers.Length > 0)
        {
            originalMaterials = new Material[flashRenderers.Length];
            for (int i = 0; i < flashRenderers.Length; i++)
            {
                originalMaterials[i] = flashRenderers[i] != null ? flashRenderers[i].sharedMaterial : null;
            }
        }

        // Prevent player from pushing the enemy - set to Kinematic
        //rb.bodyType = RigidbodyType2D.Kinematic;
        //transform.localScale = new Vector3(1, 1, 1);
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
        if (isDying)
        {
            return;
        }

        currentHealth -= damage;
        Debug.Log("damage " + damage + " cCurrent hp " + currentHealth);

        bool isFatalHit = currentHealth <= 0;
        TriggerHitFlash(isFatalHit ? fatalHitFlashDuration : hitFlashDuration);

        if (isFatalHit)
        {
            Die();
        }
    }

    private void TriggerHitFlash(float duration)
    {
        if (flashRenderers == null || flashRenderers.Length == 0)
        {
            return;
        }

        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
        }

        hitFlashRoutine = StartCoroutine(HitFlashRoutine(duration));
    }

    private IEnumerator HitFlashRoutine(float duration)
    {
        ApplyFlashMaterial();
        yield return new WaitForSeconds(Mathf.Max(0f, duration));
        RestoreOriginalMaterials();
        hitFlashRoutine = null;
    }

    private void ApplyFlashMaterial()
    {
        if (whiteFlashMaterial == null)
        {
            Debug.LogWarning("SpiderEnemy whiteFlashMaterial is not assigned.", this);
            return;
        }

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null)
            {
                flashRenderers[i].material = whiteFlashMaterial;
            }
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (flashRenderers == null || originalMaterials == null)
        {
            return;
        }

        int count = Mathf.Min(flashRenderers.Length, originalMaterials.Length);
        for (int i = 0; i < count; i++)
        {
            if (flashRenderers[i] != null)
            {
                flashRenderers[i].material = originalMaterials[i];
            }
        }
    }

    private void Update()
    {
        if (isDying)
        {
            return;
        }

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

    private bool IsGroundAhead(float moveDirectionX)
    {
        Vector2 rayOrigin = new Vector2(transform.position.x + moveDirectionX * ledgeCheckDistance, transform.position.y);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, ledgeCheckDepth, groundLayer);
        return hit.collider != null;
    }

    protected virtual void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;

        if (IsGroundAhead(direction.x))
        {
            rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            if (animator != null)
            {
                animator.SetBool("isMoving", true);
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (animator != null)
            {
                animator.SetBool("isMoving", false);
            }
        }
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
            offset = new Vector3(swordOffsetX, 0f, 0f); //facing right
        }
        else
        {
            offset = new Vector3(-swordOffsetX, 0f, 0f); //facing left
        }
        
        // Show warning box first
        warningHitBox.transform.localPosition = offset;
        warningHitBox.SetActive(true);
        yield return new WaitForSeconds(0.4f); // 0.7 second warning
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
        if (isDying)
        {
            return;
        }

        isDying = true;
        Debug.Log("killed");
        if (animator != null)
        {
            animator.SetTrigger("isDying");
        }
        DisableCombatState();

        CoinManager.Instance?.AddCoins(2);
        PlayDeathSound();
        StartCoroutine(DieAfterDelay()); 
    }

    private IEnumerator DieAfterDelay()
    {
        yield return new WaitForSeconds(0.9f);
        RestoreOriginalMaterials();
        Destroy(gameObject);
    }

    private void DisableCombatState()
    {
        isAttacking = false;

        if (warningHitBox != null)
        {
            warningHitBox.SetActive(false);
        }

        if (swordHitbox != null)
        {
            swordHitbox.SetActive(false);
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
    }

    private void PlayDeathSound()
    {
        AudioClip clipToPlay = GetRandomDeathClip();
        if (clipToPlay == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clipToPlay, transform.position);
    }

    private AudioClip GetRandomDeathClip()
    {
        if (deathClips != null && deathClips.Length > 0)
        {
            int clipIndex = Random.Range(0, deathClips.Length);
            AudioClip clip = deathClips[clipIndex];
            if (clip != null)
            {
                return clip;
            }
        }

        if (deathAudioSource != null && deathAudioSource.clip != null)
        {
            return deathAudioSource.clip;
        }

        return null;
    }

///THIS TABBED OUT CODE IS FOR IF WE WANT TO PLAYER TOUCHING THE ENEMEY TO DO DAMAGE OR ONLY THE ENEMY WEAPON

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Deal damage to player on contact - but NOT if it's the sword hitbox
        if (collision.gameObject.CompareTag("Player") && collision.gameObject.name != "SwordHitbox")
        {
            // DealDamageToPlayer(collision.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Deal damage to player if touching trigger - but NOT if it's the sword hitbox
        if (collision.CompareTag("Player") && collision.gameObject.name != "SwordHitbox")
        {
            // DealDamageToPlayer(collision.gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Continue dealing damage while touching, respecting I-frames - but NOT if it's the sword hitbox
        if (collision.CompareTag("Player") && collision.gameObject.name != "SwordHitbox")
        {
            // DealDamageToPlayer(collision.gameObject);
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
            playerHealth.TakeDamage(20, transform.position, AnalyticsKeys.DeathCauseSpiderEnemy);
        }
    }
}