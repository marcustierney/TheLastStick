using System;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(50)]
public class Sign : MonoBehaviour, IInteractable
{
    private InputAction cachedPlayerInteractAction;
    [SerializeField] private bool interactOnce = false;

    [Header("Optional UI References")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private SpriteRenderer interactionPromptGamepadIcon;
    [SerializeField] private SpriteRenderer interactionPromptKeyboardIcon;
    [SerializeField] private GameObject signPanel;

    [Header("Interact prompt sprites")]
    [SerializeField] private Sprite gamepadNorthSprite;
    [SerializeField] private Sprite gamepadEastSprite;
    [SerializeField] private Sprite gamepadSouthSprite;
    [SerializeField] private Sprite gamepadWestSprite;

    [Header("Display")]
    [SerializeField, Min(0f)] private float autoHideAfterSeconds = 0f;

    public bool IsInteracted { get; private set; } = false;
    private bool playerInRange;
    private float hideAtTime = -1f;
    private TMP_Text interactionPromptGlyphLabel;
    private UIFocusGuard uiFocusGuard;
    private PlayerInput playerInput;

    private const string GamepadSchemeName = "Gamepad";

    private string lastInteractBindingDebug;
    private string lastResolvedInteractActionDebug;

    // FIX: Also checks overridePath explicitly so remapped bindings are found
    // even if effectivePath hasn't refreshed on a stale asset copy.
    static bool TryGetFirstGameplayInteractBinding(InputAction interactAction,
        string devicePrefix,
        out int bindingIndex,
        out string effectivePath)
    {
        bindingIndex = -1;
        effectivePath = null;
        int count = interactAction.bindings.Count;
        for (int i = 0; i < count; i++)
        {
            InputBinding b = interactAction.bindings[i];
            if (b.isPartOfComposite)
            {
                continue;
            }

            // effectivePath returns overridePath ?? path, but we read it
            // directly here to stay robust against stale asset copies.
            string candidate = !string.IsNullOrEmpty(b.overridePath) ? b.overridePath : b.path;
            if (string.IsNullOrEmpty(candidate))
            {
                continue;
            }

            if (candidate.StartsWith(devicePrefix, StringComparison.OrdinalIgnoreCase))
            {
                bindingIndex = i;
                effectivePath = candidate;
                return true;
            }
        }

        return false;
    }

    static Sprite MapGamepadInteractPathToFaceSprite(string path,
        Sprite north,
        Sprite east,
        Sprite south,
        Sprite west)
    {
        StringComparison ordinalIgnoreCase = StringComparison.OrdinalIgnoreCase;
        if (string.IsNullOrEmpty(path) || !path.StartsWith("<Gamepad>/", ordinalIgnoreCase))
        {
            return null;
        }

        if (path.IndexOf("/buttonNorth", ordinalIgnoreCase) >= 0)
        {
            return north;
        }

        if (path.IndexOf("/buttonEast", ordinalIgnoreCase) >= 0)
        {
            return east;
        }

        if (path.IndexOf("/buttonSouth", ordinalIgnoreCase) >= 0)
        {
            return south;
        }

        if (path.IndexOf("/buttonWest", ordinalIgnoreCase) >= 0)
        {
            return west;
        }

        return null;
    }

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

        InputAction interactAction = ResolveInteractAction();
        if (interactAction != null && interactAction.WasPressedThisFrame() && CanInteract())
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

        if (signPanel != null)
        {
            signPanel.SetActive(true);
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
        CacheInteractionPromptVisualReferences();

        uiFocusGuard = FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
        playerInput = FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Include);
        CachePlayerInteractAction();
    }

    void CacheInteractionPromptVisualReferences()
    {
        if (interactionPrompt == null)
        {
            return;
        }

        if (interactionPromptGlyphLabel == null)
        {
            interactionPromptGlyphLabel = interactionPrompt.GetComponentInChildren<TMP_Text>(true);
        }

        if (interactionPromptGamepadIcon == null)
        {
            Transform gamepadIconTransform = interactionPrompt.transform.Find("Icon");
            if (gamepadIconTransform != null)
            {
                interactionPromptGamepadIcon = gamepadIconTransform.GetComponent<SpriteRenderer>();
            }
        }

        if (interactionPromptKeyboardIcon == null)
        {
            Transform keyboardIconTransform = interactionPrompt.transform.Find("KeyboardIcon");
            if (keyboardIconTransform != null)
            {
                interactionPromptKeyboardIcon = keyboardIconTransform.GetComponent<SpriteRenderer>();
            }
        }
    }

    private void LateUpdate()
    {
        RefreshInteractionPromptGlyphIfNeeded();
    }

    private void RefreshInteractionPromptGlyphIfNeeded()
    {
        if (interactionPrompt == null || !interactionPrompt.activeSelf)
        {
            return;
        }

        CacheInteractionPromptVisualReferences();

        InputAction interactAction = ResolveInteractAction();
        if (interactAction == null)
        {
            return;
        }
        TMP_Text overlayText = interactionPromptGlyphLabel;
        SpriteRenderer gamepadIcon = interactionPromptGamepadIcon;
        SpriteRenderer keyboardIcon = interactionPromptKeyboardIcon;

        bool schemeIsGamepad = ShouldShowGamepadInteractGlyph();

        if (!schemeIsGamepad)
        {
            bool hasKeyboardBinding =
                TryGetFirstGameplayInteractBinding(interactAction, "<Keyboard>/", out int keyboardIndex, out string keyboardPath);
            bool hasMouseBinding =
                TryGetFirstGameplayInteractBinding(interactAction, "<Mouse>/", out int mouseIndex, out string mousePath);

            InputBindingDisplayInfo keyboardDisplayInfo = hasKeyboardBinding
                ? InputBindingDisplayHelper.Build(interactAction, keyboardIndex)
                : new InputBindingDisplayInfo(null, "<none>", "<none>");
            InputBindingDisplayInfo mouseDisplayInfo = hasMouseBinding
                ? InputBindingDisplayHelper.Build(interactAction, mouseIndex)
                : new InputBindingDisplayInfo(null, "<none>", "<none>");
            string keyboardMouseDebug =
                $"[Sign] Interact Keyboard/Mouse | Keyboard: found={hasKeyboardBinding}, idx={keyboardIndex}, path={keyboardDisplayInfo.FullPath ?? keyboardPath ?? "<null>"}, display={keyboardDisplayInfo.Display}, alias={keyboardDisplayInfo.Alias} | Mouse: found={hasMouseBinding}, idx={mouseIndex}, path={mouseDisplayInfo.FullPath ?? mousePath ?? "<null>"}, display={mouseDisplayInfo.Display}, alias={mouseDisplayInfo.Alias}";
            if (!string.Equals(lastInteractBindingDebug, keyboardMouseDebug, StringComparison.Ordinal))
            {
                lastInteractBindingDebug = keyboardMouseDebug;
                Debug.Log(keyboardMouseDebug, this);
            }

            if (gamepadIcon != null)
            {
                SetSpriteVisualActive(gamepadIcon, false);
            }

            if (hasKeyboardBinding)
            {
                // FIX: keyboard icon shows for all keys; only suppressed for LMB/RMB.
                bool useTextOnlyFallback = IsTextOnlyInteractFallbackBinding(keyboardPath);
                if (keyboardIcon != null)
                {
                    SetSpriteVisualActive(keyboardIcon, !useTextOnlyFallback);
                }

                if (overlayText != null)
                {
                    SetTextVisualActive(overlayText, true);
                    overlayText.text = keyboardDisplayInfo.Alias;
                }

                return;
            }

            if (keyboardIcon != null)
            {
                SetSpriteVisualActive(keyboardIcon, false);
            }

            if (overlayText != null)
            {
                SetTextVisualActive(overlayText, true);
                if (hasMouseBinding)
                {
                    // FIX: only suppress keyboard icon for actual mouse buttons (LMB/RMB).
                    bool useTextOnlyFallback = IsTextOnlyInteractFallbackBinding(mousePath);
                    if (keyboardIcon != null)
                    {
                        SetSpriteVisualActive(keyboardIcon, !useTextOnlyFallback);
                    }

                    overlayText.text = mouseDisplayInfo.Alias;
                }
                else
                {
                    overlayText.text = string.Empty;
                }
            }

            return;
        }

        if (!TryGetFirstGameplayInteractBinding(interactAction, "<Gamepad>/", out int gamepadBindIndex,
                out string gamepadPath))
        {
            LogSimpleGamepadFailure(interactAction);
            InputBindingDisplayInfo fallbackInfo = InputBindingDisplayHelper.BuildFromDisplay(interactAction.GetBindingDisplayString());
            ShowInteractPromptTextOnly(gamepadIcon, keyboardIcon, overlayText, fallbackInfo.Alias);
            return;
        }

        InputBindingDisplayInfo gamepadDisplayInfo = InputBindingDisplayHelper.Build(interactAction, gamepadBindIndex);
        string gamepadDebug =
            $"[Sign] Interact Gamepad | found=true, idx={gamepadBindIndex}, path={gamepadDisplayInfo.FullPath ?? gamepadPath ?? "<null>"}, display={gamepadDisplayInfo.Display}, alias={gamepadDisplayInfo.Alias}";
        if (!string.Equals(lastInteractBindingDebug, gamepadDebug, StringComparison.Ordinal))
        {
            lastInteractBindingDebug = gamepadDebug;
            Debug.Log(gamepadDebug, this);
        }

        Sprite faceSprite =
            MapGamepadInteractPathToFaceSprite(gamepadPath, gamepadNorthSprite, gamepadEastSprite, gamepadSouthSprite,
                gamepadWestSprite);
        bool useFaceGraphic = gamepadIcon != null && faceSprite != null;

        if (useFaceGraphic)
        {
            SetSpriteVisualActive(gamepadIcon, true);
            gamepadIcon.sprite = faceSprite;

            if (keyboardIcon != null)
            {
                SetSpriteVisualActive(keyboardIcon, false);
            }

            if (overlayText != null)
            {
                SetTextVisualActive(overlayText, false);
            }

            return;
        }

        ShowInteractPromptTextOnly(gamepadIcon, keyboardIcon, overlayText, gamepadDisplayInfo.Alias);
    }

    static void ShowInteractPromptTextOnly(SpriteRenderer gamepadIcon, SpriteRenderer keyboardIcon, TMP_Text overlayText, string text)
    {
        SetSpriteVisualActive(gamepadIcon, false);
        SetSpriteVisualActive(keyboardIcon, false);

        if (overlayText != null)
        {
            SetTextVisualActive(overlayText, true);
            overlayText.text = text;
        }
    }

    static void SetSpriteVisualActive(SpriteRenderer spriteRenderer, bool active)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (spriteRenderer.gameObject.activeSelf != active)
        {
            spriteRenderer.gameObject.SetActive(active);
        }

        spriteRenderer.enabled = active;
    }

    static void SetTextVisualActive(TMP_Text textLabel, bool active)
    {
        if (textLabel == null)
        {
            return;
        }

        if (textLabel.gameObject.activeSelf != active)
        {
            textLabel.gameObject.SetActive(active);
        }

        textLabel.enabled = active;
    }

    // FIX: Space removed — keyboard icon now shows for all keys.
    // Only LMB (/leftButton) and RMB (/rightButton) suppress the icon.
    static bool IsTextOnlyInteractFallbackBinding(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        StringComparison ordinalIgnoreCase = StringComparison.OrdinalIgnoreCase;
        return path.IndexOf("/leftButton", ordinalIgnoreCase) >= 0
               || path.IndexOf("/rightButton", ordinalIgnoreCase) >= 0;
    }

    private void LogSimpleGamepadFailure(InputAction interactAction)
    {
        bool hasAnyGamepadPath = false;
        int bindingCount = interactAction.bindings.Count;
        for (int i = 0; i < bindingCount; i++)
        {
            InputBinding b = interactAction.bindings[i];
            string candidate = !string.IsNullOrEmpty(b.overridePath) ? b.overridePath : b.path;
            if (!string.IsNullOrEmpty(candidate) && candidate.StartsWith("<Gamepad>/", StringComparison.OrdinalIgnoreCase))
            {
                hasAnyGamepadPath = true;
                break;
            }
        }

        string scheme = playerInput != null ? playerInput.currentControlScheme : "<no PlayerInput>";
        InputBindingDisplayInfo fallbackDisplay = InputBindingDisplayHelper.BuildFromDisplay(interactAction.GetBindingDisplayString());
        string failureLog =
            $"[Sign] Interact Gamepad | found=false, scheme={scheme}, bindings={bindingCount}, hasAnyGamepadPath={hasAnyGamepadPath}, defaultDisplay={fallbackDisplay.Display}, defaultAlias={fallbackDisplay.Alias}";
        if (!string.Equals(lastInteractBindingDebug, failureLog, StringComparison.Ordinal))
        {
            lastInteractBindingDebug = failureLog;
            Debug.LogWarning(failureLog, this);
        }
    }

    private bool ShouldShowGamepadInteractGlyph()
    {
        if (playerInput == null)
        {
            playerInput = FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Include);
            CachePlayerInteractAction();
        }

        if (playerInput != null && !string.IsNullOrEmpty(playerInput.currentControlScheme))
        {
            return string.Equals(playerInput.currentControlScheme, GamepadSchemeName, StringComparison.Ordinal);
        }

        return uiFocusGuard != null && uiFocusGuard.IsGamepadInputActive;
    }

    private void CachePlayerInteractAction()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            cachedPlayerInteractAction = null;
            return;
        }

        cachedPlayerInteractAction = playerInput.actions.FindAction("Gameplay/Interact", throwIfNotFound: false)
            ?? playerInput.actions.FindAction("Interact", throwIfNotFound: false);
    }

    private InputAction ResolveInteractAction()
    {
        if (playerInput == null)
        {
            playerInput = FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Include);
            CachePlayerInteractAction();
        }

        InputAction resolvedAction = playerInput != null && playerInput.actions != null
            ? (playerInput.actions.FindAction("Gameplay/Interact", throwIfNotFound: false)
               ?? playerInput.actions.FindAction("Interact", throwIfNotFound: false))
            : null;

        // Keep cached reference aligned with the current PlayerInput asset.
        if (resolvedAction != cachedPlayerInteractAction)
        {
            cachedPlayerInteractAction = resolvedAction;
        }

        if (cachedPlayerInteractAction == null)
        {
            CachePlayerInteractAction();
        }

        LogResolvedInteractActionDebug(cachedPlayerInteractAction);
        return cachedPlayerInteractAction;
    }

    private void LogResolvedInteractActionDebug(InputAction interactAction)
    {
        string playerInputName = playerInput != null ? playerInput.gameObject.name : "<null>";
        string actionName = interactAction != null ? interactAction.name : "<null>";
        string mapName = interactAction != null && interactAction.actionMap != null
            ? interactAction.actionMap.name
            : "<null>";
        int bindingCount = interactAction != null ? interactAction.bindings.Count : 0;
        string paths = "<none>";

        if (interactAction != null && bindingCount > 0)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < bindingCount; i++)
            {
                InputBinding b = interactAction.bindings[i];
                string effective = !string.IsNullOrEmpty(b.effectivePath)
                    ? b.effectivePath
                    : (!string.IsNullOrEmpty(b.overridePath) ? b.overridePath : b.path);
                if (i > 0)
                {
                    sb.Append(" | ");
                }
                sb.Append('[').Append(i).Append("]=").Append(string.IsNullOrEmpty(effective) ? "<null>" : effective);
            }

            paths = sb.ToString();
        }

        string message =
            $"[Sign] ResolveInteractAction | playerInput={playerInputName}, map={mapName}, action={actionName}, bindings={bindingCount}, paths={paths}";

        if (!string.Equals(lastResolvedInteractActionDebug, message, StringComparison.Ordinal))
        {
            lastResolvedInteractActionDebug = message;
            Debug.Log(message, this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerInput triggerPlayerInput = other.GetComponentInParent<PlayerInput>();
        if (triggerPlayerInput != null && triggerPlayerInput != playerInput)
        {
            playerInput = triggerPlayerInput;
            CachePlayerInteractAction();
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

        interactionPrompt.SetActive(CanInteract() && !IsSignShowing());
        RefreshInteractionPromptGlyphIfNeeded();
    }

    private bool IsSignShowing()
    {
        return signPanel != null && signPanel.activeSelf;
    }

    private void HideSignUI()
    {
        if (signPanel != null)
        {
            signPanel.SetActive(false);
        }

        hideAtTime = -1f;
        UpdatePromptState();
    }

}