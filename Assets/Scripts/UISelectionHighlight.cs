using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class UISelectionHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private PointerEventData pointerEventData;
    private bool hoverApplied;
    private bool isSlider;

    public void Configure(Color color, float width, Sprite borderSprite)
    {
        // Kept for compatibility with existing UIFocusGuard wiring.
    }

    private void Awake()
    {
        isSlider = GetComponent<Slider>() != null;
        InitPointerEventData();
    }

    // EventSystem is destroyed and recreated on scene reload. Re-initialize
    // pointerEventData so it references the current EventSystem, not a
    // destroyed one — a stale reference breaks Slider navigation silently.
    private void OnEnable()
    {
        InitPointerEventData();
    }

    private void InitPointerEventData()
    {
        if (EventSystem.current != null)
        {
            pointerEventData = new PointerEventData(EventSystem.current);
        }
    }

    public void OnSelect(BaseEventData eventData) => ApplyHover(true);
    public void OnDeselect(BaseEventData eventData) => ApplyHover(false);

    private void Update()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        // Re-initialize if EventSystem was replaced (e.g. after scene reload)
        if (pointerEventData == null || pointerEventData.currentInputModule == null)
        {
            InitPointerEventData();
        }

        bool isSelected = EventSystem.current.currentSelectedGameObject == gameObject;

        // Only act on change — avoids redundant pointer events every frame
        if (isSelected == hoverApplied)
        {
            return;
        }

        ApplyHover(isSelected);
    }

    private void ApplyHover(bool shouldHover)
    {
        // Sliders manage their own pointer state internally. Injecting fake
        // pointer events corrupts their drag handling and breaks axis navigation.
        if (isSlider)
        {
            return;
        }

        if (EventSystem.current == null)
        {
            return;
        }

        if (pointerEventData == null)
        {
            InitPointerEventData();
            if (pointerEventData == null)
            {
                return;
            }
        }

        pointerEventData.pointerEnter = gameObject;
        pointerEventData.selectedObject = gameObject;
        pointerEventData.pointerPressRaycast = new RaycastResult { gameObject = gameObject };
        pointerEventData.pointerCurrentRaycast = new RaycastResult { gameObject = gameObject };

        if (shouldHover)
        {
            ExecuteEvents.Execute(gameObject, pointerEventData, ExecuteEvents.pointerEnterHandler);
            hoverApplied = true;
        }
        else
        {
            ExecuteEvents.Execute(gameObject, pointerEventData, ExecuteEvents.pointerExitHandler);
            hoverApplied = false;
        }
    }
}