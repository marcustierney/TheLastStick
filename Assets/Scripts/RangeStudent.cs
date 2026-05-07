using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowEnemy : MonoBehaviour, IHittable
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
    [Header("Hit Flash")]
    [SerializeField] private float hitFlashDuration = 0.6f;
    [SerializeField] private float fatalHitFlashDuration = 0.08f;
    [SerializeField] private Material whiteFlashMaterial;
    private SpriteRenderer[] flashRenderers;
    private Material[] originalMaterials;
    private Coroutine hitFlashRoutine;
    private bool isDying;
    [Header("Death Sound")]
    [SerializeField] private AudioSource deathAudioSource;
    [SerializeField] private AudioClip[] deathClips = new AudioClip[5];
    [SerializeField] private Collider2D[] damageableHurtboxes;
    [SerializeField] private SlashFeedback slashFeedback;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lastThrowTime = -throwCooldown;
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        CacheDamageableHurtboxes();
        
        // Prevent player from pushing the enemy - set to Kinematic
        //rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void CacheDamageableHurtboxes()
    {
        if (damageableHurtboxes != null && damageableHurtboxes.Length > 0)
        {
            return;
        }

        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>(true);
        List<Collider2D> filteredColliders = new List<Collider2D>(allColliders.Length);

        foreach (Collider2D col in allColliders)
        {
            if (col == null)
            {
                continue;
            }

            if (warningHitBox != null && col.transform.IsChildOf(warningHitBox.transform))
            {
                continue;
            }

            filteredColliders.Add(col);
        }

        damageableHurtboxes = filteredColliders.ToArray();
    }

    private bool IsSwordTouchingDamageableHurtbox(Collider2D swordCollider)
    {
        if (swordCollider == null)
        {
            return false;
        }

        if (damageableHurtboxes == null || damageableHurtboxes.Length == 0)
        {
            return true;
        }

        foreach (Collider2D hurtbox in damageableHurtboxes)
        {
            if (hurtbox == null || !hurtbox.enabled || !hurtbox.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (Physics2D.IsTouching(swordCollider, hurtbox))
            {
                return true;
            }
        }

        return false;
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
            Debug.LogWarning("whiteFlashMaterial is not assigned.", this);
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
                    spriteRenderer.flipX = false;
                else
                    spriteRenderer.flipX = true;
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
            spriteRenderer.flipX = true;
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
        if (isDying)
        {
            return;
        }

        isDying = true;
        Debug.Log("killed");
        CoinManager.Instance?.AddCoins(2);
        PlayDeathSound();

        DisableCombatState();

        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        StartCoroutine(DieAfterDelay(animator != null ? 0.5f : 0f));
    }

    private IEnumerator DieAfterDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

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

    IEnumerator ThrowBall()
    {
        isAttacking = true;
        animator.SetBool("isMoving", false);
        animator.SetBool("isThrowing", true);
        rb.linearVelocity = Vector2.zero;
        Vector2 targetPosition = player.position;
        if (warningHitBox != null)
        {
            Vector3 offset = spriteRenderer.flipX ? new Vector3(-2f, 0f, 0f) : new Vector3(2f, 0f, 0f);
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
        if (isDying)
        {
            return;
        }

        if (collision.CompareTag("SwordHitBox") && IsSwordTouchingDamageableHurtbox(collision))
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