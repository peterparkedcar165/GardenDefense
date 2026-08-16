using UnityEngine;

public class PlantData : ScriptableObject
{
    [Header("Display")]
    public Plant plantPrefab;
    public Sprite icon;
    public string plantName;
    public string displayName;
    public PlantCultivar cultivar;
    public ElementalType elementalType;
    public DamageType damageType;
    public int sunCost;

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

    public static string CultivarTag(PlantCultivar c) => c switch
    {
        PlantCultivar.Chlorophyll => "<color=#FFD700>Chlorophyll</color>",
        PlantCultivar.Verdance    => "<color=#FF6B81>Verdance</color>",
        PlantCultivar.Symbiosis   => "<color=#20B2AA>Symbiosis</color>",
        PlantCultivar.Shelter     => "<color=#A9A9A9>Shelter</color>",
        PlantCultivar.Thorn       => "<color=#DC143C>Thorn</color>",
        PlantCultivar.Wither      => "<color=#8B008B>Wither</color>",
        PlantCultivar.Burgeon     => "<color=#DA70D6>Burgeon</color>",
        PlantCultivar.Kinship     => "<color=#6495ED>Kinship</color>",
        _                         => c.ToString()
    };

    public static string DamageTypeTag(DamageType t) => t switch
    {
        DamageType.Magic    => "<color=#FFB6C1>Magic</color>",
        DamageType.Physical => "<color=#A0522D>Physical</color>",
        DamageType.True     => "<color=white>True</color>",
        _                   => t.ToString()
    };
}
