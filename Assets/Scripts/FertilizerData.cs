using UnityEngine;

[CreateAssetMenu(fileName = "FertilizerData", menuName = "Scriptable Objects/FertilizerData")]
public class FertilizerData : ScriptableObject
{
    // no fertilizer name for now
    [TextArea] public string description;
    public FertilizerStat[] stats;
    public Sprite icon;
    public ElementalType targetElementalType; // can be all!
    public bool appliesToAll; // if true, ignores targetelementaltype
    public FertilizerTier tier;
}

// OUTSIDE OF MAIN CLASS

[System.Serializable]
public struct FertilizerStat
{
    public StatType statType; // stats
    public float minValue; // range
    public float maxValue; // range
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
    NatureDamage,
    PoisonDamage,
    WindDamage,
    CriticalChance,
    CriticalDamage,
    ElementalPower,
    PassiveDamage,
    SkillDamage,
    SkillCooldown,
    DoTDamage
}