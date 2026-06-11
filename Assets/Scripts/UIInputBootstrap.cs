using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public static class UIInputBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void WireUiInputModule()
    {
        Refresh();
    }

    public static void Refresh()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;

        InputSystemUIInputModule uiModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (uiModule == null)
            return;

        PlayerInput playerInput = Object.FindAnyObjectByType<PlayerInput>();
        InputActionAsset asset = playerInput != null ? playerInput.actions : null;
        if (asset == null)
            return;

        uiModule.actionsAsset = asset;
        uiModule.move = ActionRef(asset, "UI/Navigate");
        uiModule.submit = ActionRef(asset, "UI/Submit");
        uiModule.cancel = ActionRef(asset, "UI/Cancel");
        uiModule.point = ActionRef(asset, "UI/Point");
        uiModule.leftClick = ActionRef(asset, "UI/Click");
        uiModule.rightClick = ActionRef(asset, "UI/RightClick");
        uiModule.middleClick = ActionRef(asset, "UI/MiddleClick");
        uiModule.scrollWheel = ActionRef(asset, "UI/ScrollWheel");
        uiModule.trackedDevicePosition = ActionRef(asset, "UI/TrackedDevicePosition");
        uiModule.trackedDeviceOrientation = ActionRef(asset, "UI/TrackedDeviceOrientation");

        EnableActionReference(uiModule.move);
        EnableActionReference(uiModule.submit);
        EnableActionReference(uiModule.cancel);
        EnableActionReference(uiModule.point);
        EnableActionReference(uiModule.leftClick);
        EnableActionReference(uiModule.rightClick);
        EnableActionReference(uiModule.middleClick);
        EnableActionReference(uiModule.scrollWheel);
    }

    private static void EnableActionReference(InputActionReference actionReference)
    {
        if (actionReference?.action != null && !actionReference.action.enabled)
        {
            actionReference.action.Enable();
        }
    }

    private static InputActionReference ActionRef(InputActionAsset asset, string actionPath)
    {
        InputAction action = asset.FindAction(actionPath, throwIfNotFound: false);
        return action != null ? InputActionReference.Create(action) : null;
    }
}
