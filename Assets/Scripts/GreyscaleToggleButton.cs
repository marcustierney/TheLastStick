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
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

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

        if (manager != null)
        {
            manager.GreyscaleStateChanged -= HandleGreyscaleStateChanged;
        }
    }

    private void HandleGreyscaleStateChanged(bool enabled)
    {
        if (targetImage == null)
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
}
