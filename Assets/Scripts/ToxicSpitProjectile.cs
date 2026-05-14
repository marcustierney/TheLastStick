using UnityEngine;

public class ToxicSpitProjectile : MonoBehaviour
{
    [SerializeField] private float damage = 8f;
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float gravityScale = 1.65f;
    [SerializeField] private GameObject poisonPuddlePrefab;
    [SerializeField] private LayerMask impactLayers;
    [SerializeField] private string lowShotAnimationName = "Spit";
    [SerializeField] private string highShotAnimationName = "LongSpit";

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool hasImpacted;

    private void Awake(){
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (rb != null)
        {
            rb.gravityScale = gravityScale;
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 launchVelocity)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (rb != null)
        {
            rb.linearVelocity = launchVelocity;
        }

        // Flip sprite based on launch direction
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = launchVelocity.x > 0f;
        }
    }

    public void PlayAnimationForShotMode(ToxicSpiderEnemy.ShotMode shotMode)
    {
        if (animator == null)
        {
            return;
        }

        string stateName = shotMode == ToxicSpiderEnemy.ShotMode.High ? highShotAnimationName : lowShotAnimationName;
        if (!string.IsNullOrWhiteSpace(stateName))
        {
            animator.Play(stateName, 0, 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasImpacted || collision == null)
        {
            return;
        }

        if (!IsImpactTarget(collision))
        {
            return;
        }

        hasImpacted = true;

        if (collision.CompareTag("Player"))
        {
            UpdateHealth playerHealth = collision.GetComponent<UpdateHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, transform.position, AnalyticsKeys.DeathCauseToxicSpit);
            }
        }
        else
        {
            // Only spawn puddle on ground/wall impact, not on player
            SpawnPoisonPuddle(collision);
        }

        Destroy(gameObject);
    }

    private bool IsImpactTarget(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            return true;
        }

        if (impactLayers.value != 0)
        {
            return (impactLayers.value & (1 << collision.gameObject.layer)) != 0;
        }

        return collision.CompareTag("Ground");
    }

    private void SpawnPoisonPuddle(Collider2D collision)
    {
        if (poisonPuddlePrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = transform.position;
        if (collision != null)
        {
            spawnPosition = collision.ClosestPoint(transform.position);
        }
        
        // Offset slightly above ground to ensure visibility
        spawnPosition.y += 0.2f;

        Instantiate(poisonPuddlePrefab, spawnPosition, Quaternion.identity);
    }
}