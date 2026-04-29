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
            Debug.LogError("HitboxVisualizationToggleButton: No HitboxVisualizationManager found in scene.");
            enabled = false;
            return;
        }
        
        if (toggle != null)
        {
            // Set initial state
            toggle.isOn = manager.ShowHitboxes;
            // Listen for toggle changes
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
        
        // Listen for manager state changes
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
