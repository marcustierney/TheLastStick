using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(50)]
public class InteractPromptDisplay : MonoBehaviour
{
    [Header("Interact prompt sprites")]
    [SerializeField] private Sprite gamepadNorthSprite;
    [SerializeField] private Sprite gamepadEastSprite;
    [SerializeField] private Sprite gamepadSouthSprite;
    [SerializeField] private Sprite gamepadWestSprite;

    private TMP_Text glyphLabel;
    private SpriteRenderer gamepadIcon;
    private SpriteRenderer keyboardIcon;
    private PlayerInput playerInput;
    private UIFocusGuard uiFocusGuard;
    private InputAction cachedInteractAction;

    private const string GamepadSchemeName = "Gamepad";

    private void Awake()
    {
        CacheVisualReferences();
        uiFocusGuard = FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
        playerInput = FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Include);
        CacheInteractAction();
    }

    private void LateUpdate()
    {
        if (gameObject.activeInHierarchy)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        CacheVisualReferences();

        InputAction interactAction = ResolveInteractAction();
        if (interactAction == null)
        {
            return;
        }

        bool schemeIsGamepad = ShouldShowGamepadGlyph();

        if (!schemeIsGamepad)
        {
            RefreshKeyboardOrMouseGlyph(interactAction);
            return;
        }

        RefreshGamepadGlyph(interactAction);
    }

    private void RefreshKeyboardOrMouseGlyph(InputAction interactAction)
    {
        if (TryGetFirstInteractBinding(interactAction, "<Keyboard>/", out int keyboardIndex, out string keyboardPath))
        {
            InputBindingDisplayInfo keyboardDisplay = InputBindingDisplayHelper.Build(interactAction, keyboardIndex);
            bool useTextOnly = IsTextOnlyFallbackBinding(keyboardPath);
            SetSpriteVisualActive(gamepadIcon, false);
            SetSpriteVisualActive(keyboardIcon, !useTextOnly);
            SetGlyphText(keyboardDisplay.Alias);
            return;
        }

        SetSpriteVisualActive(keyboardIcon, false);
        if (TryGetFirstInteractBinding(interactAction, "<Mouse>/", out int mouseIndex, out string mousePath))
        {
            InputBindingDisplayInfo mouseDisplay = InputBindingDisplayHelper.Build(interactAction, mouseIndex);
            bool useTextOnly = IsTextOnlyFallbackBinding(mousePath);
            SetSpriteVisualActive(keyboardIcon, !useTextOnly);
            SetGlyphText(mouseDisplay.Alias);
            return;
        }

        SetGlyphText(string.Empty);
    }

    private void RefreshGamepadGlyph(InputAction interactAction)
    {
        if (!TryGetFirstInteractBinding(interactAction, "<Gamepad>/", out int gamepadIndex, out string gamepadPath))
        {
            InputBindingDisplayInfo fallback = InputBindingDisplayHelper.BuildFromDisplay(interactAction.GetBindingDisplayString());
            ShowTextOnly(fallback.Alias);
            return;
        }

        InputBindingDisplayInfo gamepadDisplay = InputBindingDisplayHelper.Build(interactAction, gamepadIndex);
        Sprite faceSprite = MapGamepadPathToFaceSprite(gamepadPath);
        if (gamepadIcon != null && faceSprite != null)
        {
            SetSpriteVisualActive(gamepadIcon, true);
            gamepadIcon.sprite = faceSprite;
            SetSpriteVisualActive(keyboardIcon, false);
            SetGlyphText(null, visible: false);
            return;
        }

        ShowTextOnly(gamepadDisplay.Alias);
    }

    private void ShowTextOnly(string text)
    {
        SetSpriteVisualActive(gamepadIcon, false);
        SetSpriteVisualActive(keyboardIcon, false);
        SetGlyphText(text);
    }

    private void SetGlyphText(string text, bool visible = true)
    {
        if (glyphLabel == null)
        {
            return;
        }

        SetTextVisualActive(glyphLabel, visible);
        if (visible)
        {
            glyphLabel.text = text ?? string.Empty;
        }
    }

    private void CacheVisualReferences()
    {
        if (glyphLabel == null)
        {
            glyphLabel = GetComponentInChildren<TMP_Text>(true);
        }

        if (gamepadIcon == null)
        {
            Transform icon = transform.Find("Icon") ?? transform.Find("Gamepad");
            if (icon != null)
            {
                gamepadIcon = icon.GetComponent<SpriteRenderer>();
            }
        }

        if (keyboardIcon == null)
        {
            Transform icon = transform.Find("KeyboardIcon") ?? transform.Find("Keycap");
            if (icon != null)
            {
                keyboardIcon = icon.GetComponent<SpriteRenderer>();
            }
        }
    }

    private bool ShouldShowGamepadGlyph()
    {
        if (playerInput == null)
        {
            playerInput = FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Include);
            CacheInteractAction();
        }

        if (playerInput != null && !string.IsNullOrEmpty(playerInput.currentControlScheme))
        {
            return string.Equals(playerInput.currentControlScheme, GamepadSchemeName, StringComparison.Ordinal);
        }

        return uiFocusGuard != null && uiFocusGuard.IsGamepadInputActive;
    }

    private void CacheInteractAction()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            cachedInteractAction = null;
            return;
        }

        cachedInteractAction = playerInput.actions.FindAction("Gameplay/Interact", throwIfNotFound: false)
            ?? playerInput.actions.FindAction("Interact", throwIfNotFound: false);
    }

    private InputAction ResolveInteractAction()
    {
        if (playerInput == null)
        {
            playerInput = FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Include);
            CacheInteractAction();
        }

        InputAction resolved = playerInput != null && playerInput.actions != null
            ? (playerInput.actions.FindAction("Gameplay/Interact", throwIfNotFound: false)
               ?? playerInput.actions.FindAction("Interact", throwIfNotFound: false))
            : null;

        if (resolved != cachedInteractAction)
        {
            cachedInteractAction = resolved;
        }

        if (cachedInteractAction == null)
        {
            CacheInteractAction();
        }

        return cachedInteractAction;
    }

    private Sprite MapGamepadPathToFaceSprite(string path)
    {
        return MapGamepadInteractPathToFaceSprite(path, gamepadNorthSprite, gamepadEastSprite, gamepadSouthSprite,
            gamepadWestSprite);
    }

    private static bool TryGetFirstInteractBinding(InputAction interactAction, string devicePrefix,
        out int bindingIndex, out string effectivePath)
    {
        bindingIndex = -1;
        effectivePath = null;
        int count = interactAction.bindings.Count;
        for (int i = 0; i < count; i++)
        {
            InputBinding binding = interactAction.bindings[i];
            if (binding.isPartOfComposite)
            {
                continue;
            }

            string candidate = !string.IsNullOrEmpty(binding.overridePath) ? binding.overridePath : binding.path;
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

    private static Sprite MapGamepadInteractPathToFaceSprite(string path, Sprite north, Sprite east, Sprite south,
        Sprite west)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith("<Gamepad>/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (path.IndexOf("/buttonNorth", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return north;
        }

        if (path.IndexOf("/buttonEast", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return east;
        }

        if (path.IndexOf("/buttonSouth", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return south;
        }

        if (path.IndexOf("/buttonWest", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return west;
        }

        return null;
    }

    private static bool IsTextOnlyFallbackBinding(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return path.IndexOf("/leftButton", StringComparison.OrdinalIgnoreCase) >= 0
               || path.IndexOf("/rightButton", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void SetSpriteVisualActive(SpriteRenderer spriteRenderer, bool active)
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

    private static void SetTextVisualActive(TMP_Text textLabel, bool active)
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
}
