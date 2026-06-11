using UnityEngine;

public class ShopInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private ShopUI shopUI;
    private InputSystem_Actions inputActions;
    private InteractPromptDisplay interactPromptDisplay;
    private bool playerInRange;

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

        if (inputActions != null && inputActions.Gameplay.Interact.WasPressedThisFrame() && CanInteract())
            Interact();
    }

    private void Awake()
    {
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

    public void Interact()
    {
        shopUI.Toggle();
        UpdatePromptState();
    }

    public bool CanInteract() => playerInRange;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        UpdatePromptState();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        UpdatePromptState();
        shopUI.Close();
    }

    private void UpdatePromptState()
    {
        if (interactionPrompt == null)
        {
            return;
        }

        bool shopOpen = shopUI != null && shopUI.IsOpen;
        bool showPrompt = playerInRange && !shopOpen;
        interactionPrompt.SetActive(showPrompt);

        if (showPrompt)
        {
            interactPromptDisplay ??= interactionPrompt.GetComponent<InteractPromptDisplay>();
            interactPromptDisplay?.Refresh();
        }
    }
}
