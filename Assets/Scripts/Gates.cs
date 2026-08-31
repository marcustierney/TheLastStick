using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
public class Gates : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private string destinationSceneName = "InsideDojo";
    [SerializeField] private string loadingScreenSceneName = "LoadingScreen";
    [SerializeField, Min(0f)] private float activationThreshold = 0.5f;

    private InputSystem_Actions inputActions;
    private bool playerInRange;
    private InteractPromptDisplay interactPromptDisplay;

    private void Awake()
    {
        BoxCollider2D gateCollider = GetComponent<BoxCollider2D>();
        if (gateCollider != null)
        {
            gateCollider.isTrigger = true;
        }

        inputActions = new InputSystem_Actions();
        InputBindingOverrides.ApplySavedOverrides(inputActions.asset);
        InputBindingOverrides.RegisterRuntimeGameplayAsset(inputActions.asset);

        if (interactionPrompt != null)
        {
            interactPromptDisplay = interactionPrompt.GetComponent<InteractPromptDisplay>();
        }
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

    private void Update()
    {
        UpdatePromptState();

        if (!playerInRange)
        {
            return;
        }

        if (GameplayInputGate.BlocksGameplayActions)
        {
            return;
        }

        if (inputActions == null || !CanInteract())
        {
            return;
        }

        Vector2 moveInput = inputActions.Gameplay.Move.ReadValue<Vector2>();
        if (moveInput.y >= activationThreshold)
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (!CanInteract())
        {
            return;
        }

        SceneTransition.SetPendingNextScene(destinationSceneName);
        SceneManager.LoadScene(loadingScreenSceneName);
    }

    public bool CanInteract()
    {
        return playerInRange;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = true;
        UpdatePromptState();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = false;
        UpdatePromptState();
    }

    private void UpdatePromptState()
    {
        if (interactionPrompt == null)
        {
            return;
        }

        bool showPrompt = playerInRange && !GameplayInputGate.BlocksGameplayActions;
        interactionPrompt.SetActive(showPrompt);

        if (showPrompt)
        {
            interactPromptDisplay ??= interactionPrompt.GetComponent<InteractPromptDisplay>();
            interactPromptDisplay?.Refresh();
        }
    }
}
