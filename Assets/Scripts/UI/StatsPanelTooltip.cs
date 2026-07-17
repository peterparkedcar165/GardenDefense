using TMPro;
using UnityEngine;

public enum StatsSection { Core, Offense, Defense, Utility }

public class StatsPanelTooltip : MonoBehaviour
{
    [SerializeField] private StatsSection section;
    [SerializeField] private TMP_Text statsText;

    private const int Width = 30;

    private const string Fire     = "<color=orange>";
    private const string Water    = "<color=#4FC3F7>";
    private const string Grass   = "<color=green>";
    private const string Ice      = "<color=#00FFFF>";
    private const string Poison   = "<color=purple>";
    private const string Wind     = "<color=#B2EBF2>";
    private const string Ground   = "<color=#79391F>";
    private const string Magic    = "<color=#FFB6C1>";
    private const string Physical = "<color=#A0522D>";
    private const string DoT      = "<color=grey>";
    private const string Gold     = "<color=white>";
    private const string Crit     = "<color=#FFD700>";
    private const string HealCol  = "<color=#FF6B81>";
    private const string Passive  = "<color=#FFB347>";
    private const string Blue     = "<color=#6495ED>";
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

    private string ColInv(float current, float baseValue, string formatted)
    {
        if (current < baseValue) return $"<color=green>{formatted}</color>";
        if (current > baseValue) return $"<color=red>{formatted}</color>";
        return formatted;
    }

    private void Update()
    {
        if (statsText == null) return;

        Entity entity = (Entity)PlantUpgradeUI.instance?.GetSelectedPlant()
                     ?? PlantUpgradeUI.instance?.GetSelectedInsect();

        statsText.text = entity != null ? BuildEntityText(entity) : "";
    }

    private string BuildEntityText(Entity e)
    {
        return section switch
        {
            StatsSection.Core    => BuildCore(e),
            StatsSection.Offense => BuildOffense(e),
            StatsSection.Defense => BuildDefense(e),
            StatsSection.Utility => BuildUtility(e),
            _                    => ""
        };
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
        if (e is Plant plant)
            ElemLine(sb, Grass, "Projectile Speed:", $"{e.projectileSpeed:F1}", e.projectileSpeed, e.baseProjectileSpeed);
        ElemLine(sb, Crit, "Crit Chance:", $"{e.criticalChance * 100:F1}%", e.criticalChance, e.baseCriticalChance);
        ElemLine(sb, Crit, "Crit Damage:", $"{e.criticalDamage * 100:F0}%", e.criticalDamage, e.baseCriticalDamage);
        sb.AppendLine();
        ElemLine(sb, Magic, "Magic Power:", $"{e.magicPower:F0}", e.magicPower, e.baseMagicPower);
        if (e is Plant plant2)
        {
            float cdRatio = plant2.baseSkillCooldown > 0f ? plant2.skillCooldown / plant2.baseSkillCooldown : 1f;
            string cdPlain = $"{cdRatio * 100:F0}%";
            int cdDots = Mathf.Max(2, Width - "Skill Cooldown:".Length - cdPlain.Length);
            sb.AppendLine($"{Grass}Skill Cooldown:{End}{new string('.', cdDots)}{ColInv(cdRatio, 1f, $"<b>{cdPlain}</b>")}");
        }
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
        return sb.ToString().TrimEnd();
    }

    private string BuildOffense(Entity e)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<size=+8><color=white><b><u>Offense</u></b></color></size>");
        ElemLine(sb, Grass,   "Elemental Affinity:",  $"{e.elementalAffinity * 100:F0}%",  e.elementalAffinity,  e.baseelementalAffinity);
        sb.AppendLine();
        ElemLine(sb, Magic,    "Damage Over Time:",    $"{e.dotDamage * 100:F0}%",          e.dotDamage,          e.baseDotDamage);
        ElemLine(sb, Magic,    "Skill Damage:",        $"{e.skillDamage * 100:F0}%",        e.skillDamage,        e.baseSkillDamage);
        ElemLine(sb, Passive,  "Passive Damage:",      $"{e.passiveDamage * 100:F0}%",      e.passiveDamage,      e.basePassiveDamage);
        ElemLine(sb, Physical, "Counter Damage:",      $"{e.counterDamage * 100:F0}%",      e.counterDamage,      e.baseCounterDamage);
        ElemLine(sb, Blue,     "Coordinated Damage:",  $"{e.coordinatedDamage * 100:F0}%",  e.coordinatedDamage,  e.baseCoordinatedDamage);
        ElemLine(sb, Grass,   "Minion Damage:",       $"{e.minionDamage * 100:F0}%",       e.minionDamage,       e.baseMinionDamage);
        sb.AppendLine();
        ElemLine(sb, Physical, "Physical Damage:",     $"{e.physicalDamage * 100:F0}%",     e.physicalDamage,     e.basePhysicalDamage);
        ElemLine(sb, Magic,    "Magic Damage:",        $"{e.magicDamage * 100:F0}%",        e.magicDamage,        e.baseMagicDamage);
        ElemLine(sb, Physical, "Fall Damage:",         $"{e.fallDamage * 100:F0}%",         e.fallDamage,         e.baseFallDamage);
        sb.AppendLine();
        ElemLine(sb, Fire,   "Fire Damage:",   $"{e.fireDamage * 100:F0}%",   e.fireDamage,   e.baseFireDamage);
        ElemLine(sb, Water,  "Water Damage:",  $"{e.waterDamage * 100:F0}%",  e.waterDamage,  e.baseWaterDamage);
        ElemLine(sb, Grass, "Grass Damage:", $"{e.grassDamage * 100:F0}%", e.grassDamage, e.baseGrassDamage);
        ElemLine(sb, Ice,    "Ice Damage:",    $"{e.iceDamage * 100:F0}%",    e.iceDamage,    e.baseIceDamage);
        ElemLine(sb, Poison, "Poison Damage:", $"{e.poisonDamage * 100:F0}%", e.poisonDamage, e.basePoisonDamage);
        ElemLine(sb, Wind,   "Wind Damage:",   $"{e.windDamage * 100:F0}%",   e.windDamage,   e.baseWindDamage);
        ElemLine(sb, Ground, "Ground Damage:", $"{e.groundDamage * 100:F0}%", e.groundDamage, e.baseGroundDamage);
        return sb.ToString().TrimEnd();
    }

    private string BuildDefense(Entity e)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<size=+8><color=white><b><u>Defenses</u></b></color></size>");
        ElemLine(sb, Physical, "Physical Resistance:", $"{e.physicalResistance * 100:F0}%", e.physicalResistance, e.baseArmor / (100f + e.baseArmor));
        ElemLine(sb, Magic,    "Magic Resistance:",    $"{e.magicResistance * 100:F0}%",    e.magicResistance,    e.baseMagicArmor / (100f + e.baseMagicArmor));
        sb.AppendLine();
        ElemLine(sb, DoT,    "DoT Resistance:",    $"{e.dotResistance * 100:F0}%",    e.dotResistance,    e.baseDotResistance);
        sb.AppendLine();
        ElemLine(sb, Fire,   "Fire Resistance:",   $"{e.fireResistance * 100:F0}%",   e.fireResistance,   e.baseFireResistance);
        ElemLine(sb, Water,  "Water Resistance:",  $"{e.waterResistance * 100:F0}%",  e.waterResistance,  e.baseWaterResistance);
        ElemLine(sb, Grass, "Grass Resistance:", $"{e.grassResistance * 100:F0}%", e.grassResistance, e.baseGrassResistance);
        ElemLine(sb, Ice,    "Ice Resistance:",    $"{e.iceResistance * 100:F0}%",    e.iceResistance,    e.baseIceResistance);
        ElemLine(sb, Poison, "Poison Resistance:", $"{e.poisonResistance * 100:F0}%", e.poisonResistance, e.basePoisonResistance);
        ElemLine(sb, Wind,   "Wind Resistance:",   $"{e.windResistance * 100:F0}%",   e.windResistance,   e.baseWindResistance);
        ElemLine(sb, Ground, "Ground Resistance:", $"{e.groundResistance * 100:F0}%", e.groundResistance, e.baseGroundResistance);
        sb.AppendLine();
        Line(sb, "<color=grey>Tenacity:</color>", $"{e.tenacity * 100:F0}%", e.tenacity, e.baseTenacity);
        return sb.ToString().TrimEnd();
    }

    private string BuildUtility(Entity e)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<size=+8><color=white><b><u>Utility</u></b></color></size>");
        ElemLine(sb, HealCol, "Lifesteal:",  $"{e.lifesteal * 100:F0}%",  e.lifesteal,  e.baseLifesteal);
        Line(sb,              "Evasion:",    $"{e.evasion * 100:F1}%",    e.evasion,    e.baseEvasion);
        ElemLine(sb, Grass,  "Accuracy:",   $"{e.accuracy * 100:F1}%",   e.accuracy,   e.baseAccuracy);
        sb.AppendLine();
        ElemLine(sb, HealCol, "Heals & Shield Bonus:",    $"{e.healingBonus * 100:F0}%",    e.healingBonus,    e.baseHealingBonus);
        ElemLine(sb, HealCol, "Heals & Shield Received:", $"{e.healingReceived * 100:F0}%", e.healingReceived, e.baseHealingReceived);
        sb.AppendLine();
        ElemLine(sb, HealCol, "Buff Given Duration:",    $"{e.buffGivenDuration * 100:F0}%",    e.buffGivenDuration,    e.baseBuffGivenDuration);
        ElemLine(sb, HealCol, "Buff Received Duration:", $"{e.buffReceivedDuration * 100:F0}%", e.buffReceivedDuration, e.baseBuffReceivedDuration);
        ElemLine(sb, DoT,     "Debuff Given Duration:",    $"{e.debuffGivenDuration * 100:F0}%",    e.debuffGivenDuration,    e.baseDebuffGivenDuration);
        ElemLine(sb, DoT,     "Debuff Received Duration:", $"{e.debuffReceivedDuration * 100:F0}%", e.debuffReceivedDuration, e.baseDebuffReceivedDuration);
        sb.AppendLine();
        ElemLine(sb, Gold,   "Light Emission Range:", $"{e.lightEmissionRange:F1}", e.lightEmissionRange, e.baseLightEmissionRange);
        ElemLine(sb, Grass, "Skill Duration:",        $"{e.skillDuration:F1}s",    e.skillDuration,      e.baseSkillDuration);
        ElemLine(sb, DoT,     "DoT Duration:",            $"{e.dotDuration * 100:F0}%",           e.dotDuration,           e.baseDotDuration);
        ElemLine(sb, HealCol, "Regeneration Duration:",  $"{e.regenerationDuration * 100:F0}%",  e.regenerationDuration,  e.baseRegenerationDuration);
        ElemLine(sb, HealCol, "Shield Duration:",         $"{e.shieldDuration * 100:F0}%",        e.shieldDuration,        e.baseShieldDuration);
        InvLine(sb, Crit,    "Sun Generation Cooldown:", $"{e.sunGenerationCooldown * 100:F0}%", e.sunGenerationCooldown, e.baseSunGenerationCooldown);
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

    private void InvLine(System.Text.StringBuilder sb, string colorTag, string label, string plain, float current, float baseVal)
    {
        int dots = Mathf.Max(2, Width - label.Length - plain.Length);
        sb.AppendLine($"{colorTag}{label}{End}{new string('.', dots)}{ColInv(current, baseVal, $"<b>{plain}</b>")}");
    }
}
