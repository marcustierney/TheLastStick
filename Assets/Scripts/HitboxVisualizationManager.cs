using UnityEngine;

/// <summary>
/// Manages the visibility of sword hitboxes throughout the game.
/// Provides a toggle to show/hide all red sword hitbox visuals.
/// Pattern: Singleton with event system (UI button controlled)
/// </summary>
public class HitboxVisualizationManager : MonoBehaviour
{
    private const string HitboxVisualizationEnabledKey = "tls.hitboxVisualizationEnabled";

    private static HitboxVisualizationManager instance;
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private bool useSavedState = true;

    [SerializeField] private bool showHitboxes = false;
    [SerializeField] private Color hitboxColor = Color.red;
    
    public event System.Action<bool> HitboxVisualizationStateChanged;
    
    public bool ShowHitboxes => showHitboxes;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        LoadState();
    }

    private void LoadState()
    {
        if (useSavedState && PlayerPrefs.HasKey(HitboxVisualizationEnabledKey))
        {
            showHitboxes = PlayerPrefs.GetInt(HitboxVisualizationEnabledKey) == 1;
            return;
        }

        if (useSavedState)
        {
            SaveState();
        }
    }

    private void SaveState()
    {
        if (!useSavedState)
        {
            return;
        }

        PlayerPrefs.SetInt(HitboxVisualizationEnabledKey, showHitboxes ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleHitboxVisualization()
    {
        SetHitboxVisualization(!showHitboxes);
    }

    public void SetHitboxVisualization(bool enabled)
    {
        showHitboxes = enabled;
        SaveState();
        HitboxVisualizationStateChanged?.Invoke(showHitboxes);
    }

    /// <summary>
    /// Call this from any script's OnDrawGizmos() to draw its hitbox
    /// Example: HitboxVisualizationManager.instance?.DrawHitbox(transform.position, boxCollider.size, boxCollider.offset);
    /// </summary>
    public void DrawHitbox(Vector3 position, Vector3 size, Vector3 offset)
    {
        if (!showHitboxes) return;
        
        Gizmos.color = hitboxColor;
        Gizmos.DrawWireCube(position + offset, size);
    }

    /// <summary>
    /// Call this to draw a circle/sphere hitbox
    /// </summary>
    public void DrawCircleHitbox(Vector3 position, float radius)
    {
        if (!showHitboxes) return;
        
        Gizmos.color = hitboxColor;
        DrawCircle(position, radius, hitboxColor);
    }

    /// <summary>
    /// Helper to draw a circle in 2D space
    /// </summary>
    private void DrawCircle(Vector3 position, float radius, Color color)
    {
        int segments = 32;
        float angle = 0f;
        float angleStep = 360f / segments;
        Vector3 lastPoint = position + new Vector3(radius, 0, 0);

        for (int i = 0; i < segments; i++)
        {
            angle += angleStep;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 newPoint = position + new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0);
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }
    }

    public static HitboxVisualizationManager GetInstance()
    {
        if (instance == null)
        {
            instance = FindAnyObjectByType<HitboxVisualizationManager>();
        }

        return instance;
    }
}
