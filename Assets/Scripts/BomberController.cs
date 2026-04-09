using System.Collections;
using UnityEngine;

public class BomberController : Enemy
{
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");

    [SerializeField] private string explodeTriggerName = "Explode";
    [SerializeField] private float movementSpeed = 3f;
    [SerializeField] private int warningFlashCount = 3;
    [SerializeField] private float warningFlashOnDuration = 0.2f;
    [SerializeField] private float warningFlashOffDuration = 0.12f;
    [SerializeField] private float explosionHitboxDuration = 0.25f;

    private bool committedToExplode;
    private bool isWarningFlashing;
    private bool isDetonating;

    private void Start()
    {
        moveSpeed = movementSpeed;
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        if (isDetonating)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= chaseRange)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction.x > 0f;
            }

            if (!committedToExplode && distance > attackRange)
            {
                MoveTowardsPlayer();
            }
            else if (!committedToExplode && distance <= attackRange)
            {
                committedToExplode = true;
                StartCoroutine(ExplodeRoutine(skipWarningFlash: false));
            }
            else if (committedToExplode && isWarningFlashing)
            {
                MoveTowardsPlayer();
            }
        }
        else
        {
            if (committedToExplode && isWarningFlashing)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = direction.x > 0f;
                }

                MoveTowardsPlayer();
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (animator != null)
                {
                    animator.SetBool(IsMovingHash, false);
                }

                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = false;
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDetonating)
        {
            return;
        }

        if (!collision.gameObject.CompareTag("Player") && !collision.transform.root.CompareTag("Player"))
        {
            return;
        }

        StopAllCoroutines();
        committedToExplode = true;
        StartCoroutine(ExplodeRoutine(skipWarningFlash: true));
    }

    private IEnumerator ExplodeRoutine(bool skipWarningFlash)
    {
        if (!skipWarningFlash)
        {
            isWarningFlashing = true;

            for (int i = 0; i < warningFlashCount; i++)
            {
                if (warningHitBox != null)
                {
                    warningHitBox.SetActive(true);
                }

                yield return new WaitForSeconds(warningFlashOnDuration);

                if (warningHitBox != null)
                {
                    warningHitBox.SetActive(false);
                }

                yield return new WaitForSeconds(warningFlashOffDuration);
            }

            isWarningFlashing = false;
        }
        else
        {
            if (warningHitBox != null)
            {
                warningHitBox.SetActive(false);
            }

            isWarningFlashing = false;
        }
        isDetonating = true;
        rb.linearVelocity = Vector2.zero;
        if (animator != null)
        {
            animator.SetBool(IsMovingHash, false);
        }

        if (animator != null && !string.IsNullOrEmpty(explodeTriggerName))
        {
            animator.SetTrigger(explodeTriggerName);
        }

        if (swordHitbox != null)
        {
            swordHitbox.SetActive(true);
        }

        yield return new WaitForSeconds(explosionHitboxDuration);

        if (swordHitbox != null)
        {
            swordHitbox.SetActive(false);
        }

        Destroy(gameObject);
    }
}
