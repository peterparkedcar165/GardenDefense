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
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("<b>Offense</b>\n");
        sb.AppendLine($"Attack Damage:   <b>{Color(plant.attackDamage, plant.baseAttackDamage, $"{plant.attackDamage:F0}")}</b>");
        sb.AppendLine($"Attack Speed:    <b>{Color(plant.attackSpeed, plant.baseAttackSpeed, $"{plant.attackSpeed:F2}")}</b>");
        sb.AppendLine($"Attack Range:    <b>{Color(plant.attackRange, plant.baseAttackRange, $"{plant.attackRange:F1}")}</b>");
        sb.AppendLine($"Crit Chance:     <b>{Color(plant.criticalChance, plant.baseCriticalChance, $"{plant.criticalChance * 100:F1}%")}</b>");
        sb.AppendLine($"Crit Damage:     <b>{Color(plant.criticalDamage, plant.baseCriticalDamage, $"{plant.criticalDamage * 100:F0}%")}</b>");
        sb.AppendLine($"Magic Power:     <b>{Color(plant.magicPower, plant.baseMagicPower, $"{plant.magicPower * 100:F0}%")}</b>");
        sb.AppendLine($"DoT Damage:      <b>{Color(plant.dotDamage, plant.baseDotDamage, $"{plant.dotDamage * 100:F0}%")}</b>");
        sb.AppendLine($"Elemental Power: <b>{Color(plant.elementalPower, plant.baseElementalPower, $"{plant.elementalPower * 100:F0}%")}</b>");

        sb.AppendLine();
        sb.AppendLine("<b>Elemental Damage</b>\n");
        sb.AppendLine($"Fire:    <b>{Color(plant.fireDamage,   plant.baseFireDamage,   $"{plant.fireDamage * 100:F0}%")}</b>");
        sb.AppendLine($"Water:   <b>{Color(plant.waterDamage,  plant.baseWaterDamage,  $"{plant.waterDamage * 100:F0}%")}</b>");
        sb.AppendLine($"Nature:  <b>{Color(plant.natureDamage, plant.baseNatureDamage, $"{plant.natureDamage * 100:F0}%")}</b>");
        sb.AppendLine($"Ice:     <b>{Color(plant.iceDamage,    plant.baseIceDamage,    $"{plant.iceDamage * 100:F0}%")}</b>");
        sb.AppendLine($"Poison:  <b>{Color(plant.poisonDamage, plant.basePoisonDamage, $"{plant.poisonDamage * 100:F0}%")}</b>");
        sb.AppendLine($"Wind:    <b>{Color(plant.windDamage,   plant.baseWindDamage,   $"{plant.windDamage * 100:F0}%")}</b>");

        sb.AppendLine();
        sb.AppendLine("<b>Defenses</b>\n");
        sb.AppendLine($"Physical Resistance: <b>{Color(plant.physicalResistance, plant.basePhysicalResistance, $"{plant.physicalResistance * 100:F0}%")}</b>");
        sb.AppendLine($"Magic Resistance:    <b>{Color(plant.magicResistance, plant.baseMagicResistance, $"{plant.magicResistance * 100:F0}%")}</b>");
        sb.AppendLine($"Fire Resistance:     <b>{Color(plant.fireResistance,   plant.baseFireResistance,   $"{plant.fireResistance * 100:F0}%")}</b>");
        sb.AppendLine($"Water Resistance:    <b>{Color(plant.waterResistance,  plant.baseWaterResistance,  $"{plant.waterResistance * 100:F0}%")}</b>");
        sb.AppendLine($"Nature Resistance:   <b>{Color(plant.natureResistance, plant.baseNatureResistance, $"{plant.natureResistance * 100:F0}%")}</b>");
        sb.AppendLine($"Ice Resistance:      <b>{Color(plant.iceResistance,    plant.baseIceResistance,    $"{plant.iceResistance * 100:F0}%")}</b>");
        sb.AppendLine($"Poison Resistance:   <b>{Color(plant.poisonResistance, plant.basePoisonResistance, $"{plant.poisonResistance * 100:F0}%")}</b>");
        sb.AppendLine($"Wind Resistance:     <b>{Color(plant.windResistance,   plant.baseWindResistance,   $"{plant.windResistance * 100:F0}%")}</b>");

        var buffs   = plant.activeEffects.FindAll(e => e.effectType == StatusEffect.Type.positive);
        var debuffs = plant.activeEffects.FindAll(e => e.effectType == StatusEffect.Type.negative);

        if (buffs.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>Buffs</b>\n");
            foreach (var e in buffs)
                sb.AppendLine($"({FormatDuration(e.duration)}) <b>{e.GetName()}</b>");
        }

        if (debuffs.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>Debuffs</b>\n");
            foreach (var e in debuffs)
                sb.AppendLine($"({FormatDuration(e.duration)}) <b>{e.GetName()}</b>");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatDuration(float seconds)
    {
        if (seconds >= float.MaxValue / 2f) return "--:--";
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:D2}:{total % 60:D2}";
    }
}
