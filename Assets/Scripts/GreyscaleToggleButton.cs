using UnityEngine;
using UnityEngine.UI;

public class GreyscaleToggleButton : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite lanternOnSprite;
    [SerializeField] private Sprite lanternOffSprite;

    private GreyscaleManager manager;

    private void Awake()
    {
        ResolveTargetImage();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ResolveTargetImage();
        manager = EnsureGreyscaleManager();

        manager.GreyscaleStateChanged += HandleGreyscaleStateChanged;
        HandleGreyscaleStateChanged(manager.IsGreyscaleEnabled);
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        UnregisterFromManager();
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        UnregisterFromManager();
        targetImage = null;
    }

    private void HandleGreyscaleStateChanged(bool enabled)
    {
        if (!ResolveTargetImage())
        {
            return;
        }

        targetImage.sprite = enabled ? lanternOffSprite : lanternOnSprite;
    }

    public void ToggleGreyscaleFromButton()
    {
        EnsureGreyscaleManager().ToggleGreyscale();
    }

    private static GreyscaleManager EnsureGreyscaleManager()
    {
        GreyscaleManager existingManager = UnityEngine.Object.FindAnyObjectByType<GreyscaleManager>(FindObjectsInactive.Include);
        if (existingManager != null)
        {
            return existingManager;
        }

        GameObject managerObject = new GameObject("GreyscaleManager");
        return managerObject.AddComponent<GreyscaleManager>();
    }

    private void UnregisterFromManager()
    {
        if (manager != null)
        {
            manager.GreyscaleStateChanged -= HandleGreyscaleStateChanged;
            manager = null;
        }
    }

    private bool ResolveTargetImage()
    {
        if (targetImage != null)
        {
            return true;
        }

        targetImage = GetComponent<Image>();
        return targetImage != null;
    }
}
