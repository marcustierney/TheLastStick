using UnityEngine;
using System.Collections;
public class BossSword : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 30;
    public bool isFlying = false;
    public bool isStuck = false;
    private Vector2 direction;
    private Rigidbody2D rb;
    public BossController boss;
    public Collider2D physicsCollider;
    public Collider2D playerTrigger;
    private Vector2 startPosition;
    public int boomerangDistance = 25;
    private bool returning = false;
    private Collider2D[] ignoredPlayerColliders;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    void Update()
    {
        if (isFlying)
        {
            transform.Rotate(0, 0, 400 * Time.deltaTime); //spin 400 deg/sec
            if (!returning)
            {
                float horizontalTravel = Mathf.Abs(transform.position.x - startPosition.x);
                print(horizontalTravel);
                if (horizontalTravel >= 20)
                {
                    returning = true;
                }
            }

            if (returning)
            {
                Vector2 toBoss = (Vector2)boss.handPosition.position - rb.position;
                rb.linearVelocity = -direction * 10f; //-dir * speed
                if (toBoss.magnitude < 1f)
                {
                    Retrieve(boss.handPosition);
                }
            }
        }
    }

    public void Throw(Vector2 dir, BossController owner)
    {
        boss = owner;
        //direction = dir.normalized;
        direction = new Vector2(Mathf.Sign(dir.x), 0f).normalized; //Mathf.Sign(dir.x) = right left'
        startPosition = transform.position;
        returning = false;
        isFlying = true;
        isStuck = false;
        CachePlayerColliders();
        SetPlayerCollisionIgnored(true);
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = direction * 10f; //dir * speed
        physicsCollider.enabled = true;
        playerTrigger.enabled = true;
        StartCoroutine(EnableCollisionWithBoss());
    }

    private IEnumerator EnableCollisionWithBoss()
    {
        yield return new WaitForSeconds(1f);
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), boss.GetComponent<Collider2D>(), false);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Sword hit: " + collision.gameObject.name);
        //if (!isFlying) return;
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log("Sword stuck wall");
            StickIntoWall();
        }
        if (collision.gameObject.CompareTag("Boss"))
        {
            print("Boss collision sword");
            Retrieve(boss.handPosition);
        }
    }

    void StickIntoWall()
    {
        isFlying = false;
        isStuck = true;
        rb.angularVelocity = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        boss.OnSwordStuck(this);
    }

    public void Retrieve(Transform handPosition)
    {
        isStuck = false;
        isFlying = false;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        SetPlayerCollisionIgnored(false);
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), boss.GetComponent<Collider2D>(), true);
        transform.parent = handPosition;
        transform.position = handPosition.position;
        transform.localRotation = Quaternion.Euler(0f, 0f, 230f);
        playerTrigger.enabled = false;
        boss.OnSwordRetrieved();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isFlying && collision.CompareTag("Player"))
        {
            UpdateHealth health = collision.GetComponent<UpdateHealth>();
            health.TakeDamage(damage, transform.position, AnalyticsKeys.DeathCauseBossThrownSword);
        }
        if (isStuck && collision.CompareTag("Boss"))
        {
            print("Boss collision sword");
            Retrieve(boss.handPosition);
        }
    }

    public void Slam()
    {
        isFlying = false;
        isStuck = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        SetPlayerCollisionIgnored(false);
        float directionX = Mathf.Sign(boss.transform.localScale.x);
        Vector3 groundPosition = boss.transform.position;
        groundPosition.x += directionX * 2.5f;
        RaycastHit2D hit = Physics2D.Raycast(boss.transform.position, Vector2.down, 10f, LayerMask.GetMask("Ground")); //find ground with raycast
        if (hit.collider != null)
        {
            groundPosition.y = hit.point.y; //place sword exactly on the ground
        }
        else
        {
            groundPosition.y -= 1f;
        }
        transform.position = groundPosition;
        StartCoroutine(RotateSwordTo(directionX * -200f, 0.5f));
        physicsCollider.enabled = true;
        playerTrigger.enabled = true;
        isStuck = true;
    }

    private void CachePlayerColliders()
    {
        if (boss == null || boss.player == null)
        {
            ignoredPlayerColliders = null;
            return;
        }

        ignoredPlayerColliders = boss.player.GetComponentsInChildren<Collider2D>(true);
    }

    private void SetPlayerCollisionIgnored(bool ignore)
    {
        if (physicsCollider == null)
        {
            physicsCollider = GetComponent<Collider2D>();
        }

        if (physicsCollider == null || ignoredPlayerColliders == null)
        {
            return;
        }

        for (int i = 0; i < ignoredPlayerColliders.Length; i++)
        {
            Collider2D playerCollider = ignoredPlayerColliders[i];
            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(physicsCollider, playerCollider, ignore);
            }
        }
    }

    public IEnumerator RotateSwordTo(float targetZ, float duration) //Sword spin for ground slam
    {
        float startZ = transform.eulerAngles.z;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float z = Mathf.LerpAngle(startZ, targetZ, elapsed / duration);
            transform.rotation = Quaternion.Euler(0f, 0f, z);
            yield return null;
        }
        transform.rotation = Quaternion.Euler(0f, 0f, targetZ);
    }

}