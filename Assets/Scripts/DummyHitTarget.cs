using System.Collections;
using UnityEngine;

public class DummyHitTarget : MonoBehaviour, IHittable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 9999;
    [SerializeField] private bool destroyOnDeath;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hitBoolParameter = "Hit";
    [SerializeField] private float hitAnimationDuration = 0.2f;
    [SerializeField] private SlashFeedback slashFeedback;

    private int currentHealth;
    private Coroutine resetHitRoutine;

    private void Awake()
    {
        currentHealth = maxHealth;

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

        TakeDamage(hit.Damage);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        PlayHitAnimation();

        if (currentHealth <= 0 && destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    public void TutorialTakeDamage(int damage)
    {
        TakeDamage(damage);
    }

    private void PlayHitAnimation()
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