using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class FPSCapDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public Toggle vSyncToggle;

    int[] fpsOptions = { 30, 60, 120, 144, -1 };
    int pendingFPS = 60;

    void Start()
    {
        dropdown.ClearOptions();

        List<string> options = new List<string>();
        foreach (int fps in fpsOptions)
            options.Add(fps == -1 ? "Unlimited" : fps + " FPS");

        dropdown.AddOptions(options);
        dropdown.value = System.Array.IndexOf(fpsOptions, pendingFPS);
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.AddListener(i => pendingFPS = fpsOptions[i]);
    }

    public void Apply()
    {
        if (vSyncToggle.isOn)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = pendingFPS;
        }
    }
}