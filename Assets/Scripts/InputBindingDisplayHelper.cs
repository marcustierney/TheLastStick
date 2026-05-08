using System;
using System.Globalization;
using UnityEngine.InputSystem;

public readonly struct InputBindingDisplayInfo
{
    public readonly string FullPath;
    public readonly string Display;
    public readonly string Alias;

    public InputBindingDisplayInfo(string fullPath, string display, string alias)
    {
        FullPath = fullPath;
        Display = display;
        Alias = alias;
    }
}

public static class InputBindingDisplayHelper
{
    public static InputBindingDisplayInfo Build(InputAction action, int bindingIndex)
    {
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            return new InputBindingDisplayInfo(null, "—", "—");
        }

        InputBinding binding = action.bindings[bindingIndex];
        string fullPath = string.IsNullOrWhiteSpace(binding.effectivePath)
            ? (!string.IsNullOrWhiteSpace(binding.overridePath) ? binding.overridePath : binding.path)
            : binding.effectivePath;

        string display = action.GetBindingDisplayString(bindingIndex);
        if (string.IsNullOrWhiteSpace(display))
        {
            display = FormatBindingDisplay(fullPath);
        }

        string alias = ToAlias(display, fullPath);
        return new InputBindingDisplayInfo(fullPath, display, alias);
    }

    public static InputBindingDisplayInfo BuildFromPath(string fullPath)
    {
        string display = FormatBindingDisplay(fullPath);
        string alias = ToAlias(display, fullPath);
        return new InputBindingDisplayInfo(fullPath, display, alias);
    }

    public static InputBindingDisplayInfo BuildFromDisplay(string display, string fullPath = null)
    {
        string normalizedDisplay = string.IsNullOrWhiteSpace(display) ? "—" : display.Trim();
        string alias = ToAlias(normalizedDisplay, fullPath);
        return new InputBindingDisplayInfo(fullPath, normalizedDisplay, alias);
    }

    private static string ToAlias(string display, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(display))
        {
            return "—";
        }

        string normalizedDisplay = display.Trim();
        string normalized = normalizedDisplay.ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty);

        string normalizedPath = string.IsNullOrWhiteSpace(fullPath)
            ? string.Empty
            : fullPath.Trim().ToLowerInvariant();

        string shortAliasFromPath = BuildShortAliasFromPath(fullPath);

        // Prefer deterministic control-path aliases when available.
        if (!string.IsNullOrEmpty(normalizedPath))
        {
            if (normalizedPath.Contains("/buttonsouth")) return "A";
            if (normalizedPath.Contains("/buttoneast")) return "B";
            if (normalizedPath.Contains("/buttonwest")) return "X";
            if (normalizedPath.Contains("/buttonnorth")) return "Y";
            if (normalizedPath.Contains("/leftshoulder")) return "LB";
            if (normalizedPath.Contains("/rightshoulder")) return "RB";
            if (normalizedPath.Contains("/lefttrigger")) return "LT";
            if (normalizedPath.Contains("/righttrigger")) return "RT";
            if (normalizedPath.Contains("/leftstickpress")) return "L3";
            if (normalizedPath.Contains("/rightstickpress")) return "R3";
            if (normalizedPath.Contains("/dpad/left")) return "D-Left";
            if (normalizedPath.Contains("/dpad/right")) return "D-Right";
            if (normalizedPath.Contains("/dpad/up")) return "D-Up";
            if (normalizedPath.Contains("/dpad/down")) return "D-Down";

            // For keyboard/mouse and non-face gamepad controls, prefer Unity short names
            // (e.g. "<Keyboard>/q" -> "Q") over verbose display labels.
            if (!string.IsNullOrWhiteSpace(shortAliasFromPath))
            {
                return shortAliasFromPath;
            }
        }

        if (normalized.Contains("/buttonsouth") || normalized.Contains("buttonsouth")) return "A";
        if (normalized.Contains("/buttoneast") || normalized.Contains("buttoneast")) return "B";
        if (normalized.Contains("/buttonwest") || normalized.Contains("buttonwest")) return "X";
        if (normalized.Contains("/buttonnorth") || normalized.Contains("buttonnorth")) return "Y";
        if (normalized.Contains("/leftshoulder") || normalized.Contains("leftshoulder")) return "LB";
        if (normalized.Contains("/rightshoulder") || normalized.Contains("rightshoulder")) return "RB";
        if (normalized.Contains("/lefttrigger") || normalized.Contains("lefttrigger")) return "LT";
        if (normalized.Contains("/righttrigger") || normalized.Contains("righttrigger")) return "RT";
        if (normalized.Contains("/leftstickpress") || normalized.Contains("leftstickpress")) return "L3";
        if (normalized.Contains("/rightstickpress") || normalized.Contains("rightstickpress")) return "R3";

        if (normalized.Contains("/dpad/left") || normalized.EndsWith("dpadleft")) return "D-Left";
        if (normalized.Contains("/dpad/right") || normalized.EndsWith("dpadright")) return "D-Right";
        if (normalized.Contains("/dpad/up") || normalized.EndsWith("dpadup")) return "D-Up";
        if (normalized.Contains("/dpad/down") || normalized.EndsWith("dpaddown")) return "D-Down";

        // Common platform face-button names.
        if (normalized == "cross") return "A";
        if (normalized == "circle") return "B";
        if (normalized == "square") return "X";
        if (normalized == "triangle") return "Y";

        if (!string.IsNullOrWhiteSpace(fullPath) &&
            (normalizedDisplay.Contains("<") || normalizedDisplay.Contains("/")))
        {
            string formattedFromPath = FormatBindingDisplay(fullPath);
            if (!string.Equals(formattedFromPath, normalizedDisplay, StringComparison.Ordinal))
            {
                return ToAlias(formattedFromPath, null);
            }
        }

        return normalizedDisplay switch
        {
            "Button South" => "A",
            "Button East" => "B",
            "Button West" => "X",
            "Button North" => "Y",
            "Left Shoulder" => "LB",
            "Right Shoulder" => "RB",
            "Left Trigger" => "LT",
            "Right Trigger" => "RT",
            "Left Stick Press" => "L3",
            "Right Stick Press" => "R3",
            "Start" => "Menu",
            "Select" => "View",
            _ => !string.IsNullOrWhiteSpace(shortAliasFromPath) ? shortAliasFromPath : normalizedDisplay
        };
    }

    private static string BuildShortAliasFromPath(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return string.Empty;
        }

        string shortName = InputControlPath.ToHumanReadableString(
            fullPath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
            | InputControlPath.HumanReadableStringOptions.UseShortNames);

        if (string.IsNullOrWhiteSpace(shortName))
        {
            return string.Empty;
        }

        string trimmed = shortName.Trim();
        if (trimmed.Length == 1 && char.IsLetter(trimmed[0]))
        {
            return trimmed.ToUpperInvariant();
        }

        // Fallback: some bindings can surface as raw slash notation (e.g. "Keyboard/q").
        // Normalize those to compact aliases without hardcoding each key.
        string raw = fullPath.Trim();
        int slashIndex = raw.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex < raw.Length - 1)
        {
            string controlToken = raw.Substring(slashIndex + 1).Trim();
            if (controlToken.Length == 1 && char.IsLetter(controlToken[0]))
            {
                return controlToken.ToUpperInvariant();
            }

            if (controlToken.Length == 1 && char.IsDigit(controlToken[0]))
            {
                return controlToken;
            }

            if (string.Equals(controlToken, "leftArrow", StringComparison.OrdinalIgnoreCase)) return "Left";
            if (string.Equals(controlToken, "rightArrow", StringComparison.OrdinalIgnoreCase)) return "Right";
            if (string.Equals(controlToken, "upArrow", StringComparison.OrdinalIgnoreCase)) return "Up";
            if (string.Equals(controlToken, "downArrow", StringComparison.OrdinalIgnoreCase)) return "Down";
            if (string.Equals(controlToken, "space", StringComparison.OrdinalIgnoreCase)) return "Space";
            if (string.Equals(controlToken, "escape", StringComparison.OrdinalIgnoreCase)) return "Esc";
            if (string.Equals(controlToken, "leftShift", StringComparison.OrdinalIgnoreCase)) return "LShift";
            if (string.Equals(controlToken, "rightShift", StringComparison.OrdinalIgnoreCase)) return "RShift";
            if (string.Equals(controlToken, "leftCtrl", StringComparison.OrdinalIgnoreCase)) return "LCtrl";
            if (string.Equals(controlToken, "rightCtrl", StringComparison.OrdinalIgnoreCase)) return "RCtrl";
            if (string.Equals(controlToken, "leftAlt", StringComparison.OrdinalIgnoreCase)) return "LAlt";
            if (string.Equals(controlToken, "rightAlt", StringComparison.OrdinalIgnoreCase)) return "RAlt";
        }

        return trimmed;
    }

    private static string FormatBindingDisplay(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "—";
        }

        string normalizedPath = path.ToLowerInvariant();
        if (normalizedPath.Contains("/dpad/"))
        {
            string direction = FormatDirection(path);
            return string.IsNullOrEmpty(direction) ? "D-Pad" : $"D-Pad {direction}";
        }

        if (normalizedPath.Contains("/leftstick/"))
        {
            string direction = FormatDirection(path);
            return string.IsNullOrEmpty(direction) ? "Left Stick" : $"Left Stick {direction}";
        }

        if (normalizedPath.EndsWith("/leftstick"))
        {
            return "Left Stick";
        }

        if (normalizedPath.Contains("/rightstick/"))
        {
            string direction = FormatDirection(path);
            return string.IsNullOrEmpty(direction) ? "Right Stick" : $"Right Stick {direction}";
        }

        if (normalizedPath.EndsWith("/rightstick"))
        {
            return "Right Stick";
        }

        string displayName = InputControlPath.ToHumanReadableString(
            path,
            InputControlPath.HumanReadableStringOptions.OmitDevice);

        return string.IsNullOrEmpty(displayName) ? "—" : CapitalizeWords(displayName);
    }

    private static string FormatDirection(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string[] segments = path.Split('/');
        if (segments.Length == 0)
        {
            return string.Empty;
        }

        string direction = segments[segments.Length - 1].ToLowerInvariant();
        return direction switch
        {
            "left" => "Left",
            "right" => "Right",
            "up" => "Up",
            "down" => "Down",
            _ => string.Empty
        };
    }

    private static string CapitalizeWords(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
    }
}
