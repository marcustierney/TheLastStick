using System.Collections;
using UnityEngine;

public class RainingSword : MonoBehaviour
{
    public float fallSpeed = 30f;
    public float warningDuration = 1.2f;
    public int damage = 25;
    public float targetX;
    public float spawnY;
    private bool isFalling = false;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.rotation = Quaternion.Euler(0f, 0f, 45f);
    }

    public void StartFall()
    {
        StartCoroutine(FallSequence());
    }

    private IEnumerator FallSequence()
    {
        transform.position = new Vector2(targetX, spawnY + 1000f);
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
        yield return new WaitForSeconds(warningDuration);
        transform.position = new Vector2(targetX, spawnY);
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
        isFalling = true;
    }

    private void Update()
    {
        if (!isFalling) return;
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
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
            print("roof collision");
            Destroy(gameObject);
        }
        if (collision.gameObject.CompareTag("Ground"))
            Destroy(gameObject);
    }

}