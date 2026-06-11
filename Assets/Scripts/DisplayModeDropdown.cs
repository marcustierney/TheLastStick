using UnityEngine;
using TMPro;

public class DisplayModeDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    FullScreenMode[] modes =
    {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.Windowed,
        FullScreenMode.FullScreenWindow
    };

    FullScreenMode pendingMode;

    void Awake()
    {
        if (dropdown != null)
        {
            DropdownGamepadSupport.EnsureOn(dropdown);
        }
    }

    void Start()
    {
        pendingMode = Screen.fullScreenMode;

        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen: dropdown.value = 0; break;
            case FullScreenMode.Windowed:            dropdown.value = 1; break;
            case FullScreenMode.FullScreenWindow:    dropdown.value = 2; break;
        }

        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(i => pendingMode = modes[i]);
    }

    public void Apply()
    {
        Screen.fullScreenMode = pendingMode;
    }
}