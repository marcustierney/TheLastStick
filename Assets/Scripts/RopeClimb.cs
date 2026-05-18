using UnityEngine;
using UnityEngine.InputSystem;

// Attach to a vertical rope collider (BoxCollider2D set as Trigger).
// Provide a world-space `interactionPrompt` GameObject (e.g. small sprite or world-space Canvas)
// The prompt will be positioned next to the rope at the player's Y coordinate while in range.
public class RopeClimb : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private float climbSpeed = 3f;
    [SerializeField] private float dismountJumpVelocity = 15f;
    [SerializeField] private float promptOffsetX = 0.5f;
    [SerializeField] private float promptYOffset = 0.5f;
    [SerializeField] private float promptSmooth = 12f;

    private InputSystem_Actions inputActions;
    private bool playerInRange = false;
    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private Movement playerMovement;
    private float originalGravity = 1f;
    private bool isClimbing = false;
    private Collider2D ropeCollider;
    [SerializeField] private float promptVerticalMargin = 0.2f;
    private float dismountCooldownTimer = 0f;
    [SerializeField] private float dismountCooldown = 0.3f;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        InputBindingOverrides.ApplySavedOverrides(inputActions.asset);
        InputBindingOverrides.RegisterRuntimeGameplayAsset(inputActions.asset);
    }

    void Start()
    {
        ropeCollider = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        inputActions?.Gameplay.Enable();
    }

    void OnDisable()
    {
        inputActions?.Gameplay.Disable();
    }

    void OnDestroy()
    {
        if (inputActions != null)
            InputBindingOverrides.UnregisterRuntimeGameplayAsset(inputActions.asset);
    }

    void Update()
    {
        // Decrement dismount cooldown
        if (dismountCooldownTimer > 0f)
            dismountCooldownTimer -= Time.deltaTime;

        UpdatePromptState();

        if (GameplayInputGate.BlocksGameplayActions)
            return;

        if (playerInRange && inputActions != null && inputActions.Gameplay.Interact.WasPressedThisFrame() && CanInteract())
        {
            Interact();
        }

        // Block Jump only when in range, not climbing, and in the air
        if (playerMovement != null)
        {
            bool isGrounded = playerMovement.IsGrounded();
            playerMovement.CanJump = isClimbing || !playerInRange || isGrounded;
        }

        if (isClimbing && playerTransform != null && playerRb != null)
        {
            Vector2 move = inputActions.Gameplay.Move.ReadValue<Vector2>();
            float vy = move.y;

            // Fallback for digital down input (S or down-arrow) which may be bound to Crouch
            if (Mathf.Abs(vy) < 0.01f)
            {
                if (inputActions.Gameplay.Crouch.IsPressed())
                    vy = -1f;
                else if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed)
                    vy = -1f;
            }

            // Lock player to rope X position and apply vertical velocity
            Vector3 p = playerTransform.position;
            playerTransform.position = new Vector3(transform.position.x, p.y, p.z);
            playerRb.linearVelocity = new Vector2(0f, vy * climbSpeed);

            // Jump to dismount with upward velocity
            if (inputActions.Gameplay.Jump.WasPressedThisFrame())
            {
                if (playerMovement != null)
                {
                    playerMovement.SwordJump();
                }
                else if (playerRb != null)
                {
                    playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, dismountJumpVelocity);
                }

                StopClimbing();
            }
        }
    }

    public void Interact()
    {
        if (!isClimbing)
            StartClimbing();
        else
            StopClimbing();
    }

    public bool CanInteract() => playerInRange;

    public bool CanStartClimbing() => playerInRange && dismountCooldownTimer <= 0f;

    private void StartClimbing()
    {
        if (playerTransform == null || !CanStartClimbing())
            return;

        isClimbing = true;
        if (playerMovement != null)
            playerMovement.CanMoveHorizontally = false;

        if (playerRb != null)
        {
            originalGravity = playerRb.gravityScale;
            playerRb.gravityScale = 0f;
            playerRb.linearVelocity = Vector2.zero;
        }

        UpdatePromptState();
    }

    private void StopClimbing()
    {
        isClimbing = false;
        dismountCooldownTimer = dismountCooldown;
        if (playerMovement != null)
            playerMovement.CanMoveHorizontally = true;

        if (playerRb != null)
            playerRb.gravityScale = originalGravity;

        UpdatePromptState();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
        playerTransform = other.transform;
        playerRb = other.GetComponent<Rigidbody2D>();
        playerMovement = other.GetComponent<Movement>();
        UpdatePromptState();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;
        UpdatePromptState();
        if (isClimbing)
            StopClimbing();
        if (playerMovement != null)
            playerMovement.CanJump = true;
        playerTransform = null;
        playerRb = null;
        playerMovement = null;
    }

    private void UpdatePromptState()
    {
        if (interactionPrompt == null)
            return;

        bool withinVerticalRange = true;
        if (ropeCollider != null && playerTransform != null)
        {
            float minY = ropeCollider.bounds.min.y - promptVerticalMargin;
            float maxY = ropeCollider.bounds.max.y + promptVerticalMargin;
            withinVerticalRange = playerTransform.position.y >= minY && playerTransform.position.y <= maxY;
        }

        bool show = playerInRange && !isClimbing && withinVerticalRange;

        // If we are about to show the prompt and it was previously inactive, snap it to the target
        bool wasActive = interactionPrompt.activeSelf;
        if (show)
        {
            Vector3 pos = interactionPrompt.transform.position;
            float targetY = (playerTransform != null) ? (playerTransform.position.y + promptYOffset) : pos.y;

            if (!wasActive)
            {
                // snap immediately to avoid visible falling animation on first show
                interactionPrompt.transform.position = new Vector3(transform.position.x + promptOffsetX, targetY, pos.z);
            }
            else
            {
                float smoothY = Mathf.Lerp(pos.y, targetY, Time.deltaTime * promptSmooth);
                interactionPrompt.transform.position = new Vector3(transform.position.x + promptOffsetX, smoothY, pos.z);
            }

            interactionPrompt.SetActive(true);
        }
        else
        {
            interactionPrompt.SetActive(false);
        }
    }
}
