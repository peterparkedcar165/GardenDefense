using UnityEngine;

public class PlantData : ScriptableObject
{
    [Header("Display")]
    public Plant plantPrefab;
    public Sprite icon;
    public string plantName;
    public string displayName;
    public ElementalType elementalType;
    public DamageType damageType;
    public int sunCost;

    [Header("Core")]
    public float baseMaxHealth = 200f;
    public float baseAttackDamage;
    public float baseMagicPower;
    public float baseAttackSpeed;
    public float baseAttackRange;

    [Header("Healing")]
    public float baseHealingBonus;
    public float baseHealingReceived;

    [Header("Resistances")]
    public float baseTenacity;
    public float basePhysicalResistance;
    public float baseMagicResistance;
    public float baseFireResistance;
    public float baseWaterResistance;
    public float basePoisonResistance;
    public float baseIceResistance;
    public float baseNatureResistance;
    public float baseWindResistance;
    public float baseDotResistance;

    [Header("Offensive")]
    public float basePhysicalShred;
    public float baseMagicShred;
    public float baseLifesteal;
    public float baseBonusEffectChance;
    public float baseCriticalChance = 0.05f;
    public float baseCriticalDamage = 1.75f;
    public float baseDotDamage;
    public float baseElementalPower;
    public float baseCoordinatedDamage;
    public float baseCounterDamage;

    [Header("Elemental Damage")]
    public float baseFireDamage;
    public float baseWaterDamage;
    public float baseNatureDamage;
    public float baseWindDamage;
    public float basePoisonDamage;
    public float baseIceDamage;

    [Header("Misc")]
    public float baseLightEmissionRange;

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
    public float baseSkillRadius;
    public float baseSkillDamageMultiplier;
    public float baseSkillHealth;
    public float basePassiveDamage;
    public float baseSkillDamage;

    public virtual string GetAttackDescription() => "";
    public virtual string GetPassiveDescription() => "";
    public virtual string GetSkillDescription() => "";
}
