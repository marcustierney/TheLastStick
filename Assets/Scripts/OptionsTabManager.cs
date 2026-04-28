using UnityEngine;

public class OptionsTabManager : MonoBehaviour
{
    [SerializeField] CanvasGroup graphicsPanel;
    [SerializeField] CanvasGroup soundPanel;
    [SerializeField] CanvasGroup controlsPanel;
    [SerializeField] CanvasGroup accessPanel;

    void OnEnable()
    {
        ShowTab(graphicsPanel);
    }

    public void ShowGraphics()  => ShowTab(graphicsPanel);
    public void ShowSound()     => ShowTab(soundPanel);
    public void ShowControls()  => ShowTab(controlsPanel);
    public void ShowAccess()   => ShowTab(accessPanel);

    void ShowTab(CanvasGroup selected)
    {
        SetPanel(graphicsPanel,  selected == graphicsPanel);
        SetPanel(soundPanel,     selected == soundPanel);
        SetPanel(controlsPanel,  selected == controlsPanel);
        SetPanel(accessPanel,   selected == accessPanel);
    }

    void SetPanel(CanvasGroup cg, bool on)
    {
        cg.alpha          = on ? 1f : 0f;
        cg.interactable   = on;
        cg.blocksRaycasts = on;
    }
}