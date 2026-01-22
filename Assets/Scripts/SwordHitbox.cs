using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    public int damage = 1;
    private BoxCollider2D boxCollider;

    private void Awake()
    {
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
        }
    }
}