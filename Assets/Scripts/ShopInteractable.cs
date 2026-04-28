using UnityEngine;
using UnityEngine.InputSystem;

public class ShopInteractable : MonoBehaviour, IInteractable
{
    private InputSystem_Actions inputActions;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private ShopUI shopUI;

    private bool playerInRange;

    private void Update()
    {
        if (!playerInRange) return;
        if (inputActions.Player.Interact.WasPressedThisFrame() && CanInteract())
            Interact();
    }

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions?.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Player.Disable();
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
