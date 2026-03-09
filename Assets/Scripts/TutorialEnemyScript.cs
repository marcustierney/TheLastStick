using UnityEngine;
using System.Collections;
public class TutorialEnemy : MonoBehaviour
{
    public int maxHealth = 3;
    private int currentHealth;
    [SerializeField] private AudioSource deathAudioSource;

    private void Awake()
    {
        currentHealth = maxHealth;
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