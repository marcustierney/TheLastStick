using UnityEngine;
using System.Collections;
public class WolfSpiderEnemy : MonoBehaviour, IHittable
{
    public int maxHealth = 9;
    [SerializeField] private int swarmMaxHealth = 5;
    private int currentHealth;

    // [SerializeField]
    // private float damageToPlayer = 20f;
    protected float moveSpeed = 2.5f;
    protected float chaseRange = 10f;
    protected float attackRange = 1.6f;  // Increased so sword hits before body does
    protected Transform player;
    protected Rigidbody2D rb;
    private Collider2D bodyCollider;
    public GameObject swordHitbox;
    public GameObject warningHitBox;
    [Header("Attack")]
    [SerializeField] private float swordOffsetX = 0.5f;  // Adjust if needed for wider sprite
    [SerializeField] private float warningDuration = 0.7f;
    private float attackDuration = 0.14f;
    private float attackCooldown = .5f;
    protected Animator animator;
    private bool isAttacking = false;
    private bool isSwarmPhase;
    private bool isChargingSwarmDash;
    private bool isSwarmDashing;
    private bool isSwarmRecovering;
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

    [Header("Swarm Charge Attack")]
    [SerializeField] private float swarmMoveSpeed = 1.8f;
    [SerializeField] private float swarmDashAttackRange = 5f;
    [SerializeField] private float swarmDashChargeDuration = 0.85f;
    [SerializeField] private float swarmDashSpeed = 6.5f;
    [SerializeField] private float swarmDashDuration = 0.35f;
    [SerializeField] private float swarmDashRecoveryDuration = 0.6f;
    [SerializeField] private float swarmDashCooldown = 2.2f;
    [SerializeField] private int swarmDashDamage = 10;

    [Header("Swarm Animator Params")]
    [SerializeField] private string swarmPhaseBoolParam = "isSwarm";
    [SerializeField] private string swarmAttackBoolParam = "isDashing";
    [SerializeField] private string swarmTransitionTriggerParam = "toSwarm";

    private SlashFeedback slashFeedback;
    private float lastSwarmDashTime = -999f;
    private Coroutine meleeAttackRoutine;
    private Coroutine swarmDashRoutine;
    private bool isDealingSwarmDashDamage;

    private void Awake()
    {
        slashFeedback = GetComponent<SlashFeedback>();
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
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
            if (!isSwarmPhase)
            {
                EnterSwarmPhase();
            }
            else
            {
                Die();
            }
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
            Debug.LogWarning("WolfSpiderEnemy whiteFlashMaterial is not assigned.", this);
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
            if (animator != null)
            {
                animator.SetBool("canSeePlayer", true);
            }
            
            // Update facing direction - only flip when not attacking
            if (!IsBusyAttacking())
            {
                Vector2 direction = (player.position - transform.position).normalized;
                if (direction.x > 0)
                    spriteRenderer.flipX = true;
                else
                    spriteRenderer.flipX = false;
            }

            if (isSwarmPhase)
            {
                UpdateSwarmPhase(distance);
            }
            else if (!isAttacking && distance > attackRange)
            {
                MoveTowardsPlayer();
            }
            else if (!isAttacking && distance <= attackRange)
            {
                if (meleeAttackRoutine == null)
                {
                    meleeAttackRoutine = StartCoroutine(Attack());
                }
            }
        }
        else
        {
            if (animator != null)
            {
                animator.SetBool("canSeePlayer", false);
                animator.SetBool("isMoving", false);
            }
            // Stop movement when out of range (idle)
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            // Maintain flip when out of range
            spriteRenderer.flipX = false;
        }
    }

    private void UpdateSwarmPhase(float distance)
    {
        if (isChargingSwarmDash || isSwarmDashing || isSwarmRecovering)
        {
            return;
        }

        bool canDash = distance <= swarmDashAttackRange && Time.time >= lastSwarmDashTime + swarmDashCooldown;
        if (canDash)
        {
            if (swarmDashRoutine == null)
            {
                swarmDashRoutine = StartCoroutine(SwarmDashAttack());
            }
            return;
        }

        MoveTowardsPlayer(swarmMoveSpeed);
    }

    private bool IsGroundAhead(float moveDirectionX)
    {
        if (bodyCollider == null)
        {
            Vector2 fallbackOrigin = new Vector2(transform.position.x + moveDirectionX * ledgeCheckDistance, transform.position.y);
            RaycastHit2D fallbackHit = Physics2D.Raycast(fallbackOrigin, Vector2.down, ledgeCheckDepth, groundLayer);
            return fallbackHit.collider != null;
        }

        Bounds bounds = bodyCollider.bounds;
        float directionSign = Mathf.Sign(moveDirectionX);
        if (directionSign == 0f)
        {
            directionSign = spriteRenderer != null && spriteRenderer.flipX ? 1f : -1f;
        }

        float frontOffset = bounds.extents.x + ledgeCheckDistance;
        Vector2 rayOrigin = new Vector2(bounds.center.x + directionSign * frontOffset, bounds.min.y + 0.05f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, ledgeCheckDepth + 0.25f, groundLayer);
        return hit.collider != null;
    }

    protected virtual void MoveTowardsPlayer()
    {
        MoveTowardsPlayer(moveSpeed);
    }

    private void MoveTowardsPlayer(float speed)
    {
        Vector2 direction = (player.position - transform.position).normalized;

        if (IsGroundAhead(direction.x))
        {
            rb.linearVelocity = new Vector2(direction.x * speed, rb.linearVelocity.y);
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
        if (isSwarmPhase)
        {
            meleeAttackRoutine = null;
            yield break;
        }

        isAttacking = true;
        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            animator.SetBool("isAttacking", true);
        }
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
        if (warningHitBox != null)
        {
            warningHitBox.transform.localPosition = offset;
            warningHitBox.SetActive(true);
        }
        yield return new WaitForSeconds(warningDuration);
        if (warningHitBox != null)
        {
            warningHitBox.SetActive(false);
        }
        
        // Then show actual hitbox
        if (swordHitbox != null)
        {
            swordHitbox.transform.localPosition = offset;
            swordHitbox.SetActive(true);
        }
        yield return new WaitForSeconds(attackDuration);
        if (swordHitbox != null)
        {
            swordHitbox.SetActive(false);
        }
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
        }
        meleeAttackRoutine = null;
    }

    private IEnumerator SwarmDashAttack()
    {
        lastSwarmDashTime = Time.time;
        isChargingSwarmDash = true;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("isMoving", false);
        }

        SetAnimatorBoolIfExists(swarmAttackBoolParam, true);
        yield return new WaitForSeconds(swarmDashChargeDuration);

        isChargingSwarmDash = false;
        isSwarmDashing = true;
        isDealingSwarmDashDamage = true;

        float dashDirectionX = spriteRenderer != null && spriteRenderer.flipX ? 1f : -1f;
        float dashTimer = 0f;
        while (dashTimer < swarmDashDuration)
        {
            if (!IsGroundAhead(dashDirectionX))
            {
                break;
            }

            rb.linearVelocity = new Vector2(dashDirectionX * swarmDashSpeed, rb.linearVelocity.y);
            dashTimer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        isSwarmDashing = false;
        isDealingSwarmDashDamage = false;
        SetAnimatorBoolIfExists(swarmAttackBoolParam, false);

        isSwarmRecovering = true;
        yield return new WaitForSeconds(swarmDashRecoveryDuration);
        isSwarmRecovering = false;
        swarmDashRoutine = null;
    }

    private bool IsBusyAttacking()
    {
        return isAttacking || isChargingSwarmDash || isSwarmDashing || isSwarmRecovering;
    }

    private void EnterSwarmPhase()
    {
        isSwarmPhase = true;
        currentHealth = Mathf.Max(1, swarmMaxHealth);
        isAttacking = false;

        if (meleeAttackRoutine != null)
        {
            StopCoroutine(meleeAttackRoutine);
            meleeAttackRoutine = null;
        }

        if (warningHitBox != null)
        {
            warningHitBox.SetActive(false);
        }

        if (swordHitbox != null)
        {
            swordHitbox.SetActive(false);
        }

        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
        }

        SetAnimatorBoolIfExists(swarmPhaseBoolParam, true);
        SetAnimatorTriggerIfExists(swarmTransitionTriggerParam);
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
            SetAnimatorBoolIfExists(swarmPhaseBoolParam, false);
            SetAnimatorBoolIfExists(swarmAttackBoolParam, false);
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
        isChargingSwarmDash = false;
        isSwarmDashing = false;
        isSwarmRecovering = false;
        isDealingSwarmDashDamage = false;

        if (meleeAttackRoutine != null)
        {
            StopCoroutine(meleeAttackRoutine);
            meleeAttackRoutine = null;
        }

        if (swarmDashRoutine != null)
        {
            StopCoroutine(swarmDashRoutine);
            swarmDashRoutine = null;
        }

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

    private void SetAnimatorBoolIfExists(string parameterName, bool value)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        if (AnimatorHasParameter(parameterName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(parameterName, value);
        }
    }

    private void SetAnimatorTriggerIfExists(string parameterName)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        if (AnimatorHasParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(parameterName);
        }
    }

    private bool AnimatorHasParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.type == parameterType && parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
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

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!isDealingSwarmDashDamage) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            UpdateHealth health = collision.gameObject.GetComponent<UpdateHealth>();
            if (health != null)
            {
                health.TakeDamage(swarmDashDamage, transform.position, AnalyticsKeys.DeathCauseSpiderEnemy);
            }
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
