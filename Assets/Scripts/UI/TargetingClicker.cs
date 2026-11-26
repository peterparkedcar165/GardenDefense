using UnityEngine;
using UnityEngine.EventSystems;

// the plant info panel's targeting display without its arrows (Tommy, 2026-11-26): a left click on it
// steps to the next targeting mode, a right click back, both wrapping round, and while the pointer is
// over it the panel's tooltip lists every mode (PlantUpgradeUI.ShowTargetingTooltip), kept up to date
// as the clicks change it
public class TargetingClicker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public PlantUpgradeUI ui;
    private bool hovered;

    // the pointer is over it (and it's shown): the info panel then doesn't close on a right click
    public bool Hovered => hovered && isActiveAndEnabled;

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        if (ui != null) ui.ShowTargetingTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        if (ui != null) ui.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ui == null) return;
        if (eventData.button == PointerEventData.InputButton.Left) ui.OnTargetingNextClicked();
        else if (eventData.button == PointerEventData.InputButton.Right) ui.OnTargetingPrevClicked();
        else return;
        if (hovered) ui.ShowTargetingTooltip();
    }

    private void OnDisable()
    {
        if (hovered && ui != null) ui.HideTooltip();
        hovered = false;
    }
}
