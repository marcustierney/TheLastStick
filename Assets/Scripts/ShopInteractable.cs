using UnityEngine;

public class ShopInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private ShopUI shopUI;
    private InputSystem_Actions inputActions;
    private bool playerInRange;

    private void Update()
    {
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
    }

    public bool CanInteract() => playerInRange;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (interactionPrompt != null) interactionPrompt.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        shopUI.Close();
    }
}
