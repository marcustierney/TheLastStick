using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BossController : MonoBehaviour, IHittable
{
    private static readonly Collider2D[] slamOverlapResults = new Collider2D[12];
    private const string HasSwordAnimatorParam = "HasSword";

    public Transform player;
    public Transform handPosition;      
    public BossSword sword;
    public float moveSpeed = 2f;
    public Collider2D bossCollider;
    private Rigidbody2D rb;
    private bool hasSword = true;
    private bool retrievingSword = false;
    private bool slamming = false;
    public float slamRange = 6f;
    public int slamDamage = 30;
    public Collider2D slamDamageZone;
    public GameObject slamWarningZone;
    public float slamWarningDuration = 0.7f;
    public float slamDamageDuration = 0.05f;
    public float swordThrowStartOffset = 1.5f;
    [SerializeField] private float swordThrowRange = 15f;
    [SerializeField] private float swordThrowPriorityMinDistance = 8f;
    [SerializeField] private float swordThrowSpawnDelay = 0.5f;
    [SerializeField] private float swordThrowRecoveryDelay = 0.35f;
    private int currentHealth = 100;
    public int maxHealth = 100;
    public GameObject bossSword;
    private BossHealth health;
    private Animator animator;
    private bool isThrowingAnim = false;
    private bool isSlamAnim = false;
    private RigidbodyConstraints2D cachedConstraints;
    private bool slamLockApplied = false;
    int slamCount = 0;
    [SerializeField] private AudioSource walkAudioSource;
    [SerializeField] private AudioSource slamAudioSource;
    [SerializeField] private AudioSource throwAudioSource;

    private SlashFeedback slashFeedback;

    void Awake()
    {
        slashFeedback = GetComponent<SlashFeedback>();
        rb = GetComponent<Rigidbody2D>();
        bossCollider = GetComponent<Collider2D>();
        maxHealth = 100;
        currentHealth = maxHealth;
        health = GetComponent<BossHealth>();
        animator = GetComponent<Animator>();
        cachedConstraints = rb.constraints;
        AutoAssignSlamDamageZone();
        EnsureSlamDamageZoneIsTrigger();
        SetSlamZonesActive(false);
        SetAnimatorHasSword(false);

        if (sword != null)
        {
            sword.gameObject.SetActive(false);
        }
    }
    void Update()
    {
        if (isSlamAnim || slamming || isThrowingAnim)
        {
            StopWalkSound();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer < 50f)
        {
            if (hasSword
                && distanceToPlayer >= swordThrowPriorityMinDistance
                && distanceToPlayer < swordThrowRange)
            {
                TryThrowSword();
            }
            else if (hasSword && slamCount < 10)
            {
                FacePlayer();
                if (distanceToPlayer < 3f && !isSlamAnim)
                {
                    StartCoroutine(GroundSlam());
                    slamCount += Random.Range(1, 11);
                }
                else
                {
                    MoveTowardsPlayer();
                }
            }
            else if (hasSword)
            {
                TryThrowSword();
            }
            else if (retrievingSword)
            {
                MoveToSword();
            }
            else if (!slamming)
            {
                MoveTowardsPlayer();
            }
            else
            {
                StopWalkSound();
            }
        }
    }



    public IEnumerator GroundSlam()
    {
        if (isSlamAnim)
        {
            yield break;
        }

        isSlamAnim = true;
        slamming = true;
        LockBossForSlam();

        PlaySlamSound();
        animator.SetTrigger("Slam");
        yield return new WaitForSeconds(0.3f);

        yield return StartCoroutine(SlamHitbox());

        UnlockBossAfterSlam();
        slamming = false;
        isSlamAnim = false;
    }

    void TryThrowSword()
    {
        if (sword == null)
        {
            MoveTowardsPlayer();
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance < swordThrowRange)
        {
            slamCount = 0;
            ThrowSword();
        }
        else
        {
            MoveTowardsPlayer();
        }
    }

    void ThrowSword()
    {
        if (isThrowingAnim) return; //stop the animation from repeating
        FacePlayer();
        hasSword = false;
        SetAnimatorHasSword(false);
        isThrowingAnim = true;
        PlayThrowSound();
        animator.SetTrigger("Throw");
        StartCoroutine(DelayedThrow());
    }

    private IEnumerator DelayedThrow()
    {
        yield return new WaitForSeconds(swordThrowSpawnDelay);

        if (sword == null)
        {
            isThrowingAnim = false;
            yield break;
        }

        hasSword = false;
        SetAnimatorHasSword(false);

        sword.gameObject.SetActive(true);

        sword.transform.parent = null;
        Vector2 direction = player.position - handPosition.position;
        Vector2 throwDirection = new Vector2(Mathf.Sign(direction.x), 0f).normalized;
        sword.transform.position = (Vector2)handPosition.position + (throwDirection * swordThrowStartOffset);
        sword.Throw(direction, this);

        if (swordThrowRecoveryDelay > 0f)
        {
            yield return new WaitForSeconds(swordThrowRecoveryDelay);
        }

        isThrowingAnim = false;
    }

    public void OnSwordStuck(BossSword stuckSword)
    {
        retrievingSword = true;
        SetAnimatorHasSword(false);
    }

    void MoveToSword()
    {
        if (sword == null) return;

        FaceSword();

        PlayWalkSound();

        transform.position = Vector2.MoveTowards(
            transform.position,
            sword.transform.position,
            4 * Time.deltaTime //moveSpeed * Time
        );

        float distance = Vector2.Distance(transform.position, sword.transform.position);

        if (distance < 3f)
        {
            StopWalkSound();
            sword.Retrieve(handPosition);
            retrievingSword = false;
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * 4, rb.linearVelocity.y); //dir * moveSpeed, linearvelocity.y

        if (Mathf.Abs(direction.x) > 0.01f)
        {
            PlayWalkSound();
        }
        else
        {
            StopWalkSound();
        }

        //flip sprite
        if (direction.x > 0)
            transform.localScale = new Vector3(5, 5, 5);
        else
            transform.localScale = new Vector3(-5, 5, 5);
    }

    public void OnSwordRetrieved()
    {
        hasSword = true;
        SetAnimatorHasSword(true);

        if (sword != null)
        {
            sword.gameObject.SetActive(false);
        }
    }

    private void FacePlayer()
    {
        if (player.position.x > transform.position.x)
            transform.localScale = new Vector3(5, 5, 5);
        else
            transform.localScale = new Vector3(-5, 5, 5);
    }

    private void FaceSword()
    {
        if (sword == null)
        {
            return;
        }

        if (sword.transform.position.x > transform.position.x)
            transform.localScale = new Vector3(5, 5, 5);
        else
            transform.localScale = new Vector3(-5, 5, 5);
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
        currentHealth -= damage;
        health.TakeDamage(damage);
        Debug.Log("damage " + damage + " cCurrent hp " + currentHealth);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        StopWalkSound();
        if (bossSword != null)
        {
            Destroy(bossSword.gameObject);
        }
        Debug.Log("killed");
        UpdateHealth playerHealth = player != null ? player.GetComponent<UpdateHealth>() : null;
        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponentInChildren<UpdateHealth>();
        }

        LevelRunStats.Instance?.EmitLevelCompleted(playerHealth, null);
        GameAnalytics.FlushIfReady();
        PlayerPrefs.SetInt("CurrentLevel", 2);
        PlayerPrefs.Save();
        SceneTransition.SetPendingNextScene("LevelTwo", 3f);
        SceneManager.LoadScene("LoadingScreen");
        Destroy(gameObject);
    }

    private bool IsPlayerInSlamZone()
    {
        if (!slamDamageZone)
        {
            AutoAssignSlamDamageZone();
            EnsureSlamDamageZoneIsTrigger();
        }

        if (!slamDamageZone || !player)
        {
            return false;
        }

        try
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider != null)
            {
                return slamDamageZone.bounds.Intersects(playerCollider.bounds);
            }

            return slamDamageZone.bounds.Contains(player.position);
        }
        catch (MissingReferenceException)
        {
            slamDamageZone = null;
            return false;
        }
    }

    private void AutoAssignSlamDamageZone()
    {
        if (slamDamageZone)
        {
            return;
        }

        GameObject slamZoneObject = GameObject.FindGameObjectWithTag("Enemy Attack");
        if (slamZoneObject != null)
        {
            slamDamageZone = slamZoneObject.GetComponent<Collider2D>();
        }
    }

    private void EnsureSlamDamageZoneIsTrigger()
    {
        if (slamDamageZone != null)
        {
            slamDamageZone.isTrigger = true;
        }
    }

    private IEnumerator SlamHitbox()
    {
        if (slamWarningZone != null)
        {
            slamWarningZone.SetActive(true);
            yield return new WaitForSeconds(slamWarningDuration);
            slamWarningZone.SetActive(false);
        }

        bool playerDamaged = false;
        if (slamDamageZone != null)
        {
            slamDamageZone.gameObject.SetActive(true);
            float timer = 0f;

            while (timer < slamDamageDuration)
            {
                if (!playerDamaged && TryDamagePlayerInSlamZone())
                {
                    playerDamaged = true;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            slamDamageZone.gameObject.SetActive(false);
        }
    }

    private void SetSlamZonesActive(bool isActive)
    {
        if (slamWarningZone != null)
        {
            slamWarningZone.SetActive(isActive);
        }

        if (slamDamageZone != null)
        {
            slamDamageZone.gameObject.SetActive(isActive);
        }
    }

    private bool TryDamagePlayerInSlamZone()
    {
        if (!slamDamageZone)
        {
            return false;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        int hitCount = slamDamageZone.Overlap(filter, slamOverlapResults);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = slamOverlapResults[i];
            if (hit == null)
            {
                continue;
            }

            if (!hit.CompareTag("Player"))
            {
                continue;
            }

            UpdateHealth playerHealth = hit.GetComponentInParent<UpdateHealth>();
            if (playerHealth == null)
            {
                playerHealth = hit.GetComponent<UpdateHealth>();
            }

            if (playerHealth != null && !IsMovementDashing(playerHealth))
            {
                playerHealth.TakeDamage(slamDamage, transform.position, AnalyticsKeys.DeathCauseBossSlam);
                return true;
            }
        }

        if (IsPlayerInSlamZone())
        {
            UpdateHealth playerHealth = player.GetComponent<UpdateHealth>();
            if (playerHealth != null && !IsMovementDashing(playerHealth))
            {
                playerHealth.TakeDamage(slamDamage, transform.position, AnalyticsKeys.DeathCauseBossSlam);
                return true;
            }
        }

        return false;
    }

    private void LockBossForSlam()
    {
        if (rb == null)
        {
            return;
        }

        if (!slamLockApplied)
        {
            cachedConstraints = rb.constraints;
            slamLockApplied = true;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = cachedConstraints
            | RigidbodyConstraints2D.FreezePositionX
            | RigidbodyConstraints2D.FreezePositionY
            | RigidbodyConstraints2D.FreezeRotation;
    }

    private void UnlockBossAfterSlam()
    {
        if (rb == null || !slamLockApplied)
        {
            return;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = cachedConstraints;
        slamLockApplied = false;
    }

    private bool IsMovementDashing(UpdateHealth playerHealth)
    {
        if (playerHealth == null)
        {
            return false;
        }

        Movement movement = playerHealth.GetComponent<Movement>();
        return movement != null && movement.IsDashing;
    }

    private void SetAnimatorHasSword(bool hasSwordState)
    {
        if (animator != null)
        {
            animator.SetBool(HasSwordAnimatorParam, hasSwordState);
        }
    }

    private void PlayWalkSound()
    {
        if (walkAudioSource == null)
        {
            return;
        }

        walkAudioSource.loop = true;
        if (!walkAudioSource.isPlaying)
        {
            walkAudioSource.Play();
        }
    }

    private void StopWalkSound()
    {
        if (walkAudioSource != null && walkAudioSource.isPlaying)
        {
            walkAudioSource.Stop();
        }
    }

    private void PlaySlamSound()
    {
        if (slamAudioSource == null)
        {
            return;
        }

        slamAudioSource.Play();
    }

    private void PlayThrowSound()
    {
        if (throwAudioSource == null)
        {
            return;
        }

        throwAudioSource.Play();
    }
}