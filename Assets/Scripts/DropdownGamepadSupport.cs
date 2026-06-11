using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gamepad helpers for TMP_Dropdown: focus list items when opened and expose
/// a shared cancel path so menus close the list before backing out.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Dropdown))]
public class DropdownGamepadSupport : MonoBehaviour
{
    const string DropdownListName = "Dropdown List";

    TMP_Dropdown dropdown;
    bool wasExpanded;
    GameObject lastScrolledSelection;

    public static void EnsureOn(TMP_Dropdown target)
    {
        if (target == null)
        {
            return;
        }

        if (target.GetComponent<DropdownGamepadSupport>() == null)
        {
            target.gameObject.AddComponent<DropdownGamepadSupport>();
        }
    }

    public static bool IsAnyExpanded()
    {
        TMP_Dropdown[] dropdowns = Object.FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Exclude);
        foreach (TMP_Dropdown candidate in dropdowns)
        {
            if (candidate != null && candidate.IsExpanded)
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryCloseExpandedDropdown()
    {
        TMP_Dropdown[] dropdowns = Object.FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Exclude);
        foreach (TMP_Dropdown candidate in dropdowns)
        {
            if (candidate == null || !candidate.IsExpanded)
            {
                continue;
            }

            candidate.Hide();

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
                eventSystem.SetSelectedGameObject(candidate.gameObject);
            }

            UIFocusGuard focusGuard = Object.FindAnyObjectByType<UIFocusGuard>(FindObjectsInactive.Include);
            focusGuard?.SetCurrentFallback(candidate);

            return true;
        }

        return false;
    }

    void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
    }

    void LateUpdate()
    {
        if (dropdown == null)
        {
            return;
        }

        bool expanded = dropdown.IsExpanded;

        if (expanded && !wasExpanded)
        {
            lastScrolledSelection = null;
            StartCoroutine(FocusListNextFrame());
        }
        else if (expanded)
        {
            ScrollToCurrentListSelection();
        }
        else if (wasExpanded)
        {
            lastScrolledSelection = null;
        }

        wasExpanded = expanded;
    }

    IEnumerator FocusListNextFrame()
    {
        yield return null;

        if (dropdown == null || !dropdown.IsExpanded)
        {
            yield break;
        }

        GameObject list = FindDropdownList(dropdown);
        if (list == null)
        {
            yield break;
        }

        WireListItemNavigation(list);
        DisableScrollbarNavigation(list);

        Toggle[] toggles = list.GetComponentsInChildren<Toggle>(false);
        if (toggles.Length == 0)
        {
            yield break;
        }

        int index = Mathf.Clamp(dropdown.value, 0, toggles.Length - 1);
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            yield break;
        }

        GameObject selectedItem = toggles[index].gameObject;
        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(selectedItem);
        ScrollToListItem(list, selectedItem);
    }

    void ScrollToCurrentListSelection()
    {
        GameObject list = FindDropdownList(dropdown);
        if (list == null)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected == null || !selected.transform.IsChildOf(list.transform))
        {
            return;
        }

        if (selected == lastScrolledSelection)
        {
            return;
        }

        ScrollToListItem(list, selected);
    }

    void ScrollToListItem(GameObject list, GameObject selected)
    {
        ScrollRect scrollRect = list.GetComponent<ScrollRect>();
        RectTransform itemRect = selected.transform as RectTransform;
        if (scrollRect == null || itemRect == null)
        {
            return;
        }

        ScrollChildIntoView(scrollRect, itemRect);
        lastScrolledSelection = selected;
    }

    static void ScrollChildIntoView(ScrollRect scrollRect, RectTransform child)
    {
        if (scrollRect.viewport == null || scrollRect.content == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        RectTransform viewport = scrollRect.viewport;
        RectTransform content = scrollRect.content;

        Vector3[] itemCorners = new Vector3[4];
        child.GetWorldCorners(itemCorners);

        Vector3[] viewportCorners = new Vector3[4];
        viewport.GetWorldCorners(viewportCorners);

        float itemMinY = itemCorners[0].y;
        float itemMaxY = itemCorners[1].y;
        float viewportMinY = viewportCorners[0].y;
        float viewportMaxY = viewportCorners[1].y;

        float delta = 0f;
        if (itemMinY < viewportMinY)
        {
            delta = viewportMinY - itemMinY;
        }
        else if (itemMaxY > viewportMaxY)
        {
            delta = viewportMaxY - itemMaxY;
        }

        if (Mathf.Approximately(delta, 0f))
        {
            return;
        }

        Vector3 contentPosition = content.position;
        content.position = new Vector3(contentPosition.x, contentPosition.y + delta, contentPosition.z);
    }

    static GameObject FindDropdownList(TMP_Dropdown target)
    {
        if (target == null || !target.IsExpanded)
        {
            return null;
        }

        Canvas rootCanvas = target.GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            return null;
        }

        Transform[] transforms = rootCanvas.GetComponentsInChildren<Transform>(false);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate.name == DropdownListName && candidate.gameObject.activeInHierarchy)
            {
                return candidate.gameObject;
            }
        }

        return null;
    }

    static void WireListItemNavigation(GameObject list)
    {
        Toggle[] toggles = list.GetComponentsInChildren<Toggle>(false);
        Selectable previous = null;

        foreach (Toggle toggle in toggles)
        {
            if (toggle == null)
            {
                continue;
            }

            Navigation navigation = toggle.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = previous;
            navigation.selectOnDown = null;
            navigation.selectOnLeft = null;
            navigation.selectOnRight = null;
            toggle.navigation = navigation;

            if (previous != null)
            {
                Navigation previousNavigation = previous.navigation;
                previousNavigation.selectOnDown = toggle;
                previous.navigation = previousNavigation;
            }

            previous = toggle;
        }
    }

    static void DisableScrollbarNavigation(GameObject list)
    {
        Scrollbar scrollbar = list.GetComponentInChildren<Scrollbar>(true);
        if (scrollbar == null)
        {
            return;
        }

        Navigation navigation = scrollbar.navigation;
        navigation.mode = Navigation.Mode.None;
        scrollbar.navigation = navigation;
    }
}
