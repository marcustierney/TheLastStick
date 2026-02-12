using UnityEngine;
using System.Collections;

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
    private bool slamming = false;
    public float slamRange = 6f;
    public int slamDamage = 15;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bossCollider = GetComponent<Collider2D>();
    }
    void Update()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (hasSword && distanceToPlayer < 12f)
        {
            FacePlayer();
            if (distanceToPlayer < 6f)
            {
                StartCoroutine(GroundSlam());
            }
            else
            {
                MoveTowardsPlayer();
            }
        }
        else if (hasSword)
        {
            TryThrowSword();
        }
        else if (retrievingSword)
        {
            MoveToSword();
        }
        else if (!slamming)
        {
            MoveTowardsPlayer();
        }
    }

    public IEnumerator GroundSlam()
    {
        hasSword = false;
        slamming = true;
        sword.transform.parent = null;
        sword.transform.position = handPosition.position; 
        sword.Slam(); 
        //Deal damage to player within radius
        if (Vector2.Distance(transform.position, player.position) <= 7f)
        {
            UpdateHealth health = player.GetComponent<UpdateHealth>();
            health.TakeDamage(slamDamage);
        }
        yield return new WaitForSeconds(3f);
        retrievingSword = true;
        slamming = false; 
    }

    void TryThrowSword()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > 8 && distance < 15f)
        {
            ThrowSword();
        }
        else
        {
            MoveTowardsPlayer();
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

    private void FacePlayer()
    {
        if (player.position.x > transform.position.x)
            transform.localScale = new Vector3(2, 2, 2);
        else
            transform.localScale = new Vector3(-2, 2, 2);
    }
}