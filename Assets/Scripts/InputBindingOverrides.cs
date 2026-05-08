using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class InputBindingOverrides
{
    private const string BindingKeyPrefix = "Binding_";
    private const string BindingActionIndexKeyPrefix = "BindingByAction_";
    private const string DefaultBindingKeyPrefix = "BindingDefault_";

    /// <summary>
    /// In-scene clones of <see cref="InputActionAsset"/> (e.g. <c>new InputSystem_Actions()</c>)
    /// that should receive prefs-driven overrides alongside <see cref="PlayerInput"/>.
    /// </summary>
    private static readonly List<InputActionAsset> s_runtimeGameplayAssets = new List<InputActionAsset>();

    public static void RegisterRuntimeGameplayAsset(InputActionAsset asset)
    {
        if (asset == null || s_runtimeGameplayAssets.Contains(asset))
        {
            return;
        }

        s_runtimeGameplayAssets.Add(asset);
    }

    public static void UnregisterRuntimeGameplayAsset(InputActionAsset asset)
    {
        if (asset == null)
        {
            return;
        }

        s_runtimeGameplayAssets.Remove(asset);
    }

    public static void RefreshAllRegisteredRuntimeAssetsFromPrefs()
    {
        for (int i = s_runtimeGameplayAssets.Count - 1; i >= 0; i--)
        {
            InputActionAsset asset = s_runtimeGameplayAssets[i];
            if (asset == null)
            {
                s_runtimeGameplayAssets.RemoveAt(i);
                continue;
            }

            ApplySavedOverrides(asset);
        }
    }

    public static void ResetAllRegisteredRuntimeAssetsToCachedDefaults()
    {
        for (int i = s_runtimeGameplayAssets.Count - 1; i >= 0; i--)
        {
            InputActionAsset asset = s_runtimeGameplayAssets[i];
            if (asset == null)
            {
                s_runtimeGameplayAssets.RemoveAt(i);
                continue;
            }

            ResetToCachedDefaults(asset);
        }
    }
    
    public static string GetOverrideKey(InputBinding binding) => BindingKeyPrefix + binding.id;
    public static string GetOverrideKey(string bindingId) => BindingKeyPrefix + bindingId;
    public static string GetOverrideActionIndexKey(InputAction action, int bindingIndex)
    {
        if (action == null)
        {
            return string.Empty;
        }

        string mapName = action.actionMap != null ? action.actionMap.name : "<NoMap>";
        return $"{BindingActionIndexKeyPrefix}{mapName}/{action.name}/{bindingIndex}";
    }

    public static string GetDefaultKey(InputBinding binding) => DefaultBindingKeyPrefix + binding.id;
    public static string GetDefaultKey(string bindingId) => DefaultBindingKeyPrefix + bindingId;

    public static string NormalizeOverridePath(string rawPath, string fallbackBindingPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return rawPath;
        }

        string trimmedPath = rawPath.Trim();
        if (trimmedPath.StartsWith("<"))
        {
            if (string.IsNullOrWhiteSpace(fallbackBindingPath) || !fallbackBindingPath.StartsWith("<"))
            {
                return trimmedPath;
            }

            int existingDeviceEnd = trimmedPath.IndexOf(">/", System.StringComparison.Ordinal);
            int fallbackDeviceEnd = fallbackBindingPath.IndexOf(">/", System.StringComparison.Ordinal);
            if (existingDeviceEnd < 0 || fallbackDeviceEnd < 0)
            {
                return trimmedPath;
            }

            string existingDevicePrefix = trimmedPath.Substring(0, existingDeviceEnd + 1);
            string fallbackDevicePrefix = fallbackBindingPath.Substring(0, fallbackDeviceEnd + 1);
            string existingControlPart = trimmedPath.Substring(existingDeviceEnd + 2);

            int slashInControl = existingControlPart.IndexOf('/');
            if (slashInControl > 0 && slashInControl < existingControlPart.Length - 1)
            {
                string firstSegment = existingControlPart.Substring(0, slashInControl);
                if (LooksLikeDeviceSegment(firstSegment, existingDevicePrefix))
                {
                    string remainder = existingControlPart.Substring(slashInControl + 1);
                    return $"{fallbackDevicePrefix}/{remainder}";
                }
            }

            return trimmedPath;
        }

        if (string.IsNullOrWhiteSpace(fallbackBindingPath) || !fallbackBindingPath.StartsWith("<"))
        {
            return trimmedPath;
        }

        int devicePathEnd = fallbackBindingPath.IndexOf(">/", System.StringComparison.Ordinal);
        if (devicePathEnd < 0)
        {
            return trimmedPath;
        }

        string devicePrefix = fallbackBindingPath.Substring(0, devicePathEnd + 1);
        string controlPart = trimmedPath.TrimStart('/');
        if (string.IsNullOrWhiteSpace(controlPart))
        {
            return trimmedPath;
        }

        int slash = controlPart.IndexOf('/');
        if (slash > 0 && slash < controlPart.Length - 1)
        {
            string firstSegment = controlPart.Substring(0, slash);
            string rest = controlPart.Substring(slash + 1);

            if (LooksLikeDeviceSegment(firstSegment, devicePrefix))
            {
                controlPart = rest;
            }
        }

        return $"{devicePrefix}/{controlPart}";
    }

    private static bool LooksLikeDeviceSegment(string segment, string fallbackDevicePrefix)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            return false;
        }

        string normalizedSegment = segment.Trim().ToLowerInvariant();
        string normalizedFallbackDevice = fallbackDevicePrefix
            .Trim()
            .TrimStart('<')
            .TrimEnd('>')
            .ToLowerInvariant();

        if (normalizedSegment == normalizedFallbackDevice)
        {
            return true;
        }

        return normalizedSegment.Contains("keyboard")
            || normalizedSegment.Contains("mouse")
            || normalizedSegment.Contains("gamepad")
            || normalizedSegment.Contains("joystick")
            || normalizedSegment.Contains("xinput")
            || normalizedSegment.Contains("dualshock")
            || normalizedSegment.Contains("dualsense")
            || normalizedSegment.Contains("controller");
    }

    public static void EnsureDefaultsCached(InputActionAsset asset)
    {
        if (asset == null)
        {
            return;
        }

        foreach (InputAction action in asset)
        {
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];
                string defaultKey = GetDefaultKey(binding);
                if (PlayerPrefs.HasKey(defaultKey))
                {
                    continue;
                }

                PlayerPrefs.SetString(defaultKey, binding.path);
            }
        }
    }

    public static void RebuildDefaultsCache(InputActionAsset asset)
    {
        if (asset == null)
        {
            return;
        }

        foreach (InputAction action in asset)
        {
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                string bindingId = bindings[i].id.ToString();
                PlayerPrefs.DeleteKey(GetDefaultKey(bindingId));
            }
        }

        EnsureDefaultsCached(asset);
        PlayerPrefs.Save();
    }

    public static void ApplySavedOverrides(InputActionAsset asset)
    {
        if (asset == null)
        {
            return;
        }

        EnsureDefaultsCached(asset);

        foreach (InputAction action in asset)
        {
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];
                string keyById = GetOverrideKey(binding);
                string keyByActionIndex = GetOverrideActionIndexKey(action, i);
                bool hasIdKey = PlayerPrefs.HasKey(keyById);
                bool hasActionIndexKey = !string.IsNullOrEmpty(keyByActionIndex) && PlayerPrefs.HasKey(keyByActionIndex);
                if (!hasIdKey && !hasActionIndexKey)
                {
                    continue;
                }

                string savedPathById = hasIdKey ? PlayerPrefs.GetString(keyById) : null;
                string savedPathByActionIndex = hasActionIndexKey ? PlayerPrefs.GetString(keyByActionIndex) : null;
                string savedPath;
                if (hasIdKey && hasActionIndexKey)
                {
                    // Empty override means explicit unbind; it must win across key schemes.
                    if (string.IsNullOrEmpty(savedPathById) || string.IsNullOrEmpty(savedPathByActionIndex))
                    {
                        savedPath = string.Empty;
                    }
                    else
                    {
                        // Prefer action-index key when both are populated.
                        savedPath = savedPathByActionIndex;
                    }
                }
                else
                {
                    savedPath = hasIdKey ? savedPathById : savedPathByActionIndex;
                }

                string normalizedSavedPath = NormalizeOverridePath(savedPath, binding.path);
                if (!string.Equals(savedPath, normalizedSavedPath, System.StringComparison.Ordinal))
                {
                    if (hasIdKey)
                    {
                        PlayerPrefs.SetString(keyById, normalizedSavedPath);
                    }
                    if (hasActionIndexKey)
                    {
                        PlayerPrefs.SetString(keyByActionIndex, normalizedSavedPath);
                    }
                }

                // Keep both key schemes in sync so all runtime assets can resolve overrides.
                PlayerPrefs.SetString(keyById, normalizedSavedPath);
                if (!string.IsNullOrEmpty(keyByActionIndex))
                {
                    PlayerPrefs.SetString(keyByActionIndex, normalizedSavedPath);
                }
                action.ApplyBindingOverride(new InputBinding
                {
                    id = binding.id,
                    overridePath = normalizedSavedPath
                });
            }
        }
    }

    public static void ResetToCachedDefaults(InputActionAsset asset)
    {
        if (asset == null)
        {
            return;
        }

        EnsureDefaultsCached(asset);

        foreach (InputAction action in asset)
        {
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];
                string bindingId = binding.id.ToString();
                string actionIndexKey = GetOverrideActionIndexKey(action, i);

                action.RemoveBindingOverride(i);
                PlayerPrefs.DeleteKey(GetOverrideKey(bindingId));
                if (!string.IsNullOrEmpty(actionIndexKey))
                {
                    PlayerPrefs.DeleteKey(actionIndexKey);
                }

                string defaultKey = GetDefaultKey(bindingId);
                if (!PlayerPrefs.HasKey(defaultKey))
                {
                    continue;
                }

                string defaultPath = PlayerPrefs.GetString(defaultKey);
                if (string.IsNullOrWhiteSpace(defaultPath))
                {
                    continue;
                }

                if (binding.path == defaultPath)
                {
                    continue;
                }

                action.ApplyBindingOverride(new InputBinding
                {
                    id = binding.id,
                    overridePath = defaultPath
                });
            }
        }
    }

}
