using TMPro;
using UnityEngine;

public class StatsPanelTooltip : MonoBehaviour
{
    [SerializeField] private TMP_Text statsText;

    private const int Width = 30;

    private const string Fire     = "<color=orange>";
    private const string Water    = "<color=#4FC3F7>";
    private const string Grass   = "<color=green>";
    private const string Ice      = "<color=#00FFFF>";
    private const string Poison   = "<color=purple>";
    private const string Wind     = "<color=#B2EBF2>";
    private const string Ground   = "<color=#79391F>";
    private const string Effect   = "<color=#B3FFFF>";
    private const string Magic    = "<color=#FFB6C1>";
    private const string Physical = "<color=#A0522D>";
    private const string Crit     = "<color=#FFD700>";
    private const string HealCol  = "<color=#FF6B81>";
    private const string End      = "</color>";

    private static string D(string label, string plain, string formatted)
    {
        int visibleLen = 0;
        bool inTag = false;
        foreach (char c in label)
        {
            if      (c == '<') inTag = true;
            else if (c == '>') inTag = false;
            else if (!inTag)   visibleLen++;
        }
        int dots = Mathf.Max(2, Width - visibleLen - plain.Length);
        return $"{label}{new string('.', dots)}{formatted}";
    }

    private string Col(float current, float baseValue, string formatted)
    {
        if (current > baseValue) return $"<color=green>{formatted}</color>";
        if (current < baseValue) return $"<color=red>{formatted}</color>";
        return formatted;
    }

    private string lastText;

    private void Update()
    {
        if (statsText == null) return;

        Entity entity = (Entity)PlantUpgradeUI.instance?.GetSelectedPlant()
                     ?? PlantUpgradeUI.instance?.GetSelectedInsect();

        string newText = entity != null ? BuildCore(entity) : "";
        if (newText == lastText) return;
        lastText = newText;
        statsText.text = newText;

        // same pattern as StatusEffectPanel, panel hugs the text but never exceeds the screen
        Canvas.ForceUpdateCanvases();
        RectTransform panelRect = GetComponent<RectTransform>();
        float maxHeight = Screen.height * 0.975f;
        float height = Mathf.Min(statsText.preferredHeight + 40f, maxHeight);
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private string BuildCore(Entity e)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<size=+8><color=white><b><u>Core</u></b></color></size>");
        ElemLine(sb, HealCol, "Max Health:", $"{e.maxHealth:F0}", e.maxHealth, e.baseMaxHealth);
        ElemLine(sb, Grass, "Attack Damage:",  $"{e.attackDamage:F0}",  e.attackDamage,  e.baseAttackDamage);
        ElemLine(sb, Grass, "Attack Speed:",   $"{e.attackSpeed:F2}",   e.attackSpeed,   e.baseAttackSpeed);
        ElemLine(sb, Grass, "Attack Range:",   $"{e.attackRange:F1}",   e.attackRange,   e.baseAttackRange);
        if (e is Insect insect)
            Line(sb, "Move Speed:", $"{insect.movementSpeed:F2}", insect.movementSpeed, insect.baseMovementSpeed);
        ElemLine(sb, Crit, "Crit Chance:", $"{e.criticalChance * 100:F1}%", e.criticalChance, e.baseCriticalChance);
        ElemLine(sb, Crit, "Crit Damage:", $"{e.criticalDamage * 100:F0}%", e.criticalDamage, e.baseCriticalDamage);
        ElemLine(sb, Effect, "Bonus Effect Chance:", $"{e.bonusEffectChance * 100:F0}%", e.bonusEffectChance, e.baseBonusEffectChance);
        ElemLine(sb, Effect, "Elemental Effect Chance:", $"{e.elementalEffectChance * 100:F0}%", e.elementalEffectChance, e.baseElementalEffectChance);
        sb.AppendLine();
        ElemLine(sb, Magic, "Maximum Damage:", $"{e.maximumDamage * 100:F0}%", e.maximumDamage, e.baseMaximumDamage);
        ElemLine(sb, Magic, "Minimum Damage:", $"{e.minimumDamage * 100:F0}%", e.minimumDamage, e.baseMinimumDamage);
        sb.AppendLine();
        ElemLine(sb, Magic, "Magic Power:", $"{e.magicPower:F0}", e.magicPower, e.baseMagicPower);
        sb.AppendLine();
        Line(sb, "<color=#00CED1>Armor:</color>",       $"{e.armor}",      e.armor,      e.baseArmor);
        Line(sb, "<color=#FF69B4>Magic Armor:</color>", $"{e.magicArmor}", e.magicArmor, e.baseMagicArmor);
        sb.AppendLine();
        ElemLine(sb, Physical, "Armor Penetration:",     $"{e.armorPenFlat:F0}",           e.armorPenFlat,    e.baseArmorPenFlat);
        ElemLine(sb, Physical, "Armor Shred:",       $"{e.armorPenPercent * 100:F0}%", e.armorPenPercent, e.baseArmorPenPercent);
        ElemLine(sb, Magic,    "Magic Penetration:", $"{e.magicPenFlat:F0}",           e.magicPenFlat,    e.baseMagicPenFlat);
        ElemLine(sb, Magic,    "Magic Armor Shred:", $"{e.magicPenPercent * 100:F0}%", e.magicPenPercent, e.baseMagicPenPercent);
        if (e is Shooter shooter)
            ElemLine(sb, Grass, "Piercing:", $"{shooter.piercing}", shooter.piercing, shooter.basePiercing);
        sb.AppendLine();
        Line(sb,              "Evasion:",    $"{e.evasion * 100:F1}%",    e.evasion,    e.baseEvasion);
        ElemLine(sb, Grass,  "Accuracy:",   $"{e.accuracy * 100:F1}%",   e.accuracy,   e.baseAccuracy);
        Line(sb, "<color=grey>Tenacity:</color>", $"{e.tenacity * 100:F0}%", e.tenacity, e.baseTenacity);
        ElemLine(sb, HealCol, "Heals & Shield Bonus:",    $"{e.healingBonus * 100:F0}%",    e.healingBonus,    e.baseHealingBonus);
        sb.AppendLine();
        ElemLine(sb, Grass,   "Elemental Affinity:",  $"{e.elementalAffinity * 100:F0}%",  e.elementalAffinity,  e.baseelementalAffinity);
        ElemLine(sb, Fire,   "Fire Damage:",   $"{e.fireDamage * 100:F0}%",   e.fireDamage,   e.baseFireDamage);
        ElemLine(sb, Water,  "Water Damage:",  $"{e.waterDamage * 100:F0}%",  e.waterDamage,  e.baseWaterDamage);
        ElemLine(sb, Grass, "Grass Damage:", $"{e.grassDamage * 100:F0}%", e.grassDamage, e.baseGrassDamage);
        ElemLine(sb, Ice,    "Ice Damage:",    $"{e.iceDamage * 100:F0}%",    e.iceDamage,    e.baseIceDamage);
        ElemLine(sb, Poison, "Poison Damage:", $"{e.poisonDamage * 100:F0}%", e.poisonDamage, e.basePoisonDamage);
        ElemLine(sb, Wind,   "Wind Damage:",   $"{e.windDamage * 100:F0}%",   e.windDamage,   e.baseWindDamage);
        ElemLine(sb, Ground, "Ground Damage:", $"{e.groundDamage * 100:F0}%", e.groundDamage, e.baseGroundDamage);
        return sb.ToString().TrimEnd();
    }

    private void Line(System.Text.StringBuilder sb, string label, string plain, float current, float baseVal)
    {
        sb.AppendLine(D(label, plain, Col(current, baseVal, $"<b>{plain}</b>")));
    }

    private void ElemLine(System.Text.StringBuilder sb, string colorTag, string label, string plain, float current, float baseVal)
    {
        int dots = Mathf.Max(2, Width - label.Length - plain.Length);
        sb.AppendLine($"{colorTag}{label}{End}{new string('.', dots)}{Col(current, baseVal, $"<b>{plain}</b>")}");
    }
}
