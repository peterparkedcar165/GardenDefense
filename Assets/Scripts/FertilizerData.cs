using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "FertilizerData", menuName = "Scriptable Objects/FertilizerData")]
public class FertilizerData : ScriptableObject
{
    public string fertilizerName;
    public FertilizerStat[] stats;
    public Sprite icon;
    public ElementalType[] targetElementalTypes;
    [FormerlySerializedAs("targetCultivars")]
    public PlantFamily[] targetFamilies;
    public bool appliesToAll;
    public FertilizerTier tier;
}

// OUTSIDE OF MAIN CLASS

[System.Serializable]
public struct FertilizerStat
{
    public StatType statType;
    public float value;
}

public enum FertilizerTier { Common, Rare, Epic }

public enum StatType
{
    AttackDamage,
    AttackSpeed,
    AttackRange,
    FireDamage,
    IceDamage,
    WaterDamage,
    GrassDamage,
    PoisonDamage,
    WindDamage,
    CriticalChance,
    CriticalDamage,
    elementalAffinity,
    PassiveDamage,
    SkillDamage,
    SkillCooldown,
    DoTDamage,
    Piercing,
    ImmobilizeDurationAdder,
    ImmobilizeDurationMultiplier,
    PassiveCooldown,
    PassiveDurationMultiplier,
    SkillDurationAdder,
    SkillDurationMultiplier,
    CoordinatedDamage,
    HealingBonus,
    IlluminationRangeAdder,
    IlluminationRangeMultiplier,
    CounterDamage,
    PhysicalDamage,
    MagicDamage,
    PhysicalResistance,
    MagicResistance,
    MagicPower,
    DebuffGivenDuration,
    BuffGivenDuration,
    BuffReceivedDuration,
    DebuffReceivedDuration,
    MinionDamage,
    FallDamage,
    Armor,
    MagicArmor,
    ArmorPenetration,
    MagicPenetration,
    ArmorShred,
    MagicArmorShred,
    DoTDuration,
    RegenerationDuration,
    ShieldDuration,
    SunGenerationCooldownMultiplier,
    MaxHealth,
    SunYield,
    CurrencyYield,
    // unused: Ground element was removed from the game. kept in place (not deleted) so every
    // member below keeps its serialized ordinal - existing fertilizer assets reference these by
    // that raw int, not by name
    GroundDamage,
    BonusEffectChance,
    MinimumDamage,
    MaximumDamage,
    // unused: the elemental-proc roll this stat fed (elementalEffectRoll in Entity.cs) was
    // replaced by the deterministic Primer combo system. kept in place (not deleted) so
    // HeatResistance/ColdResistance/Respiration below keep their serialized ordinals
    ElementalEffectChance,
    HeatResistance,
    ColdResistance,
    Respiration,
    // flat (non-percentage) versions of AttackDamage/AttackSpeed, added for skill tree nodes that
    // grant a plain "Increase Base X by Y" bonus rather than a percentage multiplier - appended
    // here rather than next to their percentage counterparts to keep every ordinal above stable
    AttackDamageFlat,
    AttackSpeedFlat,
    // virtual per-path level bonuses (skill tree "+1 Effective X Point" nodes) - added to
    // path1LevelAdder/path2LevelAdder/path3LevelAdder rather than the real purchased level, so
    // they boost a plant's OnPathXUpgrade output without costing sun or counting as a real level
    Path1LevelAdder,
    Path2LevelAdder,
    Path3LevelAdder,
    // flat reduction to a plant's placement sun cost, applied at Tile placement time (before the
    // plant even exists) via SkillTreeManager.GetSunCostReduction - never applied through
    // PlantStatApplier since there is no live plant instance yet when it matters
    SunCostReduction,
    // flat (non-percentage) skill cooldown reduction, for skill tree nodes that grant a plain
    // "reduce cooldown by N seconds" bonus rather than a percentage multiplier - feeds
    // Plant.skillCooldownReductionAdder, appended here to keep every ordinal above stable
    SkillCooldownFlat,
    // same idea as SkillCooldownFlat but for passive cooldown (e.g. Sunflower's sun generation
    // timer) - feeds Plant.passiveCooldownAdder
    PassiveCooldownFlat,
    // flat (non-percentage) attack range, same idea as AttackDamageFlat/AttackSpeedFlat above -
    // feeds Plant.attackRangeAdder
    AttackRangeFlat,
    // Waterlily-specific: raises her stacking Slow's cap by a flat amount per rank. plant-specific
    // (cast in PlantStatApplier, same pattern as Piercing/Shooter) since MaxSlowStacks isn't a
    // generic Entity/Plant field
    MaxSlowStacksFlat,
    // PoisonShroom-specific: flat seconds added to Toxic Spore duration per rank, on top of her
    // own path2ToxicSporeDurationPerLevel scaling - plant-specific since Toxic Spore duration
    // isn't a generic Entity/Plant field
    ToxicSporeDurationFlat
}