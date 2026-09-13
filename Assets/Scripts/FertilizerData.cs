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
    Respiration
}