using System.Collections;
using UnityEngine;

public class RainingSword : MonoBehaviour
{
    public float fallSpeed = 30f;
    public float warningDuration = 1.2f;
    public int damage = 25;
    public float targetX;
    public float spawnY;
    public float facingDownRotationZ = -90f;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.08f;
    public float maxFallDistance = 40f;
    private bool isFalling = false;
    private bool hasStarted = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D swordCollider;
    private float startFallY;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        swordCollider = GetComponent<Collider2D>();
        transform.rotation = Quaternion.Euler(0f, 0f, facingDownRotationZ);
        SetArmedState(false);
    }

    public void StartFall()
    {
        if (hasStarted)
        {
            return;
        }

        hasStarted = true;
        StartCoroutine(FallSequence());
    }

    private IEnumerator FallSequence()
    {
        transform.position = new Vector2(targetX, spawnY);
        SetArmedState(false);

        yield return new WaitForSeconds(warningDuration);

        transform.position = new Vector2(targetX, spawnY);
        startFallY = transform.position.y;
        SetArmedState(true);
        isFalling = true;
    }

    private void Update()
    {
        if (!isFalling) return;

        Vector3 nextPosition = transform.position + (Vector3.down * fallSpeed * Time.deltaTime);
        nextPosition.x = targetX;
        transform.position = nextPosition;

        if (groundLayer.value != 0)
        {
            Collider2D groundHit = Physics2D.OverlapCircle(transform.position, groundCheckRadius, groundLayer);
            if (groundHit != null)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (startFallY - transform.position.y >= maxFallDistance)
        {
            Destroy(gameObject);
            return;
        }

        if (transform.position.y < -20f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isFalling) return;

        if (collision.CompareTag("Roof"))
        {
            print("roof");
            Destroy(gameObject);
        }

        if (collision.CompareTag("Player"))
        {
            UpdateHealth health = collision.GetComponent<UpdateHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, transform.position, AnalyticsKeys.DeathCauseRainingSword);
            }
            Destroy(gameObject);
        }

        if (collision.CompareTag("Ground") || collision.CompareTag("Roof"))
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isFalling) return;
        if (collision.gameObject.CompareTag("Roof"))
        {
            Destroy(gameObject);
        }
        if (collision.gameObject.CompareTag("Ground"))
            Destroy(gameObject);
    }

    private void SetArmedState(bool armed)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = armed;
        }

        if (swordCollider != null)
        {
            swordCollider.enabled = armed;
        }
    }

}