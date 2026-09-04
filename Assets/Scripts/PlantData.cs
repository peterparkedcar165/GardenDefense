using UnityEngine;
using UnityEngine.Serialization;

public class PlantData : ScriptableObject
{
    [Header("Display")]
    public Plant plantPrefab;
    public Sprite icon;
    public string plantName;
    public string displayName;
    [FormerlySerializedAs("cultivar")]
    public PlantFamily family;
    public ElementalType elementalType;
    public DamageType damageType;
    public int sunCost;
    // max number of this plant that can be alive on the field at once. 0 = unlimited
    public int placementLimit = 0;

    // dev-only label, never surfaced to players anywhere - just a way to tag which plants are
    // "elder" tier (currently Begonia and Carrot) versus the normal roster, for our own reference
    public enum PlantCategory { Normal, Elder }

    [Header("Dev Only")]
    public PlantCategory category = PlantCategory.Normal;

    [Header("Skill Tree")]
    public SkillTreeData skillTree;

    [Header("Core")]
    public float baseMaxHealth = 200f;
    public float baseAttackDamage;
    public float baseMagicPower;
    public float baseAttackSpeed;
    public float baseAttackRange;
    public int baseArmor;
    public int baseMagicArmor;
    public float baseElementalEffectChance = 0.1f;
    public float baseOnHitEffectiveness = 1f;

    [Header("Healing")]
    public float baseHealingBonus;
    public float baseHealingReceived;

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
    public float baseHeatResistance;   // slows temperature rise in Hot weather, does not reduce the resulting damage
    public float baseColdResistance;   // slows temperature drop in Cold weather, does not reduce the resulting damage
    public float baseRespiration;      // slows Air depletion while Submerged, same shape as heat/cold resistance

    [Header("Offensive")]
    public float basePhysicalDamage;
    public float baseMagicDamage;
    public int baseArmorPenFlat;
    public float baseArmorPenPercent;
    public int baseMagicPenFlat;
    public float baseMagicPenPercent;
    public float baseLifesteal;
    public float baseBonusEffectChance;
    public float baseCriticalChance = 0.05f;
    public float baseCriticalDamage = 1.75f;
    public float baseDotDamage;
    public float baseelementalAffinity;
    public float baseCoordinatedDamage;
    public float baseCounterDamage;

    [Header("Elemental Damage")]
    public float baseFireDamage;
    public float baseWaterDamage;
    public float baseGrassDamage;
    public float baseWindDamage;
    public float basePoisonDamage;
    public float baseIceDamage;
    public float baseGroundDamage;

    [Header("Misc")]
    public float baseLightEmissionRange;
    public float baseMinionDamage;   // multiplier bonus on damage dealt by this plant's minions

    [Header("Shooter")]
    public float baseProjectileSpeed;
    public float baseMaxRange;
    public int basePiercing;

    [Header("Lobber")]
    public float baseAoERadius;
    public float minFlightDuration = 0.5f;
    public float projectileHeight = 1.2f;

    [Header("Specific")]
    public float basePassiveCooldown;
    public float basePassiveDuration;
    public float baseSkillCooldown;
    public float baseSkillDuration;
    public float channelDuration;   // how long the plant cannot auto-attack while casting its skill
    public float baseSkillRadius;
    public float baseSkillDamageMultiplier;
    public float baseSkillHealth;
    public float basePassiveDamage;
    public float baseSkillDamage;

    public virtual string GetAttackDescription() => "";
    public virtual string GetPassiveDescription() => "";
    public virtual string GetSkillDescription() => "";

    public static string ElementalTag(ElementalType t) => t switch
    {
        ElementalType.Fire    => "<color=orange>Fire</color>",
        ElementalType.Water   => "<color=#4FC3F7>Water</color>",
        ElementalType.Grass  => "<color=green>Grass</color>",
        ElementalType.Wind    => "<color=#B2EBF2>Wind</color>",
        ElementalType.Poison  => "<color=purple>Poison</color>",
        ElementalType.Ice     => "<color=#00FFFF>Ice</color>",
        ElementalType.Ground  => "<color=#79391F>Ground</color>",
        _                     => t.ToString()
    };

    public static string ElementalColor(ElementalType t) => t switch
    {
        ElementalType.Fire    => "orange",
        ElementalType.Water   => "#4FC3F7",
        ElementalType.Grass  => "green",
        ElementalType.Wind    => "#B2EBF2",
        ElementalType.Poison  => "purple",
        ElementalType.Ice     => "#00FFFF",
        ElementalType.Ground  => "#79391F",
        _                     => "white"
    };

    public static string DamageTypeLabel(DamageType t) => t switch
    {
        DamageType.Magic    => "<color=#FFB6C1><b>Magic Damage</b></color>",
        DamageType.Physical => "<color=#A0522D><b>Physical Damage</b></color>",
        DamageType.True     => "<color=white><b>True Damage</b></color>",
        _                   => $"<b>{t} Damage</b>"
    };

    public static string FamilyTag(PlantFamily f) => f switch
    {
        PlantFamily.Photosynthesis => "<color=#FFD700>Photosynthesis</color>",
        PlantFamily.Verdance       => "<color=#FF6B81>Verdance</color>",
        PlantFamily.Symbiosis      => "<color=#20B2AA>Symbiosis</color>",
        PlantFamily.Shelter        => "<color=#A9A9A9>Shelter</color>",
        PlantFamily.Thorn          => "<color=#DC143C>Thorn</color>",
        PlantFamily.Wither         => "<color=#8B008B>Wither</color>",
        PlantFamily.Kindred        => "<color=#6495ED>Kindred</color>",
        _                          => f.ToString()
    };

}
