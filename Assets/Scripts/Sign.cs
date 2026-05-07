using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class Sign : MonoBehaviour, IInteractable
{
    private InputSystem_Actions inputActions;
    [Header("Sign Content")]
    [SerializeField, TextArea(2, 6)] private string signText = "Put your sign text here.";
    [SerializeField, TextArea(2, 6)] private List<string> signTexts = new List<string>();
    [SerializeField] private bool interactOnce = false;

    [Header("Optional UI References")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private GameObject signPanel;
    [SerializeField] private GameObject signTextRoot;
    [SerializeField] private TMP_Text signTextLabel;

    [Header("Display")]
    [SerializeField, Min(0f)] private float autoHideAfterSeconds = 0f;

    public bool IsInteracted { get; private set; } = false;
    private bool playerInRange;
    private float hideAtTime = -1f;

    void Update()
    {
        if (!playerInRange)
        {
            return;
        }

        if (GameplayInputGate.BlocksGameplayActions)
        {
            return;
        }

        if (inputActions.Gameplay.Interact.WasPressedThisFrame() && CanInteract())
        {
            Interact();
        }

        if (hideAtTime > 0f && Time.time >= hideAtTime)
        {
            HideSignUI();
        }
    }

    public bool CanInteract()
    {
        if (!playerInRange)
        {
            return false;
        }

        if (interactOnce)
        {
            return !IsInteracted;
        }

        return true;
    }

    public void Interact()
    {
        if (!CanInteract()) return;

        IsInteracted = true;

        ApplySignText();

        if (signPanel != null)
        {
            signPanel.SetActive(true);
        }
        else
        {
            Debug.Log($"[Sign] {signText}", this);
        }

        if (autoHideAfterSeconds > 0f)
        {
            hideAtTime = Time.time + autoHideAfterSeconds;
        }
        else
        {
            hideAtTime = -1f;
        }

        UpdatePromptState();
    }

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        InputBindingOverrides.ApplySavedOverrides(inputActions.asset);
        InputBindingOverrides.RegisterRuntimeGameplayAsset(inputActions.asset);
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            InputBindingOverrides.UnregisterRuntimeGameplayAsset(inputActions.asset);
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

    private void ApplySignText()
    {
        if (signTextRoot != null)
        {
            TMP_Text[] textLabels = signTextRoot.GetComponentsInChildren<TMP_Text>(true);

            if (signTexts != null && signTexts.Count > 0)
            {
                for (int i = 0; i < textLabels.Length; i++)
                {
                    TMP_Text label = textLabels[i];
                    if (label == null)
                    {
                        continue;
                    }

                    if (i < signTexts.Count)
                    {
                        label.text = signTexts[i];
                    }
                }
            }

            return;
        }

        if (signTextLabel != null)
        {
            signTextLabel.text = signText;
        }
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
        HideSignUI();
        UpdatePromptState();
    }

    private void UpdatePromptState()
    {
        if (interactionPrompt == null)
        {
            return;
        }

        interactionPrompt.SetActive(CanInteract());
    }

    private void HideSignUI()
    {
        if (signPanel != null)
        {
            signPanel.SetActive(false);
        }

        hideAtTime = -1f;
    }

}
