using UnityEngine;
using UnityEngine.EventSystems;

public class StatsPanelTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool isHovering = false;

    private string Color(float current, float baseValue, string formatted)
    {
        if (current > baseValue) return $"<color=green>{formatted}</color>";
        if (current < baseValue) return $"<color=red>{formatted}</color>";
        return formatted;
    }

    private void Update()
    {
        if (!isHovering) return;

        Plant plant = PlantUpgradeUI.instance?.GetSelectedPlant();
        if (plant == null) return;

        PlantUpgradeUI.instance.ShowTooltip(BuildText(plant));
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

    private string BuildText(Plant plant)
    {
        return
        $"<b>Stats</b>\n\n" +
        $"Attack Damage:   <b>{Color(plant.attackDamage, plant.baseAttackDamage, $"{plant.attackDamage:F0}")}</b>\n" +
        $"Attack Speed:    <b>{Color(plant.attackSpeed, plant.baseAttackSpeed, $"{plant.attackSpeed:F2}")}</b>\n" +
        $"Attack Range:    <b>{Color(plant.attackRange, plant.baseAttackRange, $"{plant.attackRange:F1}")}</b>\n" +
        $"Crit Chance:     <b>{Color(plant.criticalChance, plant.baseCriticalChance, $"{plant.criticalChance * 100:F1}%")}</b>\n" +
        $"Crit Damage:     <b>{Color(plant.criticalDamage, plant.baseCriticalDamage, $"{plant.criticalDamage * 100:F0}%")}</b>\n\n" +
        $"DoT Damage:      <b>{Color(plant.dotDamage, plant.baseDotDamage, $"{plant.dotDamage * 100:F0}%")}</b>\n" +
        $"Elemental Power: <b>{Color(plant.elementalPower, plant.baseElementalPower, $"{plant.elementalPower * 100:F0}%")}</b>\n\n" +
        $"Fire Damage:     <b>{Color(plant.fireDamage, plant.baseFireDamage, $"{plant.fireDamage * 100:F0}%")}</b>\n" +
        $"Water Damage:    <b>{Color(plant.waterDamage, plant.baseWaterDamage, $"{plant.waterDamage * 100:F0}%")}</b>\n" +
        $"Nature Damage:   <b>{Color(plant.natureDamage, plant.baseNatureDamage, $"{plant.natureDamage * 100:F0}%")}</b>\n" +
        $"Ice Damage:      <b>{Color(plant.iceDamage, plant.baseIceDamage, $"{plant.iceDamage * 100:F0}%")}</b>\n" +
        $"Poison Damage:   <b>{Color(plant.poisonDamage, plant.basePoisonDamage, $"{plant.poisonDamage * 100:F0}%")}</b>\n" +
        $"Wind Damage:     <b>{Color(plant.windDamage, plant.baseWindDamage, $"{plant.windDamage * 100:F0}%")}</b>";
    }
}
