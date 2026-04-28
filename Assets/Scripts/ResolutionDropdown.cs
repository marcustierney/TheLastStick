using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ResolutionDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    Resolution[] resolutions;
    Resolution pendingResolution;

    void Start()
    {
        resolutions = Screen.resolutions;
        dropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add(resolutions[i].width + " x " + resolutions[i].height);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                currentIndex = i;
        }

        dropdown.AddOptions(options);
        dropdown.value = currentIndex;
        dropdown.RefreshShownValue();

        pendingResolution = resolutions[currentIndex];
        dropdown.onValueChanged.AddListener(i => pendingResolution = resolutions[i]);
    }

    public void Apply()
    {
        Screen.SetResolution(pendingResolution.width, pendingResolution.height, Screen.fullScreenMode);
    }
}