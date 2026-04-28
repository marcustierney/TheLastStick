using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private bool areGrounded = false; 
    private bool isWalking = false;  
    private bool spacebarPressed = false;
    private Animator animator;
    private float horizontal;
    private float speed = 5f;
    private float runSpeed = 15f;
    private float jumpHeight = 15f;
    private bool isFacingRight = true;

    private float dashTime = 0.2f; 
    private float dashSpeed = 20f;
    private float dashCooldown = 0.5f;
    private float dashTimeLeft;
    private bool isDashing;
    private float dashCooldownTimer = 0f; 
    private Collider2D playerCollider; 
    private List<Collider2D> ignoredEnemyColliders = new List<Collider2D>(); // Colliders ignored during dash
    private List<Collider2D> ignoredIFrameColliders = new List<Collider2D>(); // Colliders ignored during I-frames (e.g. after taking damage)
    private bool isJumping = false; 
    private bool shiftHold = false;
    private int lastKeyboardHorizontalDirection = 1;
    private bool previousLeftPressed = false;
    private bool previousRightPressed = false;

    private bool canMoveHorizontally = true;
    [SerializeField] private AudioSource groundedMoveAudioSource;
    [SerializeField] private float walkMovePitch = 1f;
    [SerializeField] private float runMovePitch = 1.3f;
    [SerializeField] private AudioSource dashAudioSource;
    [SerializeField] private AudioSource jumpAudioSource;
    private bool wasGroundedMoving = false;

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
    [SerializeField] private List<string> dashIgnoreTags = new List<string> { "Enemy", "EnemyAttack", "Enemy Attack", "Boss" , "TutorialEnemy"}; // Tags of objects to ignore during dash 

    // Update is called once per frame
    void Update()
    {
        // Update dash timers even if knockback is active
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

        // Decrement knockback timer
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            horizontal = 0f; // No horizontal input during knockback
            if (wasGroundedMoving)
            {
                StopGroundedMoveSound();
                wasGroundedMoving = false;
            }
            return; // Block all other inputs during knockback
        }

        if (CanMoveHorizontally)
        {
            horizontal = ResolveHorizontalInput();
            isWalking = Mathf.Abs(horizontal) > 0.01f;
        }
        else
        {
            horizontal = 0f;
            isWalking = false;
        }

        bool shiftHeld = IsSprintHeld();
        shiftHold = !isDashing && shiftHeld && Mathf.Abs(horizontal) > 0f;

        // Update animator
        areGrounded = Grounded();
        if (animator != null)
        {
            animator.SetBool("isDashing", isDashing);
            animator.SetBool("isWalking", isWalking);
            animator.SetBool("areGrounded", areGrounded);
            animator.SetBool("shiftHold", shiftHold);
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

        bool isGroundedMoving = areGrounded && Mathf.Abs(horizontal) > 0f && !isDashing && knockbackTimer <= 0f;
        if (isGroundedMoving && !wasGroundedMoving)
        {
            PlayGroundedMoveSound();
        }
        if (isGroundedMoving)
        {
            UpdateGroundedMoveSoundPitch();
        }
        else if (!isGroundedMoving && wasGroundedMoving)
        {
            StopGroundedMoveSound();
        }
        wasGroundedMoving = isGroundedMoving;

        // dash input (Shift) - only if cooldown expired
        if (CanMoveHorizontally && !isDashing && IsSprintPressedThisFrame() && Mathf.Abs(horizontal) > 0.01f && dashCooldownTimer <= 0f)
        {
            isDashing = true;
            dashTimeLeft = dashTime;
            dashCooldownTimer = dashCooldown;
            PlayDashSound();
            StartDashIgnoreCollisions();
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // maintain vertical velocity
        }

        if (CanMoveHorizontally && !isDashing && inputActions.Player.Jump.WasPressedThisFrame())
        {
            spacebarPressed = true;
            if (Grounded() && !isJumping)
            {
                StartCoroutine(JumpWithDelay());
            }
        }

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
            float moveSpeed = shiftHold ? runSpeed : speed;
            rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
        }
    }

    // initialize components 
    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        inputActions?.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Player.Disable();
    }

    // bool for dashing checks
    public void AddSpeed(float amount)
    {
        speed += amount;
        runSpeed += amount;
    }

    public bool IsDashing
    {
        get { return isDashing; }
    }

    public void StartIFrameIgnoreCollisions()
    {
        if (playerCollider == null) return;
        ignoredIFrameColliders.Clear();
        ApplyIgnoreForTags(ignoredIFrameColliders);
    }

    public void EndIFrameIgnoreCollisions()
    {
        if (playerCollider == null) return;
        foreach (Collider2D ec in ignoredIFrameColliders)
        {
            if (ec != null && !ignoredEnemyColliders.Contains(ec))
                Physics2D.IgnoreCollision(playerCollider, ec, false);
        }
        ignoredIFrameColliders.Clear();
    }

    // beginning of dash so that player can pass through enemies and hopefully attacks
    // recheck once enemies are able to hit the player

    private void StartDashIgnoreCollisions()
    {
        if (playerCollider == null) return;
        ignoredEnemyColliders.Clear();
        ApplyIgnoreForTags(ignoredEnemyColliders);
    }

    // once this ends the collisions are re-enabled hopefully.
    // recheck once enemies are able to hit the player
    private void EndDashIgnoreCollisions() 
    {
        if (playerCollider == null) return;
        foreach (Collider2D ec in ignoredEnemyColliders)
        {
            if (ec != null && !ignoredIFrameColliders.Contains(ec))
                Physics2D.IgnoreCollision(playerCollider, ec, false);
        }
        ignoredEnemyColliders.Clear();
    }

    private void ApplyIgnoreForTags(List<Collider2D> ignoredColliders) // helper function to apply collision ignores for a list of tags and store the ignored colliders in the provided list
    {
        foreach (string tag in dashIgnoreTags)
        {
            if (string.IsNullOrWhiteSpace(tag))
                continue;

            GameObject[] targets;
            try
            {
                targets = GameObject.FindGameObjectsWithTag(tag);
            }
            catch
            {
                Debug.LogWarning($"Dash ignore tag not defined: {tag}");
                continue;
            }

            foreach (GameObject target in targets)
            {
                Collider2D targetCollider = target.GetComponent<Collider2D>();
                AddIgnoredCollider(ignoredColliders, targetCollider);

                Collider2D[] childColliders = target.GetComponentsInChildren<Collider2D>(true);
                foreach (Collider2D childCollider in childColliders)
                {
                    AddIgnoredCollider(ignoredColliders, childCollider);
                }

                if (tag == "Enemy")
                {
                    EnemySwordHitbox esh = target.GetComponentInChildren<EnemySwordHitbox>(true);
                    if (esh != null)
                    {
                        Collider2D eshc = esh.GetComponent<Collider2D>();
                        AddIgnoredCollider(ignoredColliders, eshc);
                    }
                }
            }
        }
    }

    private void AddIgnoredCollider(List<Collider2D> ignoredColliders, Collider2D targetCollider) // helper function to add colliders to ignore list and set physics ignore
    {
        if (targetCollider == null) return;
        if (ignoredColliders.Contains(targetCollider)) return;
        Physics2D.IgnoreCollision(playerCollider, targetCollider, true);
        ignoredColliders.Add(targetCollider);
    }

    private bool Grounded() // check if player is on the ground by checking for overlap with ground layer at the position of the groundCheck transform
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private void Flip() // flip player sprite based on movement direction
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
        PlayJumpSound();
    }

    private IEnumerator JumpWithDelay()
    {
        isJumping = true;
        yield return new WaitForSeconds(0f);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpHeight);
        PlayJumpSound();
        isJumping = false;
    }

    private void PlayGroundedMoveSound()
    {
        if (groundedMoveAudioSource == null)
        {
            return;
        }

        groundedMoveAudioSource.pitch = shiftHold ? runMovePitch : walkMovePitch;
        groundedMoveAudioSource.loop = true;
        if (!groundedMoveAudioSource.isPlaying)
            groundedMoveAudioSource.Play();
    }

    private void UpdateGroundedMoveSoundPitch()
    {
        if (groundedMoveAudioSource == null)
        {
            return;
        }

        groundedMoveAudioSource.pitch = shiftHold ? runMovePitch : walkMovePitch;
    }

    private void StopGroundedMoveSound()
    {
        if (groundedMoveAudioSource != null && groundedMoveAudioSource.isPlaying)
        {
            groundedMoveAudioSource.Stop();
        }
    }

    private void PlayDashSound()
    {
        if (dashAudioSource == null)
        {
            return;
        }

        dashAudioSource.Play();
    }

    private void PlayJumpSound()
    {
        if (jumpAudioSource == null)
        {
            return;
        }

        jumpAudioSource.Play();
    }

    private bool IsSprintHeld()
    {
        if (inputActions != null && inputActions.Player.Sprint.IsPressed())
        {
            return true;
        }

        if (Keyboard.current != null &&
            (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
        {
            return true;
        }

        return Gamepad.current != null && Gamepad.current.rightShoulder.isPressed;
    }

    private bool IsSprintPressedThisFrame()
    {
        if (inputActions != null && inputActions.Player.Sprint.WasPressedThisFrame())
        {
            return true;
        }

        if (Keyboard.current != null &&
            (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame))
        {
            return true;
        }

        return Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame;
    }

    private float ResolveHorizontalInput()
    {
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // Keep analog/gamepad movement unchanged if keyboard is unavailable.
        if (Keyboard.current == null)
        {
            return moveInput.x;
        }

        bool leftPressed = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
        bool rightPressed = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;

        bool leftJustPressed = leftPressed && !previousLeftPressed;
        bool rightJustPressed = rightPressed && !previousRightPressed;

        if (leftJustPressed)
        {
            lastKeyboardHorizontalDirection = -1;
        }
        if (rightJustPressed)
        {
            lastKeyboardHorizontalDirection = 1;
        }

        previousLeftPressed = leftPressed;
        previousRightPressed = rightPressed;

        if (leftPressed && rightPressed)
        {
            // Last input wins while both are held.
            return lastKeyboardHorizontalDirection;
        }
        if (leftPressed)
        {
            return -1f;
        }
        if (rightPressed)
        {
            return 1f;
        }

        // No keyboard horizontal keys held: allow stick/other bindings.
        return moveInput.x;
    }
}
