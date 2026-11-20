using System.Collections.Generic;
using UnityEngine;

// single source of truth for which fertilizer stats are available generically (any loadout),
// which are unlocked by an elemental presence in the current loadout, and which are only
// ever available if a specific plant's own PlantData.fertilizerPossibleStats opts into them.
// used both by the runtime stat-pool computation (building the 3 choices to roll) and by the
// PlantData Inspector (to hide already-globally-handled stats from that per-plant list)
public static class FertilizerStatRules
{
    public static readonly StatType[] GenericStats =
    {
        StatType.AttackDamage,
        StatType.AttackSpeed,
        StatType.SkillCooldown,
        StatType.CriticalChance,
        StatType.CriticalDamage,
        StatType.MaxHealth,
        StatType.MagicPower,
        StatType.MinimumDamage,
        StatType.MaximumDamage,
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

    // FallDamage/Armor/MagicArmor aren't unlocked by the player's own loadout alone - they react
    // to what's actually spawning in the current level (flying insects, aggressive insects, magic
    // insects), read from ProceduralLevel.CurrentConfig since FertilizerStatRules is static and
    // has no scene reference of its own. plantName set matches BogIris/Carrot/Anemone, whose kits
    // make FallDamage useful even with no flying insects around
    private static readonly HashSet<string> FallDamagePlantNames = new HashSet<string> { "BogIris", "Carrot", "Anemone" };

    // AttackRange is only ever useful on a ranged attacker - melee plants clamp attackRange to
    // baseAttackRange regardless (see Entity.IsMeleeAttacker), so fertilizer range bonuses do
    // nothing for them. kept as a name set here too, same as FallDamagePlantNames above, since
    // FertilizerStatRules only ever sees PlantData, never the live Plant/Entity instance that
    // actually carries the IsMeleeAttacker override
    private static readonly HashSet<string> MeleePlantNames = new HashSet<string> { "AcornKnight", "BirdOfParadise", "Cactus" };

    // a plant whose range bonuses do nothing (Entity.IsMeleeAttacker on its class)
    public static bool IsMeleePlant(PlantData plant) => plant != null && MeleePlantNames.Contains(plant.plantName);

    // reads only the hand-authored wave slots (not the procedural roster/elite arrays) - what's
    // actually placed in a wave's sub-wave spawns is what the player will really fight, so that's
    // what these level-driven stat unlocks should react to
    private static IEnumerable<InsectData> GetLevelInsectPool()
    {
        LevelConfig config = ProceduralLevel.CurrentConfig;
        if (config == null || config.waves == null) yield break;

        foreach (WaveDefinition wave in config.waves)
        {
            if (wave == null) continue;

            if (wave.subWaves != null)
                foreach (SubWaveDefinition sub in wave.subWaves)
                    if (sub != null && sub.spawns != null)
                        foreach (WaveSpawnEntry spawn in sub.spawns)
                            if (spawn != null && spawn.insectData != null) yield return spawn.insectData;

            if (wave.trickleSpawns != null)
                foreach (TrickleEntry trickle in wave.trickleSpawns)
                    if (trickle != null && trickle.insectData != null) yield return trickle.insectData;
        }
    }

    private static bool HasFlyingInsectInLevel()
    {
        foreach (InsectData data in GetLevelInsectPool())
            if (data.insectPrefab != null && data.insectPrefab.GetComponent<FlyingInsect>() != null)
                return true;
        return false;
    }

    // Aggressivity only has None/Low/High - "medium/high" maps to High, the highest tier that exists
    private static bool HasHighAggressivityInsectInLevel()
    {
        foreach (InsectData data in GetLevelInsectPool())
            if (data.aggressivity == Aggressivity.High)
                return true;
        return false;
    }

    private static bool HasMagicInsectInLevel()
    {
        foreach (InsectData data in GetLevelInsectPool())
            if (data.attackDamageType == DamageType.Magic)
                return true;
        return false;
    }

    private static HashSet<StatType> _allGloballyHandled;

    // only the truly universal stats get filtered out of PlantData's per-plant
    // fertilizerPossibleStats picker - offering those there would always be pure redundant noise
    // since every plant already has them unconditionally. elementalAffinity is excluded too since
    // it's inherently a whole-loadout payoff (2+ distinct elements present), not something a
    // single plant can meaningfully "have" on its own.
    //
    // elemental/damage-type conditional stats are deliberately NOT excluded here: a plant
    // can legitimately want one of those from a category it doesn't itself belong to (e.g.
    // Calendula picking up BuffGivenDuration) - that's exactly what
    // the per-plant list is for, so the picker leaves them selectable
    public static HashSet<StatType> AllGloballyHandled
    {
        get
        {
            if (_allGloballyHandled == null)
                // AttackRange is globally handled too, same as elementalAffinity: unlocked by
                // loadout composition (any non-melee plant present), not a per-plant opt-in
                _allGloballyHandled = new HashSet<StatType>(GenericStats) { StatType.elementalAffinity, StatType.AttackRange };
            return _allGloballyHandled;
        }
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
        public DamageType? requiredDamageType;
        public HashSet<string> requiredPlantNames; // by PlantData.plantName, for per-plant opt-ins
        // AttackRange's scope: unlocked generically by any non-melee plant being present, but
        // still shouldn't ever land on a melee plant even then (see Entity.IsMeleeAttacker) -
        // checked ahead of every other criterion below, so it overrides any other match too
        public bool excludeMelee;

        public static readonly StatScope All = new StatScope { appliesToAll = true };

        public bool Matches(Plant plant)
        {
            if (excludeMelee && plant.IsMeleeAttacker) return false;
            if (appliesToAll) return true;
            if (requiredElement.HasValue && plant.elementalType == requiredElement.Value) return true;
            if (requiredDamageType.HasValue && plant.damageType == requiredDamageType.Value) return true;
            if (requiredPlantNames != null && plant.data != null && requiredPlantNames.Contains(plant.data.plantName)) return true;
            return false;
        }

        // same rules read from plant data, for previews of a plant that isn't placed yet
        public bool Matches(PlantData data, bool isMeleeAttacker)
        {
            if (excludeMelee && isMeleeAttacker) return false;
            if (appliesToAll) return true;
            if (data == null) return false;
            if (requiredElement.HasValue && data.elementalType == requiredElement.Value) return true;
            if (requiredDamageType.HasValue && data.damageType == requiredDamageType.Value) return true;
            if (requiredPlantNames != null && requiredPlantNames.Contains(data.plantName)) return true;
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
        bool hasFallDamagePlant = false;
        bool hasRangedPlant = false;

        foreach (PlantData plant in loadout)
        {
            if (plant == null) continue;
            elementsSeen.Add(plant.elementalType);

            if (ElementalDamageStat.TryGetValue(plant.elementalType, out StatType elementalStat))
                MergeInto(result, elementalStat, new StatScope { requiredElement = plant.elementalType });

            if (DamageTypeStats.TryGetValue(plant.damageType, out StatType[] damageTypeStats))
                foreach (StatType stat in damageTypeStats)
                    MergeInto(result, stat, new StatScope { requiredDamageType = plant.damageType });

            if (plant.fertilizerPossibleStats != null)
                foreach (StatType stat in plant.fertilizerPossibleStats)
                    MergeInto(result, stat, new StatScope { requiredPlantNames = new HashSet<string> { plant.plantName } });

            if (FallDamagePlantNames.Contains(plant.plantName)) hasFallDamagePlant = true;
            if (!MeleePlantNames.Contains(plant.plantName)) hasRangedPlant = true;
        }

        if (elementsSeen.Count >= 2) result[StatType.elementalAffinity] = StatScope.All;
        if (HasDotPair(elementsSeen))
            foreach (StatType stat in DotStats) MergeInto(result, stat, StatScope.All);

        if (hasFallDamagePlant || HasFlyingInsectInLevel()) MergeInto(result, StatType.FallDamage, StatScope.All);
        if (HasHighAggressivityInsectInLevel()) MergeInto(result, StatType.Armor, StatScope.All);
        if (HasMagicInsectInLevel()) MergeInto(result, StatType.MagicArmor, StatScope.All);
        if (hasRangedPlant) MergeInto(result, StatType.AttackRange, new StatScope { appliesToAll = true, excludeMelee = true });

        return result;
    }

    // combines a newly-found unlock reason into whatever's already recorded for this stat,
    // rather than overwriting it - a stat can be unlocked by more than one reason at once (e.g.
    // Armor via a high aggressivity insect AND via a plant's own fertilizerPossibleStats
    // opt-in), and both conditions need to keep matching afterward
    private static void MergeInto(Dictionary<StatType, StatScope> result, StatType stat, StatScope addition)
    {
        if (!result.TryGetValue(stat, out StatScope existing))
        {
            result[stat] = addition;
            return;
        }

        // excludeMelee must survive regardless of which side is appliesToAll or which order
        // merges happen in - the early appliesToAll shortcuts below would otherwise silently
        // drop it (e.g. AttackRange's own exclusion getting lost if a plant also separately
        // opts into it by name via fertilizerPossibleStats)
        bool excludeMelee = existing.excludeMelee || addition.excludeMelee;

        if (existing.appliesToAll || addition.appliesToAll)
        {
            result[stat] = new StatScope { appliesToAll = true, excludeMelee = excludeMelee };
            return;
        }

        existing.excludeMelee = excludeMelee;
        if (existing.requiredElement == null) existing.requiredElement = addition.requiredElement;
        if (existing.requiredDamageType == null) existing.requiredDamageType = addition.requiredDamageType;
        if (addition.requiredPlantNames != null)
        {
            if (existing.requiredPlantNames == null) existing.requiredPlantNames = new HashSet<string>();
            existing.requiredPlantNames.UnionWith(addition.requiredPlantNames);
        }
        result[stat] = existing;
    }

    // the value range one wild mulch stat rolls in at Common, before WildMulchData's own scale.
    // these are the old field wide fertilizer ranges, kept as they were
    public static (float min, float max) BaseRange(StatType statType)
    {
        switch (statType)
        {
            case StatType.AttackDamage:    return (0.03f, 0.04f);
            case StatType.AttackSpeed:     return (0.03f, 0.04f);
            case StatType.AttackRange:     return (0.03f, 0.04f);
            case StatType.FireDamage:      return (0.03f, 0.04f);
            case StatType.IceDamage:       return (0.03f, 0.04f);
            case StatType.WaterDamage:     return (0.03f, 0.04f);
            case StatType.GrassDamage:     return (0.03f, 0.04f);
            case StatType.PoisonDamage:    return (0.03f, 0.04f);
            case StatType.WindDamage:      return (0.03f, 0.04f);
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
            case StatType.SunYield:        return (0.03f, 0.05f);
            case StatType.CurrencyYield:   return (0.03f, 0.05f);
            case StatType.ShieldDuration:  return (0.03f, 0.05f);
            case StatType.RegenerationDuration: return (0.03f, 0.05f);
            case StatType.ImmobilizeDurationAdder:     return (0.25f, 0.5f);
            case StatType.ImmobilizeDurationMultiplier: return (0.05f, 0.15f);
            case StatType.PassiveCooldown:              return (0.03f, 0.04f);
            case StatType.PassiveDurationMultiplier:    return (0.03f, 0.05f);
            case StatType.SkillDurationAdder:           return (0.5f,  1f);
            case StatType.SkillDurationMultiplier:      return (0.03f, 0.04f);
            case StatType.CoordinatedDamage:            return (0.03f, 0.04f);
            case StatType.HealingBonus:                 return (0.03f, 0.04f);
            case StatType.IlluminationRangeAdder:       return (0.25f, 0.5f);
            case StatType.IlluminationRangeMultiplier:  return (0.08f, 0.12f);
            case StatType.CounterDamage:                return (0.03f, 0.04f);
            case StatType.PhysicalDamage:               return (0.03f, 0.04f);
            case StatType.MagicDamage:                  return (0.03f, 0.04f);
            case StatType.PhysicalResistance:           return (0.02f, 0.03f);
            case StatType.MagicResistance:              return (0.02f, 0.03f);
            case StatType.MagicPower:                   return (12f,  16f);
            case StatType.DebuffGivenDuration:          return (0.03f, 0.05f);
            case StatType.BuffGivenDuration:            return (0.03f, 0.05f);
            case StatType.BuffReceivedDuration:         return (0.03f, 0.05f);
            case StatType.DebuffReceivedDuration:       return (0.03f, 0.05f);
            case StatType.MinionDamage:                 return (0.03f, 0.04f);
            case StatType.FallDamage:                   return (0.03f, 0.04f);
            case StatType.Armor:                        return (6f,  8f);
            case StatType.MagicArmor:                   return (6f,  8f);
            case StatType.ArmorPenetration:             return (4f,  7f);
            case StatType.MagicPenetration:             return (4f,  7f);
            case StatType.ArmorShred:                   return (0.02f, 0.03f);
            case StatType.MagicArmorShred:              return (0.02f, 0.03f);
            case StatType.SunGenerationCooldownMultiplier: return (-0.05f, -0.03f);
            case StatType.HeatResistance:                return (0.02f, 0.03f);
            case StatType.ColdResistance:                return (0.02f, 0.03f);
            case StatType.Respiration:                   return (0.02f, 0.03f);
            default:                       return (0f,    0f);
        }
    }

    // flat whole number stats (not percentages): a rolled 6.83 armor would look wrong
    public static bool IsIntegerStat(StatType statType)
    {
        switch (statType)
        {
            case StatType.MagicPower:
            case StatType.Armor:
            case StatType.MagicArmor:
            case StatType.ArmorPenetration:
            case StatType.MagicPenetration:
                return true;
            default:
                return false;
        }
    }
}
