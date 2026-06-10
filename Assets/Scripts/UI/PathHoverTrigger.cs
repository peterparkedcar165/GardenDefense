using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PathHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum PathType { Path1, Path2, Path3 }
    public PathType pathType;

    private bool isHovering = false;

    private void Update()
    {
        if (!isHovering) return;

        Plant plant = PlantUpgradeUI.instance?.GetSelectedPlant();
        if (plant == null) return;

        bool details = Keyboard.current.shiftKey.isPressed;
        string description = "";
        if (pathType == PathType.Path1)
            description = plant.GetPath1Description(details);
        else if (pathType == PathType.Path2)
            description = plant.GetPath2Description(details);
        else if (pathType == PathType.Path3)
            description = plant.GetPath3Description(details);

        PlantUpgradeUI.instance.ShowTooltip(description);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        PlantUpgradeUI.instance?.HideTooltip();
    }
}
