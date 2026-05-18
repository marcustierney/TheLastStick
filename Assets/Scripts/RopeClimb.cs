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

    private InputSystem_Actions inputActions;
    private bool playerInRange = false;
    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private Movement playerMovement;
    private float originalGravity = 1f;
    private bool isClimbing = false;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        InputBindingOverrides.ApplySavedOverrides(inputActions.asset);
        InputBindingOverrides.RegisterRuntimeGameplayAsset(inputActions.asset);
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
        UpdatePromptState();

        if (GameplayInputGate.BlocksGameplayActions)
            return;

        if (playerInRange && inputActions != null && inputActions.Gameplay.Interact.WasPressedThisFrame() && CanInteract())
        {
            Interact();
        }

        if (isClimbing && playerTransform != null && playerRb != null)
        {
            Vector2 move = inputActions.Gameplay.Move.ReadValue<Vector2>();
            float vy = move.y;

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

    private void StartClimbing()
    {
        if (playerTransform == null)
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
        playerTransform = null;
        playerRb = null;
        playerMovement = null;
    }

    private void UpdatePromptState()
    {
        if (interactionPrompt == null)
            return;

        bool show = playerInRange && !isClimbing;
        interactionPrompt.SetActive(show);

        if (show && playerTransform != null)
        {
            Vector3 pos = interactionPrompt.transform.position;
            interactionPrompt.transform.position = new Vector3(transform.position.x + promptOffsetX, playerTransform.position.y, pos.z);
        }
    }
}
