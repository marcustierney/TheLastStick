using System.Collections;
using UnityEngine;

public class BossFloatingSwords : MonoBehaviour
{
    public float bobSpeed = 2f;        
    public float bobAmount = 0.2f;     
    public float followSpeed = 5f;     
    public float hoverHeight = 6.5f;     
    public float horizontalOffset = 0f; 
    public float diveSpeed = 18f;      
    public float returnSpeed = 6f;     
    public float groundStabDuration = 0.3f; 
    public int damage = 8;
    public LayerMask groundLayer;
    private Transform player;
    private Vector2 floatTarget;
    private bool isDiving = false;
    private bool isReturning = false;
    private float bobOffset = 0f;      

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public void Init(float horOffset, float phase)
    {
        horizontalOffset = horOffset;
        bobOffset = phase; 
    }

    private void Update()
    {
        if (player == null || isDiving || isReturning)
        {
            return;
        }
        float targetX = player.position.x + horizontalOffset;
        float targetY = player.position.y + hoverHeight + Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobAmount;
        floatTarget = new Vector2(targetX, targetY);
        transform.position = Vector2.Lerp(transform.position, floatTarget, followSpeed * Time.deltaTime);
    }

    public IEnumerator Dive()
    {
        isDiving = true;
        Vector2 targetPos = new Vector2(player.position.x, player.position.y);
        Vector2 diveDirection = (targetPos - (Vector2)transform.position).normalized;
        while (isDiving)
        {
            transform.position += (Vector3)(diveDirection * diveSpeed * Time.deltaTime);
            Collider2D groundHit = Physics2D.OverlapCircle(transform.position, 0.2f, groundLayer);
            if (groundHit != null)
            {
                break;
            }
            if (transform.position.y < player.position.y - 8f)
            {
                break;
            }
            yield return null;
        }

        isDiving = false;
        yield return new WaitForSeconds(groundStabDuration);
        isReturning = true;
        while (isReturning)
        {
            float targetX = player.position.x + horizontalOffset;
            float targetY = player.position.y + hoverHeight;
            Vector2 returnTarget = new Vector2(targetX, targetY);
            transform.position = Vector2.MoveTowards(transform.position, returnTarget, returnSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, returnTarget) < 0.1f)
            {
                isReturning = false;
            }
            yield return null;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            UpdateHealth health = collision.gameObject.GetComponent<UpdateHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, transform.position);
            }
        }
    }
}