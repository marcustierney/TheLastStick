using System;
using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    public int damage = 1;
    private BoxCollider2D boxCollider;
    private SwordAttack swordAttack;
    [SerializeField] private AudioSource hitDamageAudioSource;

    private void Awake()
    {
        swordAttack = GetComponentInParent<SwordAttack>();
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.enabled = false;
    }
    public void EnablePlatform()
    {
        if (boxCollider == null) return;
        boxCollider.enabled = true;
        boxCollider.isTrigger = false; //standable
        gameObject.SetActive(true);
    }

    public void EnableAttack()
    {
        boxCollider.enabled = true;
        boxCollider.isTrigger = true; 
        gameObject.SetActive(true);
    }
    public void Disable()
    {
        if (boxCollider == null) return;
        gameObject.SetActive(false);
    }

    private bool IsNonDamageableEnemyHitbox(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider.GetComponent<EnemySwordHitbox>() != null)
        {
            return true;
        }

        string colliderTag = collider.tag;
        if (colliderTag == "Enemy Attack" || colliderTag == "EnemyAttack")
        {
            return true;
        }

        return collider.name.IndexOf("warning", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) return;
        if (IsNonDamageableEnemyHitbox(collision)) return;

        ShieldEnemyController shieldEnemy = collision.GetComponentInParent<ShieldEnemyController>();
        if (shieldEnemy != null)
        {
            shieldEnemy.TakeDamage(damage, transform.position);
            PlayHitDamageSound();
            swordAttack.ForceExitSwordStandWithBounce();
            return;
        }

        SpiderEnemy spiderEnemy = collision.GetComponentInParent<SpiderEnemy>();
        if (spiderEnemy != null)
        {
            spiderEnemy.TakeDamage(damage);
            PlayHitDamageSound();
            swordAttack.ForceExitSwordStandWithBounce();
            return;
        }
        
        if (collision.CompareTag("Enemy"))
        {
            Debug.Log("hit");
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                PlayHitDamageSound();
            }
            else
            {
                ThrowEnemy throwEnemy = collision.GetComponent<ThrowEnemy>();
                if (throwEnemy != null)
                {
                    throwEnemy.TakeDamage(damage);
                    PlayHitDamageSound();
                }
            }
            swordAttack.ForceExitSwordStandWithBounce();
        }
        if (collision.CompareTag("TutorialEnemy"))
        {
            Debug.Log("hit tutorial enemy");
            DummyHitTarget dummyHitTarget = collision.GetComponent<DummyHitTarget>();
            if (dummyHitTarget != null)
            {
                dummyHitTarget.TutorialTakeDamage(damage);
                PlayHitDamageSound();
            }
            else
            {
                TutorialEnemy tutorialEnemy = collision.GetComponent<TutorialEnemy>();
                if (tutorialEnemy != null)
                {
                    tutorialEnemy.TutorialTakeDamage(damage);
                    PlayHitDamageSound();
                }
            }
            swordAttack.ForceExitSwordStandWithBounce();
        }
        if (collision.CompareTag("Boss"))
        {
            Debug.Log("hit");
            BossController boss = collision.GetComponent<BossController>();
            if (boss == null)
            {
                boss = collision.GetComponentInParent<BossController>();
            }
            if (boss != null)
            {
                boss.TakeDamage(damage);
                PlayHitDamageSound();
            }
            swordAttack.ForceExitSwordStandWithBounce();
        }
        if (collision.CompareTag("BossTwo"))
        {
            Debug.Log("hit");
            BossTwoController boss = collision.GetComponent<BossTwoController>();
            if (boss == null)
            {
                boss = collision.GetComponentInParent<BossTwoController>();
            }
            if (boss != null)
            {
                boss.TakeDamage(damage);
                PlayHitDamageSound();
            }
            swordAttack.ForceExitSwordStandWithBounce();
        }
        if (collision.CompareTag("Ground"))
        {
            Debug.Log("Sword ground trigger");
            swordAttack.swordStandTouchGround = true;
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player")) return;
        if (IsNonDamageableEnemyHitbox(collision.collider)) return;

        ShieldEnemyController shieldEnemy = collision.collider.GetComponentInParent<ShieldEnemyController>();
        if (shieldEnemy != null)
        {
            shieldEnemy.TakeDamage(damage, transform.position);
            PlayHitDamageSound();
            swordAttack.ForceExitSwordStandWithBounce();
            return;
        }

        SpiderEnemy spiderEnemy = collision.collider.GetComponentInParent<SpiderEnemy>();
        if (spiderEnemy != null)
        {
            spiderEnemy.TakeDamage(damage);
            PlayHitDamageSound();
            swordAttack.ForceExitSwordStandWithBounce();
            return;
        }
        
        if (collision.collider.CompareTag("Enemy"))
        {
            Enemy enemy = collision.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                PlayHitDamageSound();
            }
            else
            {
                ThrowEnemy throwEnemy = collision.collider.GetComponent<ThrowEnemy>();
                if (throwEnemy != null)
                {
                    throwEnemy.TakeDamage(damage);
                    PlayHitDamageSound();
                }
            }
            swordAttack.ForceExitSwordStandWithBounce();
        }
        if (collision.collider.CompareTag("TutorialEnemy"))
        {
            DummyHitTarget dummyHitTarget = collision.collider.GetComponent<DummyHitTarget>();
            if (dummyHitTarget != null)
            {
                dummyHitTarget.TutorialTakeDamage(damage);
                PlayHitDamageSound();
            }
            else
            {
                TutorialEnemy tutorialEnemy = collision.collider.GetComponent<TutorialEnemy>();
                if (tutorialEnemy != null)
                {
                    tutorialEnemy.TutorialTakeDamage(damage);
                    PlayHitDamageSound();
                }
            }
            swordAttack.ForceExitSwordStandWithBounce();
        }
        if (collision.collider.CompareTag("Ground"))
        {
            Debug.Log("Sword landed on ground");
            swordAttack.swordStandTouchGround = true;
        }
    }

    private void PlayHitDamageSound()
    {
        if (hitDamageAudioSource != null)
        {
            hitDamageAudioSource.Play();
        }
    }
}