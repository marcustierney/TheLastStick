using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    public int damage = 1;
    private BoxCollider2D boxCollider;
    private SwordAttack swordAttack;
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
        if (collision.CompareTag("Enemy"))
        {
            Debug.Log("hit");
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            swordAttack.ForceExitSwordStand();
        }
        if (collision.CompareTag("Ground"))
        {
            Debug.Log("Sword ground trigger");
            swordAttack.swordStandTouchGround = true;
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Enemy"))
        {
            Enemy enemy = collision.collider.GetComponent<Enemy>();
            enemy.TakeDamage(damage);
            swordAttack.ForceExitSwordStand();
        }
        if (collision.collider.CompareTag("Ground"))
        {
            Debug.Log("Sword landed on ground");
            swordAttack.swordStandTouchGround = true;
        }
    }
}