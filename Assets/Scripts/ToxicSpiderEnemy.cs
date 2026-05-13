using System.Collections;
using UnityEngine;

public class ToxicSpiderEnemy : MonoBehaviour, IHittable
{
    [Header("Vitals")]
    [SerializeField] private int maxHealth = 4;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float chaseRange = 13f;
    [SerializeField] private float stopRange = 10f;

    [Header("Spit Warning")]
    [SerializeField] private GameObject warningHitBox;

    [Header("Spit")]
    [SerializeField] private GameObject toxicSpitPrefab;
    [SerializeField] private Transform spitPoint;
    [SerializeField] private float spitCooldown = 1.9f;
    [SerializeField] private float spitWarningDuration = 0.45f;
    [SerializeField] private float lowLobSpeed = 8.5f;
    [SerializeField] private float highLobSpeed = 7f;
    [SerializeField] private float lowLobArcBias = 0.4f;
    [SerializeField] private float highLobArcBias = 0.9f;
    [SerializeField] private float lowLobTargetJitter = 1.5f;
    [SerializeField] private float highLobTargetJitter = 2.5f;
    [SerializeField] private float sameLevelTolerance = 0.75f;
    [SerializeField] private float highArcVerticalThreshold = 1.2f;
    [SerializeField] private LayerMask arcBlockerLayers;
    [SerializeField] private int trajectorySamples = 10;
    [SerializeField] private float trajectoryTimeStep = 0.12f;

    [Header("Hit Flash")]
    [SerializeField] private float hitFlashDuration = 0.6f;
    [SerializeField] private float fatalHitFlashDuration = 0.08f;
    [SerializeField] private Material whiteFlashMaterial;

    [Header("Ledge Detection")]
    [SerializeField] private float ledgeCheckDistance = 0.8f;
    [SerializeField] private float ledgeCheckDepth = 0.6f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Death Sound")]
    [SerializeField] private AudioSource deathAudioSource;
    [SerializeField] private AudioClip[] deathClips = new AudioClip[5];

    private int currentHealth;
    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer[] flashRenderers;
    private Material[] originalMaterials;
    private Coroutine hitFlashRoutine;
    private bool isDying;
    private bool isAttacking;
    private float lastSpitTime = -999f;
    private SlashFeedback slashFeedback;

    private void Awake()
    {
        slashFeedback = GetComponent<SlashFeedback>();
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        animator = GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            SpriteRenderer animatorSpriteRenderer = animator.GetComponent<SpriteRenderer>();
            spriteRenderer = animatorSpriteRenderer != null ? animatorSpriteRenderer : GetComponent<SpriteRenderer>();
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
        TriggerHitFlash(currentHealth <= 0 ? fatalHitFlashDuration : hitFlashDuration);

        if (currentHealth <= 0)
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
        if (isDying || player == null)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        float verticalDelta = player.position.y - transform.position.y;

        if (distance > chaseRange)
        {
            SetIdle();
            return;
        }

        if (animator != null)
        {
            animator.SetBool("canSeePlayer", true);
        }

        FacePlayer();

        if (!isAttacking && distance <= stopRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (animator != null)
            {
                animator.SetBool("isMoving", false);
            }
        }
        else if (!isAttacking)
        {
            MoveTowardsPlayer();
        }

        if (!isAttacking && Time.time >= lastSpitTime + spitCooldown)
        {
            ShotMode shotMode = ChooseShotMode(distance, verticalDelta);
            if (shotMode != ShotMode.None)
            {
                StartCoroutine(SpitAttack(shotMode));
                lastSpitTime = Time.time;
            }
        }
    }

    private void FacePlayer()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        spriteRenderer.flipX = direction.x > 0f;
    }

    private void SetIdle()
    {
        if (animator != null)
        {
            animator.SetBool("canSeePlayer", false);
            animator.SetBool("isMoving", false);
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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
            if (animator != null)
            {
                animator.SetBool("isMoving", true);
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (animator != null)
            {
                animator.SetBool("isMoving", false);
            }
        }
    }

    private ShotMode ChooseShotMode(float distance, float verticalDelta)
    {
        if (toxicSpitPrefab == null || spitPoint == null)
        {
            return ShotMode.None;
        }

        // Base arc choice on distance: closer = low arc, farther = high arc
        ShotMode preferredMode = distance <= stopRange ? ShotMode.Low : ShotMode.High;
        
        // Check if preferred arc has clearance
        if (preferredMode == ShotMode.Low && HasArcClearance(lowLobSpeed, lowLobArcBias))
        {
            return ShotMode.Low;
        }
        
        if (preferredMode == ShotMode.High && HasArcClearance(highLobSpeed, highLobArcBias))
        {
            return ShotMode.High;
        }
        
        // If preferred arc is blocked, try the alternate arc
        ShotMode alternateMode = preferredMode == ShotMode.Low ? ShotMode.High : ShotMode.Low;
        if (alternateMode == ShotMode.Low && HasArcClearance(lowLobSpeed, lowLobArcBias))
        {
            return ShotMode.Low;
        }
        
        if (alternateMode == ShotMode.High && HasArcClearance(highLobSpeed, highLobArcBias))
        {
            return ShotMode.High;
        }
        
        // Don't shoot if neither arc has clearance - wait for better positioning
        return ShotMode.None;
    }

    private bool HasArcClearance(float launchSpeed, float arcBias)
    {
        if (player == null || spitPoint == null)
        {
            return false;
        }

        Vector2 startPosition = spitPoint.position;
        Vector2 targetPosition = player.position;
        Vector2 baseDirection = (targetPosition - startPosition).normalized;
        Vector2 launchVelocity = (baseDirection + Vector2.up * arcBias).normalized * launchSpeed;
        Vector2 gravity = Physics2D.gravity * 1.25f;
        Vector2 previousPoint = startPosition;
        LayerMask maskToUse = arcBlockerLayers.value == 0 ? groundLayer : arcBlockerLayers;

        for (int i = 1; i <= Mathf.Max(1, trajectorySamples); i++)
        {
            float time = i * trajectoryTimeStep;
            Vector2 nextPoint = startPosition + launchVelocity * time + 0.5f * gravity * (time * time);
            Vector2 segment = nextPoint - previousPoint;
            float segmentDistance = segment.magnitude;

            if (segmentDistance > 0f)
            {
                RaycastHit2D hit = Physics2D.Raycast(previousPoint, segment.normalized, segmentDistance, maskToUse);
                if (hit.collider != null)
                {
                    return false;
                }
            }

            previousPoint = nextPoint;
        }

        return true;
    }

    private IEnumerator SpitAttack(ShotMode shotMode)
    {
        isAttacking = true;

        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            animator.SetBool("isThrowing", true);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (warningHitBox != null)
        {
            warningHitBox.transform.localPosition = spriteRenderer != null && spriteRenderer.flipX ? new Vector3(-1.6f, 0f, 0f) : new Vector3(1.6f, 0f, 0f);
            warningHitBox.SetActive(true);
        }

        yield return new WaitForSeconds(spitWarningDuration);

        if (warningHitBox != null)
        {
            warningHitBox.SetActive(false);
        }

        if (toxicSpitPrefab != null && spitPoint != null)
        {
            Vector2 launchVelocity = BuildLaunchVelocity(shotMode);
            Vector3 spawnPosition = spitPoint.position;
            
            // Mirror spawn position if spider is flipped
            if (spriteRenderer != null && spriteRenderer.flipX)
            {
                Vector3 offset = spitPoint.localPosition;
                spawnPosition = transform.position + new Vector3(-offset.x, offset.y, offset.z);
            }
            
            GameObject spitObject = Instantiate(toxicSpitPrefab, spawnPosition, Quaternion.identity);
            ToxicSpitProjectile projectile = spitObject.GetComponent<ToxicSpitProjectile>();

            if (projectile != null)
            {
                projectile.Launch(launchVelocity);
                projectile.PlayAnimationForShotMode(shotMode);
            }
        }

        yield return new WaitForSeconds(0.35f);

        isAttacking = false;
        if (animator != null)
        {
            animator.SetBool("isThrowing", false);
        }
    }

    private Vector2 BuildLaunchVelocity(ShotMode shotMode)
    {
        Vector2 startPosition = spitPoint != null ? (Vector2)spitPoint.position : (Vector2)transform.position;
        Vector2 targetPosition = GetRandomAimPoint(shotMode, startPosition);
        Vector2 baseDirection = (targetPosition - startPosition).normalized;

        if (shotMode == ShotMode.High)
        {
            return (baseDirection + Vector2.up * highLobArcBias).normalized * highLobSpeed;
        }

        return (baseDirection + Vector2.up * lowLobArcBias).normalized * lowLobSpeed;
    }

    private Vector2 GetRandomAimPoint(ShotMode shotMode, Vector2 startPosition)
    {
        if (player == null)
        {
            return startPosition;
        }

        Vector2 playerPosition = player.position;
        float jitter = shotMode == ShotMode.High ? highLobTargetJitter : lowLobTargetJitter;
        float randomX = Random.Range(-jitter, jitter);
        float randomY = Random.Range(-jitter * 0.35f, jitter * 0.35f);

        Vector2 aimPoint = playerPosition + new Vector2(randomX, randomY);

        if (shotMode == ShotMode.Low)
        {
            aimPoint.y = Mathf.Min(aimPoint.y, playerPosition.y + jitter * 0.2f);
        }

        return aimPoint;
    }

    private void Die()
    {
        if (isDying)
        {
            return;
        }

        isDying = true;
        DisableCombatState();
        CoinManager.Instance?.AddCoins(2);
        PlayDeathSound();

        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        StartCoroutine(DieAfterDelay(animator != null ? .9f : 0f));
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

    public enum ShotMode
    {
        None,
        Low,
        High
    }
}