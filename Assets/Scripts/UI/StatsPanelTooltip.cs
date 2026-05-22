using UnityEngine;
using UnityEngine.EventSystems;

public class StatsPanelTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool isHovering = false;

    // Tune this until values sit at the right edge for your font/panel size
    private const int Width = 30;

    private const string Fire   = "<color=orange>";
    private const string Water  = "<color=#4FC3F7>";
    private const string Nature = "<color=green>";
    private const string Ice    = "<color=#00FFFF>";
    private const string Poison = "<color=purple>";
    private const string Wind   = "<color=#B2EBF2>";
    private const string Magic    = "<color=#FFB6C1>";
    private const string Physical = "<color=#A0522D>";
    private const string Gold     = "<color=white>";
    private const string End      = "</color>";

    // Dot leader: label left, dots fill gap, value right
    private static string D(string label, string plain, string formatted)
    {
        int dots = Mathf.Max(2, Width - label.Length - plain.Length);
        return $"{label}{new string('.', dots)}{formatted}";
    }

    private string Col(float current, float baseValue, string formatted)
    {
        if (current > baseValue) return $"<color=green>{formatted}</color>";
        if (current < baseValue) return $"<color=red>{formatted}</color>";
        return formatted;
    }

    private void Update()
    {
        if (!isHovering) return;

        Entity entity = (Entity)PlantUpgradeUI.instance?.GetSelectedPlant()
                     ?? PlantUpgradeUI.instance?.GetSelectedInsect();
        if (entity != null)
            PlantUpgradeUI.instance.ShowTooltip(BuildEntityText(entity));
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovering = true;

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        PlantUpgradeUI.instance?.HideTooltip();
    }

    private string BuildEntityText(Entity e)
    {
        var sb = new System.Text.StringBuilder();

        // --- Core ---
        sb.AppendLine("<size=+8><color=white><b><u>Core</u></b></color></size>");
        ElemLine(sb, Nature, "Attack Damage:",  $"{e.attackDamage:F0}",  e.attackDamage,  e.baseAttackDamage);
        ElemLine(sb, Nature, "Attack Speed:",   $"{e.attackSpeed:F2}",   e.attackSpeed,   e.baseAttackSpeed);
        ElemLine(sb, Nature, "Attack Range:",   $"{e.attackRange:F1}",   e.attackRange,   e.baseAttackRange);
        if (e is Insect insect)
            Line(sb, "Move Speed:", $"{insect.movementSpeed:F2}", insect.movementSpeed, insect.baseMovementSpeed);
        ElemLine(sb, Magic, "Magic Power:", $"{e.magicPower:F0}", e.magicPower, e.baseMagicPower);

        // --- Offense ---
        sb.AppendLine();
        sb.AppendLine("<size=+8><color=white><b><u>Offense</u></b></color></size>");
        ElemLine(sb, Gold, "Crit Chance:", $"{e.criticalChance * 100:F1}%", e.criticalChance, e.baseCriticalChance);
        ElemLine(sb, Gold, "Crit Damage:", $"{e.criticalDamage * 100:F0}%", e.criticalDamage, e.baseCriticalDamage);
        Line(sb, "Elemental Power:",  $"{e.elementalPower * 100:F0}%", e.elementalPower,  e.baseElementalPower);
        Line(sb, "Damage Over Time:", $"{e.dotDamage * 100:F0}%",      e.dotDamage,       e.baseDotDamage);
        if (e is Shooter shooter)
            ElemLine(sb, Nature, "Piercing:", $"{shooter.piercing}", shooter.piercing, shooter.basePiercing);

        sb.AppendLine();
        ElemLine(sb, Physical, "Physical Damage:", $"{e.physicalDamage * 100:F0}%", e.physicalDamage, e.basePhysicalDamage);
        ElemLine(sb, Magic,    "Magic Damage:",    $"{e.magicDamage * 100:F0}%",    e.magicDamage,    e.baseMagicDamage);
        ElemLine(sb, Fire,   "Fire Damage:",   $"{e.fireDamage * 100:F0}%",   e.fireDamage,   e.baseFireDamage);
        ElemLine(sb, Water,  "Water Damage:",  $"{e.waterDamage * 100:F0}%",  e.waterDamage,  e.baseWaterDamage);
        ElemLine(sb, Nature, "Nature Damage:", $"{e.natureDamage * 100:F0}%", e.natureDamage, e.baseNatureDamage);
        ElemLine(sb, Ice,    "Ice Damage:",    $"{e.iceDamage * 100:F0}%",    e.iceDamage,    e.baseIceDamage);
        ElemLine(sb, Poison, "Poison Damage:", $"{e.poisonDamage * 100:F0}%", e.poisonDamage, e.basePoisonDamage);
        ElemLine(sb, Wind,   "Wind Damage:",   $"{e.windDamage * 100:F0}%",   e.windDamage,   e.baseWindDamage);

        // --- Defenses ---
        sb.AppendLine();
        sb.AppendLine("<size=+8><color=white><b><u>Defenses</u></b></color></size>");
        ElemLine(sb, Physical, "Physical Resistance:", $"{e.physicalResistance * 100:F0}%", e.physicalResistance, e.basePhysicalResistance);
        ElemLine(sb, Magic,    "Magic Resistance:",    $"{e.magicResistance * 100:F0}%",    e.magicResistance,    e.baseMagicResistance);
        sb.AppendLine();
        ElemLine(sb, Fire,   "Fire Resistance:",   $"{e.fireResistance * 100:F0}%",   e.fireResistance,   e.baseFireResistance);
        ElemLine(sb, Water,  "Water Resistance:",  $"{e.waterResistance * 100:F0}%",  e.waterResistance,  e.baseWaterResistance);
        ElemLine(sb, Nature, "Nature Resistance:", $"{e.natureResistance * 100:F0}%", e.natureResistance, e.baseNatureResistance);
        ElemLine(sb, Ice,    "Ice Resistance:",    $"{e.iceResistance * 100:F0}%",    e.iceResistance,    e.baseIceResistance);
        ElemLine(sb, Poison, "Poison Resistance:", $"{e.poisonResistance * 100:F0}%", e.poisonResistance, e.basePoisonResistance);
        ElemLine(sb, Wind,   "Wind Resistance:",   $"{e.windResistance * 100:F0}%",   e.windResistance,   e.baseWindResistance);

        // --- Buffs / Debuffs ---
        var buffs   = e.activeEffects.FindAll(ef => ef.effectType == StatusEffect.Type.positive);
        var debuffs = e.activeEffects.FindAll(ef => ef.effectType == StatusEffect.Type.negative);

        if (buffs.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<size=+8><color=white><b><u>Buffs</u></b></color></size>");
            foreach (var ef in buffs)
                sb.AppendLine($"({FormatDuration(ef.duration)}) <b>{ef.GetName()}</b> [<color=green><b>{ef.level}</b></color>]");
        }

        if (debuffs.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<size=+8><color=white><b><u>Debuffs</u></b></color></size>");
            foreach (var ef in debuffs)
                sb.AppendLine($"({FormatDuration(ef.duration)}) <b>{ef.GetName()}</b> [<color=green><b>{ef.level}</b></color>]");
        }

        return sb.ToString().TrimEnd();
    }

    private void Line(System.Text.StringBuilder sb, string label, string plain, float current, float baseVal)
    {
        sb.AppendLine(D(label, plain, $"<b>{Col(current, baseVal, plain)}</b>"));
    }

    private void ElemLine(System.Text.StringBuilder sb, string colorTag, string label, string plain, float current, float baseVal)
    {
        int dots = Mathf.Max(2, Width - label.Length - plain.Length);
        sb.AppendLine($"{colorTag}{label}{End}{new string('.', dots)}<b>{Col(current, baseVal, plain)}</b>");
    }

    private static string FormatDuration(float seconds)
    {
        if (seconds >= float.MaxValue / 2f) return "--:--";
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:D2}:{total % 60:D2}";
    }
}
