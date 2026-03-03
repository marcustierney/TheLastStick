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
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) return;
        
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
            TutorialEnemy tutorialEnemy = collision.GetComponent<TutorialEnemy>();
            if (tutorialEnemy != null)
            {
                tutorialEnemy.TutorialTakeDamage(damage);
                PlayHitDamageSound();
            }
            swordAttack.ForceExitSwordStandWithBounce();
        }
        if (collision.CompareTag("Boss"))
        {
            Debug.Log("hit");
            BossController boss = collision.GetComponent<BossController>();
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
            TutorialEnemy tutorialEnemy = collision.collider.GetComponent<TutorialEnemy>();
            if (tutorialEnemy != null)
            {
                tutorialEnemy.TutorialTakeDamage(damage);
                PlayHitDamageSound();
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