using UnityEngine;

public class BossController : MonoBehaviour
{
    public Transform player;
    public Transform handPosition;
    public BossSword sword;
    public float moveSpeed = 2f;
    public Collider2D bossCollider;
    private Rigidbody2D rb;
    private bool hasSword = true;
    private bool retrievingSword = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bossCollider = GetComponent<Collider2D>();
    }
    void Update()
    {
        if (hasSword)
        {
            TryThrowSword();
        }
        else if (retrievingSword)
        {
            MoveToSword();
        }
        else
        {
            MoveTowardsPlayer();
        }
    }

    void TryThrowSword()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance < 15f)
        {
            ThrowSword();
        }
    }

    void ThrowSword()
    {
        hasSword = false;

        sword.transform.parent = null;

        Vector2 direction = player.position - handPosition.position;

        sword.Throw(direction, this);
    }

    public void OnSwordStuck(BossSword stuckSword)
    {
        retrievingSword = true;
    }

    void MoveToSword()
    {
        if (sword == null) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            sword.transform.position,
            moveSpeed * Time.deltaTime
        );

        float distance = Vector2.Distance(transform.position, sword.transform.position);

        if (distance < 3f)
        {
            sword.Retrieve(handPosition);
            retrievingSword = false;
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);

        //flip sprite
        if (direction.x > 0)
            transform.localScale = new Vector3(2, 2, 2);
        else
            transform.localScale = new Vector3(-2, 2, 2);
    }

    public void OnSwordRetrieved()
    {
        hasSword = true;
    }
}