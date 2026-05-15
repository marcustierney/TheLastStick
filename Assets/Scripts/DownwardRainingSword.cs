using System.Collections;
using UnityEngine;

/// <summary>
/// A sword that falls straight down from a fixed lane position, inspired by Hollow Knight's Radiance attack.
/// Starts with a warning phase (hidden), then becomes visible and falls straight down.
/// </summary>
public class DownwardRainingSword : MonoBehaviour
{
    public float fallSpeed = 100f;
    public float hoverDuration = 1f;
    public float hoverBobAmount = 0.18f;
    public float hoverBobSpeed = 5f;
    public int damage = 25;
    public float targetX;
    public float spawnY;
    public LayerMask groundLayer;
    public float maxFallDistance = 500f;

    private bool isActive = false;
    private float fallStartTime = 0f;
    private SpriteRenderer spriteRenderer;
    private Collider2D swordCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        swordCollider = GetComponent<Collider2D>();
        transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        SetVisible(false);
        IgnoreBossCollisions();
    }

    public void StartFall()
    {
        isActive = true;
        fallStartTime = Time.time + hoverDuration;
        StartCoroutine(HoverPhase());
    }

    private IEnumerator HoverPhase()
    {
        SetVisible(true);
        yield return new WaitForSeconds(hoverDuration);
    }

    private void Update()
    {
        if (!isActive) return;

        float currentTime = Time.time;

        // During hover phase, stay in position (hidden)
        if (currentTime < fallStartTime)
        {
            float hoverYOffset = Mathf.Sin(currentTime * hoverBobSpeed) * hoverBobAmount;
            transform.position = new Vector2(targetX, spawnY + hoverYOffset);
            return;
        }

        // Falling phase: move down
        float moveDistance = fallSpeed * Time.deltaTime;
        Vector2 currentPosition = transform.position;
        
        // Check for ground contact using raycast ahead
        RaycastHit2D groundHit = Physics2D.Raycast(currentPosition, Vector2.down, moveDistance + 1f, groundLayer);
        if (groundHit.collider != null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 nextPosition = transform.position + (Vector3.down * moveDistance);
        nextPosition.x = targetX;
        transform.position = nextPosition;
        
        // Fallback despawn if sword falls too far (shouldn't normally be reached)
        if (spawnY - transform.position.y > maxFallDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        float currentTime = Time.time;
        bool isFalling = currentTime >= fallStartTime;

        if (!isFalling) return;

        if (collision.CompareTag("Player"))
        {
            UpdateHealth health = collision.GetComponent<UpdateHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, transform.position, AnalyticsKeys.DeathCauseRainingSword);
            }
            Destroy(gameObject);
        }

        if (collision.CompareTag("BossTwo"))
        {
            return;
        }

        if (collision.CompareTag("Ground") || collision.CompareTag("Roof"))
        {
            Destroy(gameObject);
        }
    }

    private void SetVisible(bool visible)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;
        if (swordCollider != null)
            swordCollider.enabled = visible;
    }

    private void IgnoreBossCollisions()
    {
        GameObject boss = GameObject.FindGameObjectWithTag("BossTwo");
        if (boss == null || swordCollider == null)
        {
            return;
        }

        Collider2D[] bossColliders = boss.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < bossColliders.Length; i++)
        {
            Collider2D bossCollider = bossColliders[i];
            if (bossCollider != null)
            {
                Physics2D.IgnoreCollision(swordCollider, bossCollider, true);
            }
        }
    }
}
