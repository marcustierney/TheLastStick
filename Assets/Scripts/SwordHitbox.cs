using System;
using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    public int damage = 1;
    private int baseDamage = 1;
    public int currentComboIndex;
    private BoxCollider2D boxCollider;
    private SwordAttack swordAttack;
    [SerializeField] private AudioSource hitDamageAudioSource;

    private void Awake()
    {
        swordAttack = GetComponentInParent<SwordAttack>();
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.enabled = false;
        baseDamage = damage;
    }

    public void ApplyDamagePercent(int percent)
    {
        float multiplier = 1f + (percent / 100f);
        damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
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
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }

        if (boxCollider == null)
        {
            return;
        }

        gameObject.SetActive(true);
        boxCollider.enabled = true;
        boxCollider.isTrigger = true;
    }
    public void Disable()
    {
        if (boxCollider == null) return;
        currentComboIndex = 0;
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

    private bool TryDeliverPlayerMeleeHit(Collider2D collision)
    {
        if (collision.GetComponentInParent<ShopInteractable>() != null)
        {
            return false;
        }

        IHittable hittable = collision.GetComponentInParent<IHittable>();
        if (hittable == null)
        {
            return false;
        }

        var hit = new PlayerMeleeHit
        {
            Damage = damage,
            ComboIndex = currentComboIndex,
            HitPoint = collision.ClosestPoint(transform.position)
        };
        hittable.ReceiveHit(hit);
        PlayHitDamageSound();
        return true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) return;
        if (IsNonDamageableEnemyHitbox(collision)) return;

        if (TryDeliverPlayerMeleeHit(collision))
        {
            swordAttack.ForceExitSwordStandWithBounce();
            return;
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

        if (TryDeliverPlayerMeleeHit(collision.collider))
        {
            swordAttack.ForceExitSwordStandWithBounce();
            return;
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
