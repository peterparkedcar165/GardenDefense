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
        instance = this;
    }

    public (FertilizerStat[] stats, float[] values) RollFor(FertilizerData fertilizer)
    {
        int statCount = fertilizer.tier switch
        {
            FertilizerTier.Common => Random.Range(1, 3),
            FertilizerTier.Rare   => Random.Range(1, 3),
            FertilizerTier.Epic   => Random.Range(2, 4),
            _                     => 1
        };

        float multiplier = GetTierMultiplier(fertilizer.tier);

        var seen = new HashSet<StatType>();
        var deduped = new List<FertilizerStat>();
        foreach (var stat in fertilizer.stats)
            if (seen.Add(stat.statType)) deduped.Add(stat);

        FertilizerStat[] pool = deduped.ToArray();
        statCount = Mathf.Min(statCount, pool.Length);

        for (int i = pool.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        FertilizerStat[] stats = new FertilizerStat[statCount];
        float[] values = new float[statCount];
        for (int i = 0; i < statCount; i++)
        {
            stats[i] = pool[i];
            if (pool[i].minValue > pool[i].maxValue)
                Debug.LogWarning($"FertilizerData '{fertilizer.name}': stat {i} ({pool[i].statType}) has minValue ({pool[i].minValue}) greater than maxValue ({pool[i].maxValue}).");
            values[i] = Random.Range(pool[i].minValue, pool[i].maxValue) * multiplier;
        }

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

        activeFertilizer = null;
        selectedStats = null;
        rolledValues = null;
    }

    private float GetTierMultiplier(FertilizerTier tier)
    {
        switch (tier)
        {
            case FertilizerTier.Common: return 1f;
            case FertilizerTier.Rare:   return 1.3f;
            case FertilizerTier.Epic:   return 1.85f;
            default:                    return 1f;
        }
    }

    private void ApplyStat(Plant plant, StatType statType, float value)
    {
        switch (statType)
        {
            case StatType.AttackDamage:    plant.attackDamageAdder           += value; break;
            case StatType.AttackSpeed:     plant.attackSpeedAdder            += value; break;
            case StatType.AttackRange:     plant.attackRangeAdder            += value; break;
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
        }
    }
}
