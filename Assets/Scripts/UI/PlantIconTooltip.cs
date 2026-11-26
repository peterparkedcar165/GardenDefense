using UnityEngine;
using UnityEngine.EventSystems;

public class PlantIconTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        TutorialManager.instance?.NotifyEvent(TutorialEvents.HoverPlantInfoIcon);

        // the name on top and the description in framed sections, like the Attack, Passive and Skill
        // tooltips (Tommy, 2026-11-26): a plant's description and its affinity; an insect's short
        // description, its ability, and its traits and aggressivity
        Plant plant = PlantUpgradeUI.instance?.GetSelectedPlant();
        if (plant != null)
        {
            PlantUpgradeUI.instance.ShowSectionedTooltip(plant.GetName(), new[]
            {
                plant.GetDescription(),
                $"<b>Affinity</b>:\n\n{plant.GetElement()}\n{plant.GetElementDescription()}",
            });
            return;
        }

        Insect insect = PlantUpgradeUI.instance?.GetSelectedInsect();
        if (insect != null)
            PlantUpgradeUI.instance.ShowSectionedTooltip(insect.GetName(), insect.GetDescriptionSections());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlantUpgradeUI.instance?.HideTooltip();
    }
}
