using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private SlashFeedback slashFeedback;
    [Header("Audio")]
    [SerializeField] private AudioSource walkAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip walkLoopClip;
    [SerializeField] private AudioClip summonChargeClip;
    [SerializeField] private AudioClip swordDropClip;
    [SerializeField] private float swordDropSoundDelay = 1f;
    [SerializeField] private AudioClip dashChargeUpClip;
    [SerializeField] private AudioClip dashAttackClip;

    [Header("UI")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject hud;

    private void Awake()
    {
        slashFeedback = GetComponent<SlashFeedback>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        AutoAssignAudioSources();
        ConfigureAudioSource(walkAudioSource, walkLoopClip, true, 0.25f);
        ConfigureAudioSource(sfxAudioSource, null, false, 0.35f);
    }

    private void Start()
    {
        if (summoningSwords != null)
        {
            summoningSwords.onChargingChanged += OnSummonChargingChanged;
            summoningSwords.onSwordsDescending += OnSwordsDescending;
        }

        StartCoroutine(SummonAttackLoop());
    }

    private void OnDestroy()
    {
        if (summoningSwords != null)
        {
            summoningSwords.onChargingChanged -= OnSummonChargingChanged;
            summoningSwords.onSwordsDescending -= OnSwordsDescending;
        }
    }

    private void OnSummonChargingChanged(bool charging)
    {
        isSummonCharging = charging;
        if (animator != null)
        {
            animator.SetBool("isCharging", charging);
        }

        if (charging)
        {
            PlaySummonChargeSound();
        }
    }

    private bool IsBusyAttacking()
    {
        return isDashing || isChargingDash || isDazed || isSummonCharging;
    }

    private void Update()
    {
        if (isDead || player == null)
        {
            StopWalkSound();
            return;
        }

        if (IsBusyAttacking())
        {
            StopWalkSound();
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetBool("isMoving", false);
            return;
        }

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
        PlayWalkSound();
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
        StopWalkSound();
        PlayDashChargeUpSound();
        yield return new WaitForSeconds(dashChargeUpDuration);
        animator.SetBool("isChargingDash", false);
        isChargingDash = false;
        isDashing = true;
        isDealingDashDamage = true;
        animator.SetBool("isDashing", true);
        PlayDashSound();
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
            yield return new WaitUntil(() => isDead || !IsBusyAttacking());
            if (isDead)
            {
                yield break;
            }

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
        StopWalkSound();
        Debug.Log("killed");
        UpdateHealth playerHealth = player != null ? player.GetComponent<UpdateHealth>() : null;
        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponentInChildren<UpdateHealth>();
        }

        LevelRunStats.Instance?.EmitLevelCompleted(playerHealth, null);
        GameAnalytics.FlushIfReady();
        PlayerPrefs.SetInt("CurrentLevel", 3);
        PlayerPrefs.Save();
        SceneTransition.SetPendingNextScene("LevelThree", 3f);
        SceneManager.LoadScene("LoadingScreen");
        Destroy(gameObject);
    }


    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!isDealingDashDamage) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            UpdateHealth health = collision.gameObject.GetComponent<UpdateHealth>();
            if (health != null)
            {
                health.TakeDamage(dashDamage, transform.position, AnalyticsKeys.DeathCauseBossTwoDash);
            }
        }
    }

    private void PlayWalkSound()
    {
        AudioSource source = GetWalkAudioSource();
        if (source == null || walkLoopClip == null)
        {
            return;
        }

        source.loop = true;
        if (source.clip == null)
        {
            source.clip = walkLoopClip;
        }

        if (!source.isPlaying)
        {
            source.Play();
        }
    }

    private void StopWalkSound()
    {
        AudioSource source = GetWalkAudioSource();
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }

    private void AutoAssignAudioSources()
    {
        AudioSource[] audioSources = GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];
            if (source == null)
            {
                continue;
            }

            string sourceName = source.gameObject.name;
            if (walkAudioSource == null && sourceName.IndexOf("Walk", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                walkAudioSource = source;
                continue;
            }

            if (sfxAudioSource == null && sourceName.IndexOf("SFX", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                sfxAudioSource = source;
            }
        }

        if (sfxAudioSource == null && audioSources.Length > 0)
        {
            sfxAudioSource = audioSources[0];
        }

        if (walkAudioSource == null)
        {
            walkAudioSource = sfxAudioSource;
        }
    }

    private void ConfigureAudioSource(AudioSource source, AudioClip defaultClip, bool loop, float volume)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.ignoreListenerPause = true;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = 20f;

        if (defaultClip != null && source.clip == null)
        {
            source.clip = defaultClip;
        }
    }

    private AudioSource GetWalkAudioSource()
    {
        return walkAudioSource != null ? walkAudioSource : sfxAudioSource;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (sfxAudioSource == null || clip == null)
        {
            return;
        }

        sfxAudioSource.PlayOneShot(clip);
    }

    private void PlaySummonChargeSound()
    {
        PlayOneShot(summonChargeClip);
    }

    private void PlaySwordDropSound()
    {
        PlayOneShot(swordDropClip);
    }

    private void OnSwordsDescending()
    {
        StartCoroutine(PlaySwordDropSoundAfterDelay());
    }

    private IEnumerator PlaySwordDropSoundAfterDelay()
    {
        if (swordDropSoundDelay > 0f)
        {
            yield return new WaitForSeconds(swordDropSoundDelay);
        }

        PlaySwordDropSound();
    }

    private void PlayDashChargeUpSound()
    {
        PlayOneShot(dashChargeUpClip);
    }

    private void PlayDashSound()
    {
        PlayOneShot(dashAttackClip);
    }
}