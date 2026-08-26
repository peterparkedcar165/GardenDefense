using UnityEngine;

[CreateAssetMenu(fileName = "FertilizerData", menuName = "Scriptable Objects/FertilizerData")]
public class FertilizerData : ScriptableObject
{
    public string fertilizerName;
    public FertilizerStat[] stats;
    public Sprite icon;
    public ElementalType[] targetElementalTypes;
    public PlantCultivar[] targetCultivars;
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
    GroundDamage,
    BonusEffectChance,
    MinimumDamage,
    MaximumDamage,
    ElementalEffectChance,
    HeatResistance,
    ColdResistance
}