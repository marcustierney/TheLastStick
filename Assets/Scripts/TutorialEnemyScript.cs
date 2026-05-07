using System.Collections;
using UnityEngine;

public class TutorialEnemy : MonoBehaviour, IHittable
{
    private SlashFeedback slashFeedback;
    [SerializeField] private Animator animator;
    [SerializeField] private string hitBoolParameter = "Hit";
    [SerializeField] private float hitAnimationDuration = 0.52f;

    private Coroutine resetHitRoutine;

    private void Awake()
    {
        slashFeedback = GetComponent<SlashFeedback>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public void ReceiveHit(PlayerMeleeHit hit)
    {
        if (slashFeedback != null)
        {
            slashFeedback.PlaySlash(hit.ComboIndex);
        }

        PlayHitReaction();
    }

    private void PlayHitReaction()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(hitBoolParameter, true);

        if (resetHitRoutine != null)
        {
            StopCoroutine(resetHitRoutine);
        }

        resetHitRoutine = StartCoroutine(ResetHitBoolAfterDelay());
    }

    private IEnumerator ResetHitBoolAfterDelay()
    {
        yield return new WaitForSeconds(hitAnimationDuration);

        if (animator != null)
        {
            animator.SetBool(hitBoolParameter, false);
        }

        resetHitRoutine = null;
    }
}
