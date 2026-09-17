using System.Collections.Generic;
using UnityEngine;

// single source of truth for which fertilizer stats are available generically (any loadout),
// which are unlocked by an elemental/family presence in the current loadout, and which are only
// ever available if a specific plant's own PlantData.fertilizerPossibleStats opts into them.
// used both by the runtime stat-pool computation (building the 3 choices to roll) and by the
// PlantData Inspector (to hide already-globally-handled stats from that per-plant list)
public static class FertilizerStatRules
{
    public static readonly StatType[] GenericStats =
    {
        StatType.AttackDamage,
        StatType.AttackSpeed,
        StatType.AttackRange,
        StatType.SkillCooldown,
        StatType.CriticalChance,
        StatType.CriticalDamage,
        StatType.MaxHealth,
        StatType.Armor,
        StatType.MagicArmor,
        StatType.MagicPower,
        StatType.BonusEffectChance,
        StatType.MinimumDamage,
        StatType.MaximumDamage,
        StatType.Piercing,
        StatType.SunYield,
        StatType.SkillDurationMultiplier,
        StatType.SkillDamage,
    };

    // Physical-dealing plant present -> armor pen/shred; Magic-dealing plant present -> magic
    // pen/shred. keyed by PlantData.damageType, not element - a plant's damage type (Physical/
    // Magic/True) is orthogonal to its elemental type
    private static readonly Dictionary<DamageType, StatType[]> DamageTypeStats = new Dictionary<DamageType, StatType[]>
    {
        { DamageType.Physical, new[] { StatType.ArmorPenetration, StatType.ArmorShred } },
        { DamageType.Magic,    new[] { StatType.MagicPenetration, StatType.MagicArmorShred } },
    };

    // elementalAffinity itself is unlocked separately (2+ distinct elements present), not tied
    // to any single element, so it's not in this per-element map
    private static readonly Dictionary<ElementalType, StatType> ElementalDamageStat = new Dictionary<ElementalType, StatType>
    {
        { ElementalType.Fire,   StatType.FireDamage },
        { ElementalType.Water,  StatType.WaterDamage },
        { ElementalType.Grass,  StatType.GrassDamage },
        { ElementalType.Poison, StatType.PoisonDamage },
        { ElementalType.Ice,    StatType.IceDamage },
        { ElementalType.Wind,   StatType.WindDamage },
    };

    // DoTDamage/DoTDuration unlock when the loadout has both elements of one of these pairs
    // present - each pair is a real elemental reaction that produces a DoT (Fire+Grass -> Burn,
    // Grass+Poison -> Poisoned, Poison+Ice -> Frostbite). unlocked broadly (StatScope.All) once
    // satisfied, same as elementalAffinity, since the payoff isn't tied to just the two plants
    // that happen to satisfy the pair. a plant can still separately opt into these directly via
    // its own fertilizerPossibleStats regardless of whether any pair is present
    private static readonly (ElementalType a, ElementalType b)[] DotUnlockPairs =
    {
        (ElementalType.Fire, ElementalType.Grass),
        (ElementalType.Grass, ElementalType.Poison),
        (ElementalType.Poison, ElementalType.Ice),
    };

    private static readonly StatType[] DotStats = { StatType.DoTDamage, StatType.DoTDuration };

    private static readonly Dictionary<PlantFamily, StatType[]> FamilyStats = new Dictionary<PlantFamily, StatType[]>
    {
        { PlantFamily.Verdance,       new[] { StatType.HealingBonus } },
        { PlantFamily.Photosynthesis, new[] { StatType.SunGenerationCooldownMultiplier } },
        { PlantFamily.Wither,         new[] { StatType.DebuffGivenDuration } },
        { PlantFamily.Kindred,        new[] { StatType.CoordinatedDamage } },
        { PlantFamily.Symbiosis,      new[] { StatType.BuffGivenDuration } },
        // Ironbark and Thorn intentionally have no family-conditional stats beyond generic
    };

    private static HashSet<StatType> _allGloballyHandled;

    // only the truly universal stats get filtered out of PlantData's per-plant
    // fertilizerPossibleStats picker - offering those there would always be pure redundant noise
    // since every plant already has them unconditionally. elementalAffinity is excluded too since
    // it's inherently a whole-loadout payoff (2+ distinct elements present), not something a
    // single plant can meaningfully "have" on its own.
    //
    // elemental/family/damage-type conditional stats are deliberately NOT excluded here: a plant
    // can legitimately want one of those from a category it doesn't itself belong to (e.g.
    // Calendula, a Kindred plant, picking up Symbiosis's BuffGivenDuration) - that's exactly what
    // the per-plant list is for, so the picker leaves them selectable
    public static HashSet<StatType> AllGloballyHandled
    {
        get
        {
            if (_allGloballyHandled == null)
                _allGloballyHandled = new HashSet<StatType>(GenericStats) { StatType.elementalAffinity };
            return _allGloballyHandled;
        }
    }

    // builds the full set of stats a fertilizer is allowed to roll for the player's current
    // loadout: generic + whichever elemental/family/multi-affinity rules the loadout satisfies +
    // the union of every present plant's own fertilizerPossibleStats
    public static HashSet<StatType> GetAvailableStats(IEnumerable<PlantData> loadout)
    {
        HashSet<StatType> available = new HashSet<StatType>(GenericStats);
        HashSet<ElementalType> elementsSeen = new HashSet<ElementalType>();

        foreach (PlantData plant in loadout)
        {
            if (plant == null) continue;

            if (ElementalDamageStat.TryGetValue(plant.elementalType, out StatType elementalStat))
                available.Add(elementalStat);
            elementsSeen.Add(plant.elementalType);

            if (FamilyStats.TryGetValue(plant.family, out StatType[] familyStats))
                foreach (StatType stat in familyStats) available.Add(stat);

            if (DamageTypeStats.TryGetValue(plant.damageType, out StatType[] damageTypeStats))
                foreach (StatType stat in damageTypeStats) available.Add(stat);

            if (plant.fertilizerPossibleStats != null)
                foreach (StatType stat in plant.fertilizerPossibleStats) available.Add(stat);
        }

        if (elementsSeen.Count >= 2) available.Add(StatType.elementalAffinity);
        if (HasDotPair(elementsSeen))
            foreach (StatType stat in DotStats) available.Add(stat);

        return available;
    }

    private static bool HasDotPair(HashSet<ElementalType> elementsSeen)
    {
        foreach ((ElementalType a, ElementalType b) in DotUnlockPairs)
            if (elementsSeen.Contains(a) && elementsSeen.Contains(b))
                return true;
        return false;
    }

    // which plants a rolled stat should actually apply to, once a generated fertilizer commits
    // it - a stat unlocked by "a Fire plant is present" should only ever boost Fire plants, even
    // if it ends up bundled alongside a generic stat in the same fertilizer. AppliesToAll covers
    // both the generic pool and elementalAffinity (a multi-element payoff, not tied to one)
    public struct StatScope
    {
        public bool appliesToAll;
        public ElementalType? requiredElement;
        public PlantFamily? requiredFamily;
        public DamageType? requiredDamageType;
        public HashSet<string> requiredPlantNames; // by PlantData.plantName, for per-plant opt-ins

        public static readonly StatScope All = new StatScope { appliesToAll = true };

        public bool Matches(Plant plant)
        {
            if (appliesToAll) return true;
            if (requiredElement.HasValue && plant.elementalType == requiredElement.Value) return true;
            if (requiredFamily.HasValue && plant.data != null && plant.data.family == requiredFamily.Value) return true;
            if (requiredDamageType.HasValue && plant.damageType == requiredDamageType.Value) return true;
            if (requiredPlantNames != null && plant.data != null && requiredPlantNames.Contains(plant.data.plantName)) return true;
            return false;
        }
    }

    // same as GetAvailableStats, but keyed with each stat's applicability scope so a generated
    // fertilizer's stats only ever land on the plants that actually make them make sense
    public static Dictionary<StatType, StatScope> GetAvailableStatsWithScope(IEnumerable<PlantData> loadout)
    {
        Dictionary<StatType, StatScope> result = new Dictionary<StatType, StatScope>();
        foreach (StatType stat in GenericStats) result[stat] = StatScope.All;

        HashSet<ElementalType> elementsSeen = new HashSet<ElementalType>();

        foreach (PlantData plant in loadout)
        {
            if (plant == null) continue;
            elementsSeen.Add(plant.elementalType);

            if (ElementalDamageStat.TryGetValue(plant.elementalType, out StatType elementalStat))
                MergeInto(result, elementalStat, new StatScope { requiredElement = plant.elementalType });

            if (FamilyStats.TryGetValue(plant.family, out StatType[] familyStats))
                foreach (StatType stat in familyStats)
                    MergeInto(result, stat, new StatScope { requiredFamily = plant.family });

            if (DamageTypeStats.TryGetValue(plant.damageType, out StatType[] damageTypeStats))
                foreach (StatType stat in damageTypeStats)
                    MergeInto(result, stat, new StatScope { requiredDamageType = plant.damageType });

            if (plant.fertilizerPossibleStats != null)
                foreach (StatType stat in plant.fertilizerPossibleStats)
                    MergeInto(result, stat, new StatScope { requiredPlantNames = new HashSet<string> { plant.plantName } });
        }

        if (elementsSeen.Count >= 2) result[StatType.elementalAffinity] = StatScope.All;
        if (HasDotPair(elementsSeen))
            foreach (StatType stat in DotStats) MergeInto(result, stat, StatScope.All);

        return result;
    }

    // combines a newly-found unlock reason into whatever's already recorded for this stat,
    // rather than overwriting it - a stat can be unlocked by more than one reason at once (e.g.
    // BuffGivenDuration via a Symbiosis plant AND via Calendula's own fertilizerPossibleStats
    // opt-in), and both conditions need to keep matching afterward
    private static void MergeInto(Dictionary<StatType, StatScope> result, StatType stat, StatScope addition)
    {
        if (!result.TryGetValue(stat, out StatScope existing))
        {
            result[stat] = addition;
            return;
        }
        if (existing.appliesToAll) return;
        if (addition.appliesToAll) { result[stat] = StatScope.All; return; }

        if (existing.requiredElement == null) existing.requiredElement = addition.requiredElement;
        if (existing.requiredFamily == null) existing.requiredFamily = addition.requiredFamily;
        if (existing.requiredDamageType == null) existing.requiredDamageType = addition.requiredDamageType;
        if (addition.requiredPlantNames != null)
        {
            if (existing.requiredPlantNames == null) existing.requiredPlantNames = new HashSet<string>();
            existing.requiredPlantNames.UnionWith(addition.requiredPlantNames);
        }
        result[stat] = existing;
    }
}
