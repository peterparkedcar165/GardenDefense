// shared display formatting for fertilizer stats, used by FertilizerCard (selection)
// and FertilizerInfoTooltip (in game reminder) so both render identically
public static class FertilizerFormat
{
    public static bool IsInvertedStat(StatType statType)
    {
        switch (statType)
        {
            case StatType.DebuffReceivedDuration: return true;
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
                return $"{sign}{UnityEngine.Mathf.RoundToInt(value)}";
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
            case StatType.NatureDamage:   return "Nature Damage";
            case StatType.PoisonDamage:   return "Poison Damage";
            case StatType.WindDamage:     return "Wind Damage";
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
            default:                      return statType.ToString();
        }
    }
}
