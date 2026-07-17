using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class FertilizerManager : MonoBehaviour
{
    public static FertilizerManager instance;

    private FertilizerData activeFertilizer;
    private FertilizerStat[] selectedStats;
    private float[] rolledValues;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
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

    public FertilizerData ActiveFertilizer => activeFertilizer;
    public bool HasActiveFertilizer => activeFertilizer != null && selectedStats != null;

    // builds the formatted reminder text shown by the in game info tooltip
    public string GetActiveSummary()
    {
        if (!HasActiveFertilizer) return null;

        var sb = new StringBuilder();
        sb.AppendLine($"<b>{activeFertilizer.fertilizerName}</b>");
        string target;
        if (activeFertilizer.appliesToAll)
        {
            target = "All Plants";
        }
        else
        {
            var parts = new System.Collections.Generic.List<string>();
            if (activeFertilizer.targetElementalTypes != null)
                foreach (var e in activeFertilizer.targetElementalTypes)
                    parts.Add(e.ToString());
            if (activeFertilizer.targetCultivars != null)
                foreach (var c in activeFertilizer.targetCultivars)
                    parts.Add(c.ToString());
            target = parts.Count > 0 ? string.Join(", ", parts) : "None";
        }
        sb.AppendLine($"<size=85%>Applies to: <color=#FFD700>{target}</color></size>");
        sb.AppendLine();

        for (int i = 0; i < selectedStats.Length; i++)
        {
            bool isGood = rolledValues[i] >= 0f;
            if (FertilizerFormat.IsInvertedStat(selectedStats[i].statType)) isGood = !isGood;
            string color = isGood ? "green" : "red";
            sb.AppendLine($"{FertilizerFormat.FormatStatName(selectedStats[i].statType)}: <color={color}><b>{FertilizerFormat.FormatValue(selectedStats[i].statType, rolledValues[i])}</b></color>");
        }

        return sb.ToString().TrimEnd();
    }

    public float GetPreviewRangeMultiplier(ElementalType elementalType)
    {
        if (activeFertilizer == null || selectedStats == null) return 0f;
        if (!activeFertilizer.appliesToAll && (activeFertilizer.targetElementalTypes == null || System.Array.IndexOf(activeFertilizer.targetElementalTypes, elementalType) < 0)) return 0f;
        float multiplier = 0f;
        for (int i = 0; i < selectedStats.Length; i++)
            if (selectedStats[i].statType == StatType.AttackRange)
                multiplier += rolledValues[i];
        return multiplier;
    }

    public void ApplyTo(Plant plant)
    {
        if (activeFertilizer == null) return;
        if (activeFertilizer.stats == null) return;

        if (!activeFertilizer.appliesToAll && !MatchesTarget(plant)) return;

        for (int i = 0; i < selectedStats.Length; i++)
            ApplyStat(plant, selectedStats[i].statType, rolledValues[i]);
    }

    private bool MatchesTarget(Plant plant)
    {
        if (activeFertilizer.targetElementalTypes != null)
            foreach (var e in activeFertilizer.targetElementalTypes)
                if (plant.elementalType == e) return true;
        if (activeFertilizer.targetCultivars != null)
            foreach (var c in activeFertilizer.targetCultivars)
                if (plant.data != null && plant.data.cultivar == c) return true;
        return false;
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
            case StatType.GrassDamage:    return (0.06f, 0.08f);
            case StatType.PoisonDamage:    return (0.06f, 0.08f);
            case StatType.WindDamage:      return (0.06f, 0.08f);
            case StatType.GroundDamage:    return (0.06f, 0.08f);
            case StatType.CriticalChance:  return (0.04f, 0.06f);
            case StatType.CriticalDamage:  return (0.125f,  0.15f);
            case StatType.elementalAffinity:  return (0.01f, 0.125f);
            case StatType.PassiveDamage:   return (0.06f, 0.08f);
            case StatType.SkillDamage:     return (0.06f, 0.08f);
            case StatType.SkillCooldown:   return (0.06f, 0.08f);
            case StatType.DoTDamage:       return (0.06f, 0.08f);
            case StatType.Piercing:                    return (1f,    1f);
            case StatType.ImmobilizeDurationAdder:     return (0.5f,  1f);
            case StatType.ImmobilizeDurationMultiplier: return (0.1f, 0.3f);
            case StatType.PassiveCooldown:              return (0.06f, 0.08f);
            case StatType.PassiveDurationMultiplier:    return (0.06f, 0.10f);
            case StatType.SkillDurationAdder:           return (1f,    2f);
            case StatType.SkillDurationMultiplier:      return (0.06f, 0.08f);
            case StatType.CoordinatedDamage:            return (0.06f, 0.08f);
            case StatType.HealingBonus:                 return (0.06f, 0.08f);
            case StatType.IlluminationRangeAdder:       return (0.5f,  1f);
            case StatType.IlluminationRangeMultiplier:  return (0.06f, 0.08f);
            case StatType.CounterDamage:                return (0.06f, 0.08f);
            case StatType.PhysicalDamage:               return (0.06f, 0.08f);
            case StatType.MagicDamage:                  return (0.06f, 0.08f);
            case StatType.PhysicalResistance:           return (0.04f, 0.06f);
            case StatType.MagicResistance:              return (0.04f, 0.06f);
            case StatType.MagicPower:                   return (5f,    10f);
            case StatType.DebuffGivenDuration:          return (0.06f, 0.10f);
            case StatType.BuffGivenDuration:            return (0.06f, 0.10f);
            case StatType.BuffReceivedDuration:         return (0.06f, 0.10f);
            case StatType.DebuffReceivedDuration:       return (0.06f, 0.10f);
            case StatType.MinionDamage:                 return (0.06f, 0.08f);
            case StatType.FallDamage:                   return (0.06f, 0.08f);
            case StatType.Armor:                        return (5f,    10f);
            case StatType.MagicArmor:                   return (5f,    10f);
            case StatType.ArmorPenetration:             return (3f,    6f);
            case StatType.MagicPenetration:             return (3f,    6f);
            case StatType.ArmorShred:                   return (0.04f, 0.06f);
            case StatType.MagicArmorShred:              return (0.04f, 0.06f);
            default:                       return (0f,    0f);
        }
    }

    // stat application shared with the skill tree system
    private void ApplyStat(Plant plant, StatType statType, float value)
    {
        PlantStatApplier.Apply(plant, statType, value);
    }
}
