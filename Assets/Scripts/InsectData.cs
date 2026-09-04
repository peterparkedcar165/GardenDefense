using UnityEngine;

[CreateAssetMenu(fileName = "InsectData", menuName = "Scriptable Objects/Insect Data")]
public class InsectData : ScriptableObject
{
    [Header("Display")]
    public string displayName;
    public Color nameColor = Color.white;
    public Sprite sprite;
    public GameObject insectPrefab;

    [Header("Core Stats")]
    public float baseMaxHealth;
    public float baseAttackDamage;
    public float baseMagicPower;
    public float baseAttackSpeed;
    public float baseAttackRange;
    public float baseMovementSpeed;
    public float baseTargetingRange;
    public int baseArmor;
    public int baseMagicArmor;

    [Header("Offensive")]
    public DamageType attackDamageType = DamageType.Physical;
    public ElementalType attackElementalType = ElementalType.Neutral;
    public float baseLifesteal;
    public float basePhysicalDamage;
    public float baseMagicDamage;
    public int baseArmorPenFlat;
    public float baseArmorPenPercent;
    public int baseMagicPenFlat;
    public float baseMagicPenPercent;

    [Header("Resistances")]
    public float startingShield;
    public float baseTenacity;
    public float baseFireResistance;
    public float baseWaterResistance;
    public float basePoisonResistance;
    public float baseIceResistance;
    public float baseGrassResistance;
    public float baseWindResistance;
    public float baseGroundResistance;
    public float baseDotResistance;
    public float flatPhysicalDamageReduction;


    [Header("Threat")]
    public float threatValue = 1f;
    public ThreatType threatType = ThreatType.Basic;

    [Header("Carry")]
    [Tooltip("A carrier (e.g. Duskdarter) picks up the eligible ground insect with the highest carryPriority in range, rather than the slowest one. Insects that should naturally get ferried past danger (slow, tanky, or otherwise valuable to protect) should have this set higher than the default.")]
    public int carryPriority = 0;

    [Header("Evasion / Accuracy")]
    public float baseEvasion;
    public float baseAccuracy;

    [Header("Other")]
    public int sunDrop;
    public float sunDropAdder;
    public float sunDropMultiplier;
    public int currencyDrop;
    public float currencyDropAdder;
    public float currencyDropMultiplier;
    public Aggressivity aggressivity;
    public float baseLightEmissionRange;
    public float baseHealingBonus;
    public float baseHealingReceived;

}
