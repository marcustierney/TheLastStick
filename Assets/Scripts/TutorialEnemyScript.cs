using UnityEngine;
using System.Collections;
public class TutorialEnemy : MonoBehaviour, IHittable
{
    public int maxHealth = 3;
    private int currentHealth;
    [SerializeField] private AudioSource deathAudioSource;
    [SerializeField] private SlashFeedback slashFeedback;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void ReceiveHit(PlayerMeleeHit hit)
    {
        if (slashFeedback != null)
        {
            slashFeedback.PlaySlash(hit.ComboIndex);
        }

        TutorialTakeDamage(hit.Damage);
    }

    public void TutorialTakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("damage " + damage + " cCurrent hp " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("killed");
        PlayDeathSound();
        Destroy(gameObject);
    }

    private void PlayDeathSound()
    {
        if (deathAudioSource == null || deathAudioSource.clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(deathAudioSource.clip, transform.position);
    }
}