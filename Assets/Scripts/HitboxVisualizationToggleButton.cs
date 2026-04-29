using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to a Toggle (UI) to control hitbox visualization from the options menu
/// Pattern: Following VSyncToggle and GreyscaleToggleButton implementation
/// </summary>
public class HitboxVisualizationToggleButton : MonoBehaviour
{
    private HitboxVisualizationManager manager;
    private Toggle toggle;

    private void OnEnable()
    {
        manager = HitboxVisualizationManager.GetInstance();
        toggle = GetComponent<Toggle>();

        if (manager == null)
        {
            // For non-level scenes
            if (toggle != null)
            {
                toggle.interactable = false;
            }
            return;
        }
        
        if (toggle != null)
        {
            toggle.interactable = true;
            toggle.isOn = manager.ShowHitboxes;
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
        
        manager.HitboxVisualizationStateChanged += HandleVisualizationStateChanged;
    }

    private void OnDisable()
    {
        if (manager != null)
        {
            manager.HitboxVisualizationStateChanged -= HandleVisualizationStateChanged;
        }
        
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }

    private void OnToggleValueChanged(bool isOn)
    {
        if (manager == null) return;
        manager.SetHitboxVisualization(isOn);
    }

    private void HandleVisualizationStateChanged(bool isEnabled)
    {
        if (toggle != null && toggle.isOn != isEnabled)
        {
            toggle.isOn = isEnabled;
        }
    }
}
