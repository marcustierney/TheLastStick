using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShopInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private ShopUI shopUI;
    private bool playerInRange;

    private void Update()
    {
        if (!playerInRange) return;
        if (IsInteractPressed() && CanInteract())
            Interact();
    }

    private bool IsInteractPressed()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        if (!Enum.TryParse(interactKey.ToString(), true, out Key key))
        {
            return false;
        }

        return Keyboard.current[key].wasPressedThisFrame;
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
