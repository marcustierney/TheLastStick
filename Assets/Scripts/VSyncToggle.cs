using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VSyncToggle : MonoBehaviour
{
    public Toggle toggle;
    public TMP_Dropdown fpsDropdown;

    void Start()
    {
        bool vSyncOn = QualitySettings.vSyncCount > 0;
        toggle.isOn = vSyncOn;
        fpsDropdown.interactable = !vSyncOn;
        toggle.onValueChanged.AddListener(isOn => fpsDropdown.interactable = !isOn);
    }
}