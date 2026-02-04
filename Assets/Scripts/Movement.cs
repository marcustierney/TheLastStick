using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    private bool shiftHit = false;
    private bool areGrounded = false;
    private bool isWalking = false;
    private bool spacebarPressed = false;
    private Animator animator;
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
    private bool isJumping = false;

    private bool canMoveHorizontally = true;
    public bool CanMoveHorizontally
    {
        get
        {
            return canMoveHorizontally;
        }
        set
        {
            canMoveHorizontally = value;
        }
    }

    public bool IsFacingRight
    {
        get { return isFacingRight; }
    }

    private float knockbackTimer = 0f;
    public void ApplyKnockback(float duration)
    {
        knockbackTimer = duration;
    }


    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    // Update is called once per frame
    void Update()
    {
        // Decrement knockback timer
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            horizontal = 0f; // No horizontal input during knockback
            return; // Block all other inputs during knockback
        }

        if (CanMoveHorizontally)
        {
            horizontal = Input.GetAxisRaw("Horizontal");
            isWalking = (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D));
        }
        else
        {
            horizontal = 0f;
            isWalking = false;
        }

        // Update animator
        areGrounded = Grounded();
        if (animator != null)
        {
            animator.SetBool("isWalking", isWalking);
            animator.SetBool("areGrounded", areGrounded);
            animator.SetBool("shiftHit", shiftHit);
            if (spacebarPressed)
            {
                animator.SetBool("spacebarPressed", true);
                spacebarPressed = false;
            }
            else
            {
                animator.SetBool("spacebarPressed", false);
            }
        }

        // dash input (Shift) - only if cooldown expired
        if (CanMoveHorizontally && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) && dashCooldownTimer <= 0f)
        {
            isDashing = true;
            shiftHit = true;
            dashTimeLeft = dashTime;
            dashCooldownTimer = dashCooldown;
            StartDashIgnoreCollisions();
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // maintain vertical velocity
        }

        if (Input.GetButtonDown("Jump"))
        {
            spacebarPressed = true;
            if (Grounded() && !isJumping)
            {
                StartCoroutine(JumpWithDelay());
            }
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
                shiftHit = false;
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
        // Don't override velocity during knockback
        if (knockbackTimer > 0f)
            return;

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
        animator = GetComponent<Animator>();
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

    public void SwordJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpHeight);
    }

    private IEnumerator JumpWithDelay()
    {
        isJumping = true;
        yield return new WaitForSeconds(0.2f);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpHeight);
        isJumping = false;
    }
}
