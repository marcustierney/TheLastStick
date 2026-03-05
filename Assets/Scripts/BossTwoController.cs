using System.Collections;
using UnityEngine;

public class BossTwoController : MonoBehaviour
{
    public BossSummoningSwords summoningSwords;
    public int maxHealth = 100;
    private int currentHealth;
    public float summonAttackCooldown = 12f; 
    private bool isDead = false;
    private bool isSummonOnCooldown = false;

    private void Start()
    {
        currentHealth = maxHealth;
        StartCoroutine(SummonAttackLoop());
    }

    private IEnumerator SummonAttackLoop()
    {
        yield return new WaitForSeconds(6f);

        while (!isDead)
        {
            if (summoningSwords != null)
            {
                summoningSwords.TriggerSummonAttack();
            }
            yield return new WaitForSeconds(summonAttackCooldown);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log("Boss health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Destroy(gameObject);
    }
}