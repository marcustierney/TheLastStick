using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ResolutionDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    Resolution[] resolutions;
    Resolution pendingResolution;

    void Awake()
    {
        if (dropdown == null)
        {
            dropdown = GetComponentInChildren<TMP_Dropdown>(true);
        }

        FlattenDropdownRectZ();
        DisableTemplateScrollbarNavigation();
        DropdownGamepadSupport.EnsureOn(dropdown);
    }

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

    void FlattenDropdownRectZ()
    {
        RectTransform rect = dropdown.transform as RectTransform;
        if (rect == null)
        {
            return;
        }

        Vector3 local = rect.localPosition;
        if (local.z != 0f)
        {
            rect.localPosition = new Vector3(local.x, local.y, 0f);
        }
    }

    void DisableTemplateScrollbarNavigation()
    {
        if (dropdown.template == null)
        {
            return;
        }

        Scrollbar scrollbar = dropdown.template.GetComponentInChildren<Scrollbar>(true);
        if (scrollbar == null)
        {
            return;
        }

        Navigation navigation = scrollbar.navigation;
        navigation.mode = Navigation.Mode.None;
        scrollbar.navigation = navigation;
    }

    public void Apply()
    {
        Screen.SetResolution(pendingResolution.width, pendingResolution.height, Screen.fullScreenMode);
    }
}