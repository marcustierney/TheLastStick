using UnityEngine;
using UnityEngine.InputSystem;

public static class InputBindingOverrides
{
    private const string BindingKeyPrefix = "Binding_";
    private const string DefaultBindingKeyPrefix = "BindingDefault_";

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
                string defaultKey = DefaultBindingKeyPrefix + binding.id;
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
                PlayerPrefs.DeleteKey(DefaultBindingKeyPrefix + bindingId);
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
                string key = BindingKeyPrefix + binding.id;
                if (!PlayerPrefs.HasKey(key))
                {
                    continue;
                }

                string savedPath = PlayerPrefs.GetString(key);
                if (string.IsNullOrWhiteSpace(savedPath))
                {
                    continue;
                }

                action.ApplyBindingOverride(new InputBinding
                {
                    id = binding.id,
                    overridePath = savedPath
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

                action.RemoveBindingOverride(i);
                PlayerPrefs.DeleteKey(BindingKeyPrefix + bindingId);

                string defaultKey = DefaultBindingKeyPrefix + bindingId;
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
