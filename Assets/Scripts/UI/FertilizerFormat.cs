// shared display formatting for fertilizer stats, used by FertilizerCard (selection)
// and FertilizerInfoTooltip (in game reminder) so both render identically
public static class FertilizerFormat
{
    // indices into statTypes, ordered by each stat's displayed name (A to Z) rather than
    // whatever order they were rolled in - every fertilizer stat list renders through this so
    // the displayed order is consistent and predictable regardless of roll order
    public static int[] SortedIndicesByName(StatType[] statTypes)
    {
        int[] indices = new int[statTypes.Length];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        System.Array.Sort(indices, (a, b) =>
            string.Compare(FormatStatName(statTypes[a]), FormatStatName(statTypes[b]), System.StringComparison.Ordinal));
        return indices;
    }

    public static bool IsInvertedStat(StatType statType)
    {
        switch (statType)
        {
            case StatType.DebuffReceivedDuration:
            case StatType.SunGenerationCooldownMultiplier:
                return true;
            default: return false;
        }
    }

    public static string FormatValue(StatType statType, float value)
    {
        string sign = value >= 0f ? "+" : "";
        switch (statType)
        {
            case StatType.ImmobilizeDurationAdder:
            case StatType.SkillDurationAdder:
            case StatType.IlluminationRangeAdder:
                return $"{sign}{value:F1}s";
            case StatType.Piercing:
            case StatType.MagicPower:
            case StatType.Armor:
            case StatType.MagicArmor:
            case StatType.ArmorPenetration:
            case StatType.MagicPenetration:
            case StatType.AttackDamageFlat:
            case StatType.Path1LevelAdder:
            case StatType.Path2LevelAdder:
            case StatType.Path3LevelAdder:
            case StatType.SunCostReduction:
                return $"{sign}{UnityEngine.Mathf.RoundToInt(value)}";
            case StatType.AttackSpeedFlat:
                return $"{sign}{value:F2}";
            default:
                return $"{sign}{value * 100f:F0}%";
        }
    }

    public static string FormatStatName(StatType statType)
    {
        switch (statType)
        {
            case StatType.AttackDamage:   return "Attack Damage";
            case StatType.AttackSpeed:    return "Attack Speed";
            case StatType.AttackRange:    return "Attack Range";
            case StatType.FireDamage:     return "Fire Damage";
            case StatType.IceDamage:      return "Ice Damage";
            case StatType.WaterDamage:    return "Water Damage";
            case StatType.GrassDamage:   return "Grass Damage";
            case StatType.PoisonDamage:   return "Poison Damage";
            case StatType.WindDamage:     return "Wind Damage";
            case StatType.BonusEffectChance: return "Bonus Effect Chance";
            case StatType.MinimumDamage:  return "Minimum Damage";
            case StatType.MaximumDamage:  return "Maximum Damage";
            case StatType.CriticalChance: return "Critical Chance";
            case StatType.CriticalDamage: return "Critical Damage";
            case StatType.elementalAffinity: return "Elemental Affinity";
            case StatType.PassiveDamage:  return "Passive Damage";
            case StatType.SkillDamage:    return "Skill Damage";
            case StatType.SkillCooldown:  return "Skill Cd. Reduction";
            case StatType.DoTDamage:      return "Damage Over Time";
            case StatType.Piercing:                     return "Piercing";
            case StatType.ImmobilizeDurationAdder:      return "Immobilize Duration";
            case StatType.ImmobilizeDurationMultiplier: return "Immobilize Duration";
            case StatType.PassiveCooldown:              return "Passive Cd. Reduction";
            case StatType.PassiveDurationMultiplier:    return "Passive Duration";
            case StatType.SkillDurationAdder:           return "Skill Duration";
            case StatType.SkillDurationMultiplier:      return "Skill Duration";
            case StatType.CoordinatedDamage:            return "Coordinated Damage";
            case StatType.HealingBonus:                 return "Heals & Shield Bonus";
            case StatType.IlluminationRangeAdder:       return "Illumination Range";
            case StatType.IlluminationRangeMultiplier:  return "Illumination Range";
            case StatType.CounterDamage:                return "Counter Damage";
            case StatType.PhysicalDamage:               return "Physical Damage";
            case StatType.MagicDamage:                  return "Magic Damage";
            case StatType.PhysicalResistance:           return "Physical Resistance";
            case StatType.MagicResistance:              return "Magic Resistance";
            case StatType.MagicPower:                   return "Magic Power";
            case StatType.DebuffGivenDuration:          return "Debuff Given Duration";
            case StatType.BuffGivenDuration:            return "Buff Given Duration";
            case StatType.BuffReceivedDuration:         return "Buff Received Duration";
            case StatType.DebuffReceivedDuration:       return "Debuff Received Duration";
            case StatType.MinionDamage:                 return "Minion Damage";
            case StatType.FallDamage:                   return "Fall Damage";
            case StatType.Armor:                        return "Armor";
            case StatType.MagicArmor:                   return "Magic Armor";
            case StatType.ArmorPenetration:             return "Armor Penetration";
            case StatType.MagicPenetration:             return "Magic Penetration";
            case StatType.ArmorShred:                       return "Armor Shred";
            case StatType.MagicArmorShred:                  return "Magic Armor Shred";
            case StatType.DoTDuration:                      return "DoT Duration";
            case StatType.RegenerationDuration:             return "Regeneration Duration";
            case StatType.ShieldDuration:                   return "Shield Duration";
            case StatType.SunGenerationCooldownMultiplier:  return "Sun Generation Cooldown";
            case StatType.MaxHealth:                        return "Max Health";
            case StatType.SunYield:                         return "Sun Yield";
            case StatType.CurrencyYield:                    return "Currency Yield";
            case StatType.HeatResistance:                   return "Heat Resistance";
            case StatType.ColdResistance:                   return "Cold Resistance";
            case StatType.Respiration:                      return "Respiration";
            case StatType.AttackDamageFlat:                 return "Attack Damage";
            case StatType.AttackSpeedFlat:                  return "Attack Speed";
            case StatType.Path1LevelAdder:                  return "Effective Attack Point";
            case StatType.Path2LevelAdder:                  return "Effective Passive Point";
            case StatType.Path3LevelAdder:                  return "Effective Skill Point";
            case StatType.SunCostReduction:                 return "Sun Cost";
            default:                                        return statType.ToString();
        }
    }

    // same palette StatsPanelTooltip.cs uses for these stats' own tooltip lines, so a fertilizer
    // roll's stat name reads as the same "kind" of stat at a glance. stats with no clear
    // counterpart there (pure utility/duration stats not tied to any element or category) fall
    // back to plain white rather than an arbitrary invented color
    private const string White = "white";
    private const string Fire     = "orange";
    private const string Water    = "#4FC3F7";
    private const string Grass    = "green";
    private const string Ice      = "#00FFFF";
    private const string Poison   = "purple";
    private const string Wind     = "#B2EBF2";
    private const string Effect   = "#B3FFFF";
    private const string Magic    = "#FFB6C1";
    private const string Physical = "#A0522D";
    private const string Crit     = "#FFD700";
    private const string Heal     = "#FF6B81";
    private const string Sun      = "#FFD700";
    private const string ArmorCol = "#00CED1";
    private const string MagicArmorCol = "#FF69B4";
    private const string Coordinated = "#6495ED";

    public static string GetStatColor(StatType statType)
    {
        switch (statType)
        {
            case StatType.AttackDamage:
            case StatType.AttackDamageFlat:
            case StatType.AttackSpeed:
            case StatType.AttackSpeedFlat:
            case StatType.AttackRange:
            case StatType.GrassDamage:
            case StatType.elementalAffinity:
            case StatType.Piercing:
                return Grass;
            case StatType.FireDamage:
            case StatType.HeatResistance:
                return Fire;
            case StatType.IceDamage:
            case StatType.ColdResistance:
                return Ice;
            case StatType.WaterDamage:
                return Water;
            case StatType.PoisonDamage:
                return Poison;
            case StatType.WindDamage:
                return Wind;
            case StatType.BonusEffectChance:
                return Effect;
            case StatType.MinimumDamage:
            case StatType.MaximumDamage:
            case StatType.MagicDamage:
            case StatType.MagicResistance:
            case StatType.MagicPower:
            case StatType.MagicPenetration:
            case StatType.MagicArmorShred:
                return Magic;
            case StatType.CriticalChance:
            case StatType.CriticalDamage:
                return Crit;
            case StatType.CoordinatedDamage:
                return Coordinated;
            case StatType.HealingBonus:
            case StatType.RegenerationDuration:
            case StatType.ShieldDuration:
            case StatType.MaxHealth:
                return Heal;
            case StatType.IlluminationRangeAdder:
            case StatType.IlluminationRangeMultiplier:
                return Fire;
            case StatType.PhysicalDamage:
            case StatType.PhysicalResistance:
            case StatType.ArmorPenetration:
            case StatType.ArmorShred:
                return Physical;
            case StatType.Armor:
                return ArmorCol;
            case StatType.MagicArmor:
                return MagicArmorCol;
            case StatType.SunGenerationCooldownMultiplier:
            case StatType.SunYield:
            case StatType.CurrencyYield:
            case StatType.SunCostReduction:
                return Sun;
            default:
                return White;
        }
    }
}
