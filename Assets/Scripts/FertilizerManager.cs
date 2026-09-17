using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class FertilizerManager : MonoBehaviour
{
    public static FertilizerManager instance;

    // loaded from Resources in Awake instead of a scene-dragged reference - this is a
    // DontDestroyOnLoad singleton that potentially exists (and needs configuring) in every
    // level scene, so a serialized field would mean re-dragging it dozens of times
    private PlantRegistry plantRegistry;

    private FertilizerData activeFertilizer;
    private FertilizerStat[] selectedStats;
    private float[] rolledValues;

    // mid-level procedural fertilizer queue - entirely separate from the single pre-level pick
    // above. session-only by design: ResetFertilizerQueue is called per level start, so nothing
    // here is meant to survive leaving and re-entering a level
    private readonly Queue<FertilizerTier> pendingGrants = new Queue<FertilizerTier>();
    private readonly List<GeneratedFertilizer> committedGenerated = new List<GeneratedFertilizer>();

    public int PendingFertilizerCount => pendingGrants.Count;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        plantRegistry = Resources.Load<PlantRegistry>("PlantRegistry");
        if (plantRegistry == null)
            Debug.LogError("FertilizerManager: PlantRegistry not found at Resources/PlantRegistry.asset");
    }

    // called once per level start (ProceduralLevel.Start) - this queue is session-only
    public void ResetFertilizerQueue()
    {
        pendingGrants.Clear();
        committedGenerated.Clear();
    }

    // called when a level's FertilizerGrant fires (its configured wave just completed)
    public void GrantFertilizer(FertilizerTier tier)
    {
        pendingGrants.Enqueue(tier);
    }

    // builds 3 random choices for the oldest queued grant, without consuming it - only
    // CommitGenerated actually dequeues, so backing out of the picker costs nothing
    public GeneratedFertilizer[] GenerateChoicesForOldest()
    {
        if (pendingGrants.Count == 0) return null;
        FertilizerTier tier = pendingGrants.Peek();
        Dictionary<StatType, FertilizerStatRules.StatScope> available = FertilizerStatRules.GetAvailableStatsWithScope(GetCurrentLoadout());

        GeneratedFertilizer[] choices = new GeneratedFertilizer[3];
        for (int i = 0; i < 3; i++)
            choices[i] = RollGenerated(tier, available);
        return choices;
    }

    // rerolls a single displayed choice in place, independent of the other 2 cards and of the
    // queue itself - the queue is untouched either way, since nothing is consumed until a card
    // is actually picked
    public GeneratedFertilizer RerollSingle(FertilizerTier tier)
    {
        Dictionary<StatType, FertilizerStatRules.StatScope> available = FertilizerStatRules.GetAvailableStatsWithScope(GetCurrentLoadout());
        return RollGenerated(tier, available);
    }

    // consumes the oldest queued grant and applies the chosen bundle immediately to every plant
    // already on the field - LoadData's ApplyTo only ever catches plants placed AFTER this call,
    // so existing plants need this explicit pass to actually feel the new fertilizer right away
    public void CommitGenerated(GeneratedFertilizer chosen)
    {
        if (pendingGrants.Count == 0 || chosen == null) return;
        pendingGrants.Dequeue();
        committedGenerated.Add(chosen);

        foreach (Plant plant in new List<Plant>(Plant.allPlants))
            if (plant != null && plant.IsAlive)
                ApplyGeneratedSingle(chosen, plant);
    }

    private List<PlantData> GetCurrentLoadout()
    {
        List<PlantData> result = new List<PlantData>();
        if (SaveManager.instance == null || plantRegistry == null || plantRegistry.plants == null) return result;
        foreach (string plantName in SaveManager.instance.selectedLoadout)
            foreach (PlantData data in plantRegistry.plants)
                if (data != null && data.plantName == plantName) { result.Add(data); break; }
        return result;
    }

    private GeneratedFertilizer RollGenerated(FertilizerTier tier, Dictionary<StatType, FertilizerStatRules.StatScope> available)
    {
        List<StatType> pool = new List<StatType>(available.Keys);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int count = Mathf.Min(Random.Range(2, 4), pool.Count); // 2 or 3 stats per bundle
        GeneratedFertilizerStat[] stats = new GeneratedFertilizerStat[count];
        for (int i = 0; i < count; i++)
        {
            StatType stat = pool[i];
            (float min, float max) = GetBaseRange(stat);
            float rolled = Random.Range(min, max) * GetTierMultiplier(tier);
            stats[i] = new GeneratedFertilizerStat { statType = stat, value = rolled, scope = available[stat] };
        }
        return new GeneratedFertilizer { tier = tier, stats = stats };
    }

    private void ApplyGeneratedSingle(GeneratedFertilizer fert, Plant plant)
    {
        foreach (GeneratedFertilizerStat stat in fert.stats)
            if (stat.scope.Matches(plant))
                ApplyStat(plant, stat.statType, stat.value);
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
            if (activeFertilizer.targetFamilies != null)
                foreach (var f in activeFertilizer.targetFamilies)
                    parts.Add(f.ToString());
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
        if (activeFertilizer != null && activeFertilizer.stats != null
            && (activeFertilizer.appliesToAll || MatchesTarget(plant)))
        {
            for (int i = 0; i < selectedStats.Length; i++)
                ApplyStat(plant, selectedStats[i].statType, rolledValues[i]);
        }

        // catches plants placed after a mid-level fertilizer was already committed - plants
        // already on the field when it's committed instead get it via CommitGenerated directly
        foreach (GeneratedFertilizer fert in committedGenerated)
            ApplyGeneratedSingle(fert, plant);
    }

    private bool MatchesTarget(Plant plant)
    {
        if (activeFertilizer.targetElementalTypes != null)
            foreach (var e in activeFertilizer.targetElementalTypes)
                if (plant.elementalType == e) return true;
        if (activeFertilizer.targetFamilies != null)
            foreach (var f in activeFertilizer.targetFamilies)
                if (plant.data != null && plant.data.family == f) return true;
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
            case StatType.AttackDamage:    return (0.03f, 0.04f);
            case StatType.AttackSpeed:     return (0.03f, 0.04f);
            case StatType.AttackRange:     return (0.03f, 0.04f);
            case StatType.FireDamage:      return (0.03f, 0.04f);
            case StatType.IceDamage:       return (0.03f, 0.04f);
            case StatType.WaterDamage:     return (0.03f, 0.04f);
            case StatType.GrassDamage:    return (0.03f, 0.04f);
            case StatType.PoisonDamage:    return (0.03f, 0.04f);
            case StatType.WindDamage:      return (0.03f, 0.04f);
            case StatType.BonusEffectChance: return (0.02f, 0.03f);
            case StatType.MinimumDamage:   return (0.02f, 0.03f);
            case StatType.MaximumDamage:   return (0.03f, 0.04f);
            case StatType.CriticalChance:  return (0.02f, 0.03f);
            case StatType.CriticalDamage:  return (0.0625f, 0.075f);
            case StatType.elementalAffinity:  return (0.03f, 0.05f);
            case StatType.PassiveDamage:   return (0.03f, 0.04f);
            case StatType.SkillDamage:     return (0.03f, 0.04f);
            case StatType.SkillCooldown:   return (0.03f, 0.04f);
            case StatType.DoTDamage:       return (0.03f, 0.04f);
            case StatType.DoTDuration:     return (0.03f, 0.05f);
            case StatType.MaxHealth:       return (0.03f, 0.05f);
            case StatType.Piercing:                    return (1f,  1f);
            case StatType.ImmobilizeDurationAdder:     return (0.25f, 0.5f);
            case StatType.ImmobilizeDurationMultiplier: return (0.05f, 0.15f);
            case StatType.PassiveCooldown:              return (0.03f, 0.04f);
            case StatType.PassiveDurationMultiplier:    return (0.03f, 0.05f);
            case StatType.SkillDurationAdder:           return (0.5f,  1f);
            case StatType.SkillDurationMultiplier:      return (0.03f, 0.04f);
            case StatType.CoordinatedDamage:            return (0.03f, 0.04f);
            case StatType.HealingBonus:                 return (0.03f, 0.04f);
            case StatType.IlluminationRangeAdder:       return (0.25f, 0.5f);
            case StatType.IlluminationRangeMultiplier:  return (0.12f, 0.18f);
            case StatType.CounterDamage:                return (0.03f, 0.04f);
            case StatType.PhysicalDamage:               return (0.03f, 0.04f);
            case StatType.MagicDamage:                  return (0.03f, 0.04f);
            case StatType.PhysicalResistance:           return (0.02f, 0.03f);
            case StatType.MagicResistance:              return (0.02f, 0.03f);
            case StatType.MagicPower:                   return (2.5f,  5f);
            case StatType.DebuffGivenDuration:          return (0.03f, 0.05f);
            case StatType.BuffGivenDuration:            return (0.03f, 0.05f);
            case StatType.BuffReceivedDuration:         return (0.03f, 0.05f);
            case StatType.DebuffReceivedDuration:       return (0.03f, 0.05f);
            case StatType.MinionDamage:                 return (0.03f, 0.04f);
            case StatType.FallDamage:                   return (0.03f, 0.04f);
            case StatType.Armor:                        return (2.5f,  5f);
            case StatType.MagicArmor:                   return (2.5f,  5f);
            case StatType.ArmorPenetration:             return (1.5f,  3f);
            case StatType.MagicPenetration:             return (1.5f,  3f);
            case StatType.ArmorShred:                   return (0.02f, 0.03f);
            case StatType.MagicArmorShred:              return (0.02f, 0.03f);
            case StatType.SunGenerationCooldownMultiplier: return (-0.05f, -0.03f);
            case StatType.HeatResistance:                return (0.02f, 0.03f);
            case StatType.ColdResistance:                return (0.02f, 0.03f);
            case StatType.Respiration:                   return (0.02f, 0.03f);
            default:                       return (0f,    0f);
        }
    }

    // stat application shared with the skill tree system
    private void ApplyStat(Plant plant, StatType statType, float value)
    {
        PlantStatApplier.Apply(plant, statType, value);
    }
}
