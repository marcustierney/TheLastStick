using UnityEngine;
using System.Collections;
public class BossSword : MonoBehaviour
{
    public float speed = 4f;
    public int damage = 10;
    public bool isFlying = false;
    public bool isStuck = false;
    private Vector2 direction;
    private Rigidbody2D rb;
    public BossController boss;
    public Collider2D physicsCollider;
    public Collider2D playerTrigger;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    void Update()
    {
        if (isFlying)
        {
            transform.Rotate(0, 0, 60 * Time.deltaTime); //spin 60 deg/sec
        }
    }

    public void Throw(Vector2 dir, BossController owner)
    {
        boss = owner;
        //direction = dir.normalized;
        direction = new Vector2(Mathf.Sign(dir.x), 0f).normalized; //Mathf.Sign(dir.x) = right left
        isFlying = true;
        isStuck = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = direction * speed;
        physicsCollider.enabled = true;
        playerTrigger.enabled = true;
        StartCoroutine(EnableCollisionWithBoss());
    }

    private IEnumerator EnableCollisionWithBoss()
    {
        yield return new WaitForSeconds(4f);
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
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), boss.GetComponent<Collider2D>(), true);
        transform.position = handPosition.position;
        transform.parent = handPosition;
        playerTrigger.enabled = false;
        boss.OnSwordRetrieved();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isFlying && collision.CompareTag("Player"))
        {
            print("Player damage");
        }
        if (isStuck && collision.CompareTag("Boss"))
        {
            print("Boss collision sword");
            Retrieve(boss.handPosition);
        }
    }
}