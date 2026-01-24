using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    private float horizontal;
    private float speed = 5f;
    private float jumpHeight = 15f;
    private bool isFacingRight = true;

    private float dashTime = 0.2f;
    private float dashSpeed = 20f;
    private float dashCooldown = 0.5f;
    private float dashTimeLeft;
    private bool isDashing;
    private float dashCooldownTimer = 0f;
    private Collider2D playerCollider;
    private List<Collider2D> ignoredEnemyColliders = new List<Collider2D>();
    

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    // Update is called once per frame
    void Update()
    {
        horizontal = Input.GetAxisRaw("Horizontal");

        // dash input (Shift) - only if cooldown expired
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) && dashCooldownTimer <= 0f)
        {
            isDashing = true;
            dashTimeLeft = dashTime;
            dashCooldownTimer = dashCooldown;
            StartDashIgnoreCollisions();
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // maintain vertical velocity
        }

        if (Input.GetButtonDown("Jump") && Grounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpHeight);
        }

        // when player lets go of jump before max height is reached you start going back down
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f) 
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        // update dash timers
        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0f)
            {
                isDashing = false;
                EndDashIgnoreCollisions();
                rb.gravityScale = 5f;
            }
        }

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        Flip();
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            float dir = isFacingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(dir * dashSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);
        }
    }

    // initialize components 
    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
    }

    // bool for dashing checks
    public bool IsDashing
    {
        get { return isDashing; }
    }

    // beginning of dash so that player can pass through enemies and hopefully attacks
    // recheck once enemies are able to hit the player

    private void StartDashIgnoreCollisions()
    {
        if (playerCollider == null) return;
        ignoredEnemyColliders.Clear();
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject e in enemies)
        {
            Collider2D ec = e.GetComponent<Collider2D>();
            if (ec != null)
            {
                Physics2D.IgnoreCollision(playerCollider, ec, true);
                ignoredEnemyColliders.Add(ec);
            }
        }
    }

    // once this ends the collisions are re-enabled hopefully.
    // recheck once enemies are able to hit the player
    private void EndDashIgnoreCollisions()
    {
        if (playerCollider == null) return;
        foreach (Collider2D ec in ignoredEnemyColliders)
        {
            if (ec != null)
                Physics2D.IgnoreCollision(playerCollider, ec, false);
        }
        ignoredEnemyColliders.Clear();
    }

    private bool Grounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private void Flip()
    {
        if (FacingRight && horizontal < 0f || !FacingRight && horizontal > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    public bool FacingRight
    {
        get 
        { 
            return isFacingRight; 
        }
        private set 
        { 
            isFacingRight = value; 
        }
    }

    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }
}
