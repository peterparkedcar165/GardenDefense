using UnityEngine;
using System.Collections.Generic;

public class FertilizerManager : MonoBehaviour
{
    public static FertilizerManager instance;

    private FertilizerData activeFertilizer;
    private FertilizerStat[] selectedStats;
    private float[] rolledValues;

    void Awake()
    {
        if (instance != null) return;
        instance = this;
    }

    public (FertilizerStat[] stats, float[] values) RollFor(FertilizerData fertilizer)
    {
        var seen = new HashSet<StatType>();
        var deduped = new List<FertilizerStat>();
        foreach (var stat in fertilizer.stats)
            if (seen.Add(stat.statType)) deduped.Add(stat);

        FertilizerStat[] stats = deduped.ToArray();
        float[] values = new float[stats.Length];
        for (int i = 0; i < stats.Length; i++)
            values[i] = stats[i].value;

        return (stats, values);
    }

    public void Commit(FertilizerData fertilizer, FertilizerStat[] stats, float[] values)
    {
        activeFertilizer = fertilizer;
        selectedStats = stats;
        rolledValues = values;
    }

    public void ApplyTo(Plant plant)
    {
        if (activeFertilizer == null) return;
        if (activeFertilizer.stats == null) return;

        if (!activeFertilizer.appliesToAll && plant.elementalType != activeFertilizer.targetElementalType) return;

        for (int i = 0; i < selectedStats.Length; i++)
            ApplyStat(plant, selectedStats[i].statType, rolledValues[i]);
    }

    private float GetTierMultiplier(FertilizerTier tier)
    {
        switch (tier)
        {
            case FertilizerTier.Common: return 1f;
            case FertilizerTier.Rare:   return 2f;
            case FertilizerTier.Epic:   return 3f;
            default:                    return 1f;
        }
    }

    private (float min, float max) GetBaseRange(StatType statType)
    {
        switch (statType)
        {
            case StatType.AttackDamage:    return (0.06f, 0.08f);
            case StatType.AttackSpeed:     return (0.06f, 0.08f);
            case StatType.AttackRange:     return (0.06f, 0.08f);
            case StatType.FireDamage:      return (0.06f, 0.08f);
            case StatType.IceDamage:       return (0.06f, 0.08f);
            case StatType.WaterDamage:     return (0.06f, 0.08f);
            case StatType.NatureDamage:    return (0.06f, 0.08f);
            case StatType.PoisonDamage:    return (0.06f, 0.08f);
            case StatType.WindDamage:      return (0.06f, 0.08f);
            case StatType.CriticalChance:  return (0.04f, 0.06f);
            case StatType.CriticalDamage:  return (0.125f,  0.15f);
            case StatType.ElementalPower:  return (0.01f, 0.125f);
            case StatType.PassiveDamage:   return (0.06f, 0.08f);
            case StatType.SkillDamage:     return (0.06f, 0.08f);
            case StatType.SkillCooldown:   return (0.06f, 0.08f);
            case StatType.DoTDamage:       return (0.06f, 0.08f);
            case StatType.Piercing:                    return (1f,    1f);
            case StatType.ImmobilizeDurationAdder:     return (0.5f,  1f);
            case StatType.ImmobilizeDurationMultiplier: return (0.1f, 0.3f);
            case StatType.PassiveCooldown:              return (0.06f, 0.08f);
            case StatType.SkillDurationAdder:           return (1f,    2f);
            case StatType.SkillDurationMultiplier:      return (0.06f, 0.08f);
            case StatType.CoordinatedDamage:            return (0.06f, 0.08f);
            case StatType.HealingBonus:                 return (0.06f, 0.08f);
            default:                       return (0f,    0f);
        }
    }

    private void ApplyStat(Plant plant, StatType statType, float value)
    {
        switch (statType)
        {
            case StatType.AttackDamage:    plant.attackDamageMultiplier           += value; break;
            case StatType.AttackSpeed:     plant.attackSpeedMultiplier            += value; break;
            case StatType.AttackRange:     plant.attackRangeMultiplier            += value; break;
            case StatType.FireDamage:      plant.fireDamageAdder             += value; break;
            case StatType.IceDamage:       plant.iceDamageAdder              += value; break;
            case StatType.WaterDamage:     plant.waterDamageAdder            += value; break;
            case StatType.NatureDamage:    plant.natureDamageAdder           += value; break;
            case StatType.PoisonDamage:    plant.poisonDamageAdder           += value; break;
            case StatType.WindDamage:      plant.windDamageAdder             += value; break;
            case StatType.CriticalChance:  plant.criticalChanceAdder         += value; break;
            case StatType.CriticalDamage:  plant.criticalDamageAdder         += value; break;
            case StatType.ElementalPower:  plant.elementalPowerAdder         += value; break;
            case StatType.PassiveDamage:   plant.passiveDamageAdder          += value; break;
            case StatType.SkillDamage:     plant.skillDamageAdder            += value; break;
            case StatType.SkillCooldown:   plant.skillCooldownReductionAdder += value; break;
            case StatType.DoTDamage:       plant.dotDamageAdder              += value; break;
            case StatType.Piercing:                     if (plant is Shooter shooter) shooter.piercingAdder += Mathf.RoundToInt(value); break;
            case StatType.ImmobilizeDurationAdder:      plant.immobilizeDurationAdder      += value; break;
            case StatType.ImmobilizeDurationMultiplier: plant.immobilizeDurationMultiplier += value; break;
            case StatType.PassiveCooldown:              plant.passiveCooldownReductionMultiplier += value; break;
            case StatType.SkillDurationAdder:           plant.skillDurationAdder      += value; break;
            case StatType.SkillDurationMultiplier:      plant.skillDurationMultiplier      += value; break;
            case StatType.CoordinatedDamage:            plant.coordinatedDamageAdder       += value; break;
            case StatType.HealingBonus:                 plant.healingBonusAdder            += value; break;
        }
    }
}
