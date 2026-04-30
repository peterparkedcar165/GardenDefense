using UnityEngine;
using UnityEngine.EventSystems;

// This script goes on each path row in the UI.
// It detects when the mouse enters or exits that row,
// and tells the tooltip what to display.
public class PathHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Set this in the Inspector to tell the script which path this row belongs to
    public enum PathType { Path1, Path2, Path3 }
    public PathType pathType;

    // Called automatically by Unity when the mouse enters this UI element
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Get the currently selected plant from the upgrade UI
        Plant plant = PlantUpgradeUI.instance?.GetSelectedPlant();

        // If no plant is selected, do nothing
        if (plant == null) return;

        // Pick the right description based on which path this row is
        string description = "";
        if (pathType == PathType.Path1)
            description = plant.GetPath1Description();
        else if (pathType == PathType.Path2)
            description = plant.GetPath2Description();
        else if (pathType == PathType.Path3)
            description = plant.GetPath3Description();

        // Show the tooltip with that description
        PlantUpgradeUI.instance.ShowTooltip(description);
    }

    // Called automatically by Unity when the mouse leaves this UI element
    public void OnPointerExit(PointerEventData eventData)
    {
        // Hide the tooltip when the mouse moves away
        PlantUpgradeUI.instance?.HideTooltip();
    }
}
