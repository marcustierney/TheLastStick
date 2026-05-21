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
    private float baseSpeed = 5f;
    private float baseRunSpeed = 10f;
    private float jumpHeight = 15f;
    private bool isFacingRight = true;

    private float crouchSpeed = 2.5f; // Crouch walk speed (half of normal walk speed)

    private float dashTime = 0.2f; 
    private float dashSpeed = 20f;
    private float dashCooldown = 0.5f;
    private float dashTimeLeft;
    private bool isDashing;
    private bool jumpBufferedDuringDash;
    private bool dashEndedThisFrame;
    private float dashCooldownTimer = 0f; 
    private Collider2D playerCollider; 
    private List<Collider2D> ignoredEnemyColliders = new List<Collider2D>(); // Colliders ignored during dash
    private List<Collider2D> ignoredIFrameColliders = new List<Collider2D>(); // Colliders ignored during I-frames (e.g. after taking damage)
    private bool isJumping = false; 
    private bool shiftHold = false;
    private bool isCrouching = false;
    private bool isCrouchWalking = false;

    private bool canMoveHorizontally = true;
    private bool canJump = true;
    [SerializeField] private AudioSource groundedMoveAudioSource;
    [SerializeField] private float walkMovePitch = 1f;
    [SerializeField] private float runMovePitch = 1.3f;
    [SerializeField] private AudioSource dashAudioSource;
    [SerializeField] private AudioSource jumpAudioSource;
    private bool wasGroundedMoving = false;
    private bool wasGrounded = false;
    private float previousVelocityY;
    private CameraController cameraController;
    [SerializeField] private float dashSlowMoTimeScale = 0.55f;
    private readonly List<HorizontalControlBinding> horizontalControlBindings = new List<HorizontalControlBinding>();

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

    public bool CanJump
    {
        get
        {
            return canJump;
        }
        set
        {
            canJump = value;
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

    // Play the damage animation on the player's animator, if available.
    public void PlayDamageAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }
    }

    // Play the death animation on the player's animator and prepare animator to run while timeScale == 0
    public void PlayDeathAnimation()
    {
        // Prevent further horizontal input
        CanMoveHorizontally = false;

        // Stop movement-related sounds
        StopGroundedMoveSound();
        if (dashAudioSource != null && dashAudioSource.isPlaying)
            dashAudioSource.Stop();

        if (animator != null)
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.SetTrigger("Death");
        }
    }

    // Face the player toward the given source position (used when taking damage)
    public void FaceTowards(Vector2 sourcePosition)
    {
        bool shouldFaceRight = transform.position.x < sourcePosition.x;
        if (shouldFaceRight != isFacingRight)
        {
            isFacingRight = shouldFaceRight;
            Vector3 localScale = transform.localScale;
            float absX = Mathf.Abs(localScale.x);
            localScale.x = absX * (isFacingRight ? 1f : -1f);
            transform.localScale = localScale;
        }
    }


    [Header("Jump Forgiveness")]
    [SerializeField] private float coyoteTimeDuration = 0.12f;
    [SerializeField] private float jumpBufferDuration = 0.08f;
    [SerializeField] private float maxCoyoteRiseVelocity = 8f;

    [Header("Jump Corner Correction")]
    [SerializeField] private bool cornerCorrectionEnabled = true;
    [SerializeField] private float cornerProbeDistance = 0.1f;
    [SerializeField] private float cornerStepHorizontal = 0.03f;
    [SerializeField] private float cornerStepVertical = 0.03f;
    [SerializeField] private int cornerMaxStepsPerFrame = 2;

    [SerializeField] private float groundCheckHeight = 0.1f;
    [SerializeField] private float jumpGroundSeparation = 0.06f;

    private float coyoteTimeRemaining;
    private float jumpBufferRemaining;
    private bool hasLeftGroundSinceJump;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private List<string> dashIgnoreTags = new List<string> { "Enemy", "EnemyAttack", "Enemy Attack", "Boss", "BossTwo", "TutorialEnemy"}; // Tags of objects to ignore during dash 

    // Update is called once per frame
    void Update()
    {
        if (GameplayInputGate.BlocksGameplayActions)
        {
            horizontal = 0f;
            isWalking = false;
            shiftHold = false;
            return;
        }

        dashEndedThisFrame = false;

        // Update dash timers even if knockback is active
        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0f)
            {
                isDashing = false;
                dashEndedThisFrame = true;
                EndDashIgnoreCollisions();
                rb.gravityScale = 5f;
                cameraController?.OnDashEnded();
                GameFeelTimeScale.Instance?.CancelSlowMo();
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
        previousVelocityY = rb != null ? rb.linearVelocity.y : 0f;
        areGrounded = Grounded();
        if (dashEndedThisFrame)
        {
            if (areGrounded && jumpBufferedDuringDash)
                jumpBufferRemaining = 0f;
            jumpBufferedDuringDash = false;
        }
        if (!areGrounded)
            hasLeftGroundSinceJump = true;
        if (!wasGrounded && areGrounded && cameraController != null)
        {
            cameraController.OnPlayerLanded(Mathf.Abs(previousVelocityY));
        }
        wasGrounded = areGrounded;
        if (animator != null)
        {
            animator.SetBool("isDashing", isDashing);
            animator.SetBool("isWalking", isWalking);
            animator.SetBool("areGrounded", areGrounded);
            animator.SetBool("hasLeftGroundSinceJump", hasLeftGroundSinceJump);
            animator.SetBool("shiftHold", shiftHold);
            animator.SetBool("isCrouching", isCrouching);
            animator.SetBool("isCrouchWalking", isCrouchWalking);
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
            if (areGrounded)
                jumpBufferRemaining = 0f;
            cameraController?.OnDashStarted();
            GameFeelTimeScale.Instance?.RequestSlowMo(dashSlowMoTimeScale, dashTime);
        }

        RefreshJumpForgivenessTimers();
        TryExecuteJump();

        // Handle crouch input strictly through the remappable action.
        bool crouchPressed = inputActions.Gameplay.Crouch.IsPressed();

        if (CanMoveHorizontally && crouchPressed && areGrounded)
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = false;
        }

        // Determine if crouch walking
        isCrouchWalking = isCrouching && Mathf.Abs(horizontal) > 0.01f;

        Flip();
    }

        

    private void FixedUpdate()
    {
        if (!GameplayInputGate.BlocksGameplayActions)
            TryJumpCornerCorrection();

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
            float moveSpeed;
            if (isCrouchWalking)
            {
                moveSpeed = crouchSpeed;
            }
            else
            {
                moveSpeed = shiftHold ? runSpeed : speed;
            }
            rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
        }
    }

    // initialize components 
    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        InputBindingOverrides.ApplySavedOverrides(inputActions.asset);
        InputBindingOverrides.RegisterRuntimeGameplayAsset(inputActions.asset);
        CacheHorizontalControlBindings();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();
        if (playerCollider == null)
            playerCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        baseSpeed = speed;
        baseRunSpeed = runSpeed;

        if (Camera.main != null)
        {
            cameraController = Camera.main.GetComponent<CameraController>();
        }

        wasGrounded = Grounded();
        if (wasGrounded)
            coyoteTimeRemaining = coyoteTimeDuration;
    }

    private void OnEnable()
    {
        inputActions?.Gameplay.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Gameplay.Disable();
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            InputBindingOverrides.UnregisterRuntimeGameplayAsset(inputActions.asset);
        }
    }

    // Apply a total speed bonus measured in percent points from the base values.
    public void ApplySpeedPercent(float percent)
    {
        float multiplier = 1f + (percent / 100f);
        speed = baseSpeed * multiplier;
        runSpeed = baseRunSpeed * multiplier;
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

    private void RefreshJumpForgivenessTimers()
    {
        if (areGrounded)
            coyoteTimeRemaining = coyoteTimeDuration;
        else
            coyoteTimeRemaining -= Time.deltaTime;

        if (inputActions.Gameplay.Jump.WasPressedThisFrame())
        {
            jumpBufferRemaining = jumpBufferDuration;
            if (isDashing)
                jumpBufferedDuringDash = true;
        }
        else if (jumpBufferRemaining > 0f)
            jumpBufferRemaining -= Time.deltaTime;
    }

    private bool CanPerformJump()
    {
        if (!CanMoveHorizontally || isDashing || !CanJump || isJumping || knockbackTimer > 0f)
            return false;

        if (jumpBufferRemaining <= 0f)
            return false;

        bool coyoteValid = coyoteTimeRemaining > 0f
            && rb != null
            && rb.linearVelocity.y <= maxCoyoteRiseVelocity;

        return areGrounded || coyoteValid;
    }

    private void TryExecuteJump()
    {
        if (!CanPerformJump())
            return;

        spacebarPressed = true;
        jumpBufferRemaining = 0f;
        coyoteTimeRemaining = 0f;
        hasLeftGroundSinceJump = false;
        jumpBufferedDuringDash = false;
        StartCoroutine(JumpWithDelay());
    }

    private void TryJumpCornerCorrection()
    {
        if (!cornerCorrectionEnabled || playerCollider == null || Grounded() || isDashing || knockbackTimer > 0f)
            return;

        for (int step = 0; step < cornerMaxStepsPerFrame; step++)
        {
            if (!IsCornerClipBlocked(out bool blockedAbove, out bool blockedSide))
                break;

            if (!TryApplyCornerNudge(blockedAbove, blockedSide))
                break;
        }
    }

    private bool IsCornerClipBlocked(out bool blockedAbove, out bool blockedSide)
    {
        blockedAbove = false;
        blockedSide = false;

        Bounds bounds = playerCollider.bounds;
        float probe = cornerProbeDistance;
        float sign = GetCornerNudgeSign();

        Vector2 topLeft = new Vector2(bounds.min.x + 0.02f, bounds.max.y);
        Vector2 topRight = new Vector2(bounds.max.x - 0.02f, bounds.max.y);
        blockedAbove = RaycastHitsGround(topLeft, Vector2.up, probe)
            || RaycastHitsGround(topRight, Vector2.up, probe);

        float midY = (bounds.min.y + bounds.max.y) * 0.5f;
        float footY = bounds.min.y + bounds.size.y * 0.2f;
        Vector2 midOrigin = new Vector2(sign > 0f ? bounds.max.x : bounds.min.x, midY);
        Vector2 footOrigin = new Vector2(sign > 0f ? bounds.max.x : bounds.min.x, footY);
        Vector2 sideDirection = sign > 0f ? Vector2.right : Vector2.left;
        blockedSide = RaycastHitsGround(midOrigin, sideDirection, probe)
            || RaycastHitsGround(footOrigin, sideDirection, probe);

        return blockedAbove || blockedSide;
    }

    private bool TryApplyCornerNudge(bool blockedAbove, bool blockedSide)
    {
        if (!blockedAbove && !blockedSide)
            return false;

        float sign = GetCornerNudgeSign();
        Vector2[] candidates =
        {
            new Vector2(sign * cornerStepHorizontal, 0f),
            new Vector2(-sign * cornerStepHorizontal, 0f),
            new Vector2(0f, cornerStepVertical),
            new Vector2(sign * cornerStepHorizontal, cornerStepVertical),
            new Vector2(-sign * cornerStepHorizontal, cornerStepVertical),
        };

        foreach (Vector2 offset in candidates)
        {
            if (offset.sqrMagnitude > cornerProbeDistance * cornerProbeDistance)
                continue;

            if (!IsCornerOffsetClear(offset))
                continue;

            bool horizontalPrimary = Mathf.Abs(offset.x) > 0.001f && Mathf.Abs(offset.y) < 0.001f;
            if (horizontalPrimary && !HasLedgeSupportAfterOffset(offset))
                continue;

            transform.position += (Vector3)offset;
            return true;
        }

        return false;
    }

    private float GetCornerNudgeSign()
    {
        if (Mathf.Abs(horizontal) > 0.01f)
            return Mathf.Sign(horizontal);

        return isFacingRight ? 1f : -1f;
    }

    private bool RaycastHitsGround(Vector2 origin, Vector2 direction, float distance)
    {
        return Physics2D.Raycast(origin, direction, distance, groundLayer).collider != null;
    }

    private bool IsCornerOffsetClear(Vector2 offset)
    {
        Bounds bounds = playerCollider.bounds;
        Vector2 center = (Vector2)bounds.center + offset;
        Vector2 size = bounds.size * 0.9f;
        return Physics2D.OverlapBox(center, size, 0f, groundLayer) == null;
    }

    private bool HasLedgeSupportAfterOffset(Vector2 offset)
    {
        Bounds bounds = playerCollider.bounds;
        Vector2 footProbe = new Vector2(bounds.center.x + offset.x, bounds.min.y + offset.y + 0.02f);
        return Physics2D.Raycast(footProbe, Vector2.down, cornerProbeDistance, groundLayer).collider != null;
    }

    private float GetPlayerColliderWidth()
    {
        Collider2D col = playerCollider != null ? playerCollider : GetComponent<Collider2D>();
        if (col == null)
            return 0.4f;

        return col.bounds.size.x;
    }

    private void GetGroundCheckBox(out Vector2 center, out Vector2 size)
    {
        center = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        size = new Vector2(GetPlayerColliderWidth(), groundCheckHeight);
    }

    private bool Grounded()
    {
        GetGroundCheckBox(out Vector2 center, out Vector2 size);
        return Physics2D.OverlapBox(center, size, 0f, groundLayer);
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
        return Grounded();
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
        ApplyJumpGroundSeparation();
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpHeight);
        PlayJumpSound();
        isJumping = false;
    }

    private void ApplyJumpGroundSeparation()
    {
        if (!Grounded() || jumpGroundSeparation <= 0f)
            return;

        transform.position += Vector3.up * jumpGroundSeparation;
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
        return inputActions != null && inputActions.Gameplay.Sprint.IsPressed();
    }

    private bool IsSprintPressedThisFrame()
    {
        return inputActions != null && inputActions.Gameplay.Sprint.WasPressedThisFrame();
    }

    private float ResolveHorizontalInput()
    {
        InputAction moveAction = inputActions.Gameplay.Move;
        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        // Rebuild from effective controls every frame so runtime rebind/apply
        // updates are reflected immediately (no stale cached controls).
        CacheHorizontalControlBindings();

        bool leftPressed = false;
        bool rightPressed = false;
        float latestLeftPressTime = float.NegativeInfinity;
        float latestRightPressTime = float.NegativeInfinity;

        float now = Time.unscaledTime;
        foreach (HorizontalControlBinding binding in horizontalControlBindings)
        {
            bool isPressed = binding.control != null && binding.control.IsPressed();
            if (isPressed && !binding.wasPressedLastFrame)
            {
                binding.lastPressedTime = now;
            }

            binding.wasPressedLastFrame = isPressed;

            if (!isPressed)
            {
                continue;
            }

            if (binding.direction < 0f)
            {
                leftPressed = true;
                if (binding.lastPressedTime > latestLeftPressTime)
                    latestLeftPressTime = binding.lastPressedTime;
            }
            else
            {
                rightPressed = true;
                if (binding.lastPressedTime > latestRightPressTime)
                    latestRightPressTime = binding.lastPressedTime;
            }
        }

        if (leftPressed && rightPressed)
        {
            // Conflict case: when opposite directions are held, the most recently pressed direction wins.
            return latestRightPressTime > latestLeftPressTime ? 1f : -1f;
        }

        if (leftPressed)
            return -1f;
        if (rightPressed)
            return 1f;

        // Fall back to analog movement value when no digital left/right bindings are pressed.
        return moveInput.x;
    }

    private void CacheHorizontalControlBindings()
    {
        horizontalControlBindings.Clear();
        InputAction moveAction = inputActions?.Gameplay.Move;
        if (moveAction == null)
        {
            return;
        }

        foreach (InputControl control in moveAction.controls)
        {
            if (control == null)
            {
                continue;
            }

            int bindingIndex = moveAction.GetBindingIndexForControl(control);
            if (bindingIndex < 0 || bindingIndex >= moveAction.bindings.Count)
            {
                continue;
            }

            InputBinding binding = moveAction.bindings[bindingIndex];
            if (!binding.isPartOfComposite)
            {
                continue;
            }

            float direction = 0f;
            if (binding.name == "left")
                direction = -1f;
            else if (binding.name == "right")
                direction = 1f;
            else
                continue;

            horizontalControlBindings.Add(new HorizontalControlBinding
            {
                control = control,
                direction = direction,
                lastPressedTime = float.NegativeInfinity,
                wasPressedLastFrame = false
            });
        }
    }

    private class HorizontalControlBinding
    {
        public InputControl control;
        public float direction;
        public float lastPressedTime;
        public bool wasPressedLastFrame;
    }
}
