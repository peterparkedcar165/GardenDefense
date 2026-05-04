using UnityEngine;
using UnityEngine.EventSystems;

public class PlantIconTooltip : MonoBehaviour,IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Plant plant = PlantUpgradeUI.instance.GetSelectedPlant();
        if (plant == null) return;

        string text = $"{plant.GetName()}\n\n" + $"{plant.GetDescription()}\n\n" + $"Attack:\n{plant.GetAttackDescription()}\n\n" + $"Passive:\n{plant.GetPassiveDescription()}\n\n" + $"Skill (NOT IMPLEMENTED YET):\n{plant.GetSkillDesription()}\n\n";

        PlantUpgradeUI.instance.ShowTooltip(text);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlantUpgradeUI.instance.HideTooltip();
    }
}
