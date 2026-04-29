using UnityEngine;

/// <summary>
/// Attach this to any GameObject with a sword hitbox (BoxCollider2D or CircleCollider2D)
/// to automatically visualize it when HitboxVisualizationManager is enabled.
/// 
/// Usage: Add component to GameObject → It will draw the hitbox visuals automatically
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SwordHitboxVisualizer : MonoBehaviour
{
    private new Collider2D collider2D;
    private HitboxVisualizationManager vizManager;
    private SpriteRenderer[] spriteRenderers;

    [SerializeField] private bool affectSpriteVisibility = true;
    [SerializeField] [Range(0f, 1f)] private float visibleAlpha = 1f;
    [SerializeField] [Range(0f, 1f)] private float hiddenAlpha = 0f;

    private void OnEnable()
    {
        collider2D = GetComponent<Collider2D>();
        vizManager = HitboxVisualizationManager.GetInstance();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (vizManager != null)
        {
            vizManager.HitboxVisualizationStateChanged += HandleVisualizationStateChanged;
            ApplySpriteVisibility(vizManager.ShowHitboxes);
        }
    }

    private void OnDisable()
    {
        if (vizManager != null)
        {
            vizManager.HitboxVisualizationStateChanged -= HandleVisualizationStateChanged;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        if (vizManager == null)
        {
            vizManager = HitboxVisualizationManager.GetInstance();
        }

        if (vizManager == null) return;
        if (!vizManager.ShowHitboxes) return;

        collider2D = GetComponent<Collider2D>();
        if (collider2D == null) return;

        // Handle BoxCollider2D
        if (collider2D is BoxCollider2D boxCollider)
        {
            Vector3 worldPos = transform.TransformPoint(new Vector3(boxCollider.offset.x, boxCollider.offset.y, 0));
            vizManager.DrawHitbox(worldPos, boxCollider.size, Vector3.zero);
        }
        // Handle CircleCollider2D
        else if (collider2D is CircleCollider2D circleCollider)
        {
            Vector3 worldPos = transform.TransformPoint(new Vector3(circleCollider.offset.x, circleCollider.offset.y, 0));
            vizManager.DrawCircleHitbox(worldPos, circleCollider.radius);
        }
    }

    private void HandleVisualizationStateChanged(bool showHitboxes)
    {
        ApplySpriteVisibility(showHitboxes);
    }

    private void ApplySpriteVisibility(bool showHitboxes)
    {
        if (!affectSpriteVisibility || spriteRenderers == null)
        {
            return;
        }

        float targetAlpha = showHitboxes ? visibleAlpha : hiddenAlpha;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;

            Color c = sr.color;
            c.a = targetAlpha;
            sr.color = c;
        }
    }
}
