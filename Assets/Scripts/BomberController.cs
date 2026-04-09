using System.Collections;
using UnityEngine;

public class BomberController : Enemy
{
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");

    [SerializeField] private string explodeTriggerName = "Explode";
    [SerializeField] private int warningFlashCount = 3;
    [SerializeField] private float warningFlashOnDuration = 0.2f;
    [SerializeField] private float warningFlashOffDuration = 0.12f;
    [SerializeField] private float explosionHitboxDuration = 0.25f;

    private bool committedToExplode;
    private bool isDetonating;

    private new void Update()
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
                StartCoroutine(ExplodeRoutine());
            }
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

    private IEnumerator ExplodeRoutine()
    {
        isDetonating = true;
        rb.linearVelocity = Vector2.zero;
        // Keep isMoving true while warning so Animator stays on BomberRun (Idle has no path to Explode).

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
