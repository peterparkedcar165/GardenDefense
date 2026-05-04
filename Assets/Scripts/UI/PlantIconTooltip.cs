using UnityEngine;
using UnityEngine.EventSystems;

public class PlantIconTooltip : MonoBehaviour,IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Plant plant = PlantUpgradeUI.instance.GetSelectedPlant();
        if (plant == null) return;

        string text = $"{plant.GetName()}\n\n" + $"{plant.GetDescription()}\n\n<b>Affinity</b>:\n\n{plant.GetElement()}\n{plant.GetElementDescription()}";

        PlantUpgradeUI.instance.ShowTooltip(text);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlantUpgradeUI.instance.HideTooltip();
    }
}
