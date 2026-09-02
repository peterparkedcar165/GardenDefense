using UnityEngine;

[CreateAssetMenu(fileName = "HelleboreData", menuName = "Scriptable Objects/PlantData/Hellebore")]
public class HelleboreData : PlantData
{
    [Header("Path 1 - Attack")]
    public float path1AttackSpeedPerLevel = 0.05f;
    public float path1MagicPowerPerLevel  = 5f;
    public float path1AttackRangePerLevel = 0.2f;

    [Header("Passive - Self Armor")]
    public int   selfArmorBase      = 14;
    public int   selfArmorPerLevel  = 5;
    public float selfArmorMP        = 0.14f;
    public float passiveCDRPerHit   = 0.5f;

    [Header("Path 2 - Passive")]
    public float path2CDRPerLevel       = 0.1f;
    public float path2AuraSharePerLevel = 0.05f;

    [Header("Passive - Aura")]
    public float auraShareBase = 0.5f;

    [Header("Skill - Thorned Guard")]
    public float shieldAmount      = 120f;
    public float shieldMP          = 0.5f;
    public float shieldDuration    = 12f;
    public float reflectPoisonBase = 15f;
    public float reflectPoisonMP   = 0.2f;

    [Header("Path 3 - Skill")]
    public float path3ShieldPerLevel   = 30f;
    public float path3DurationPerLevel = 2f;
    public float path3ReflectPerLevel  = 5f;

    public override string GetAttackDescription() =>
        $"Fires a thorned projectile dealing {DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        "Each attack hit reduces skill cooldown. Hellebore gains bonus <color=#00CED1><b>Armor</b></color>, scaling with <color=#FFB6C1><b>Magic Power</b></color>. " +
        "Plants within attack range gain <color=#9B30D0><b>Hellebore's Protection</b></color>, sharing a portion of Hellebore's Armor.";

    public override string GetSkillDescription() =>
        "Targets a plant anywhere on the field, granting <color=#9B30D0><b>Thorned Guard</b></color>: a shield that reflects <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage back to attackers and negates negative effects. The protection fades when the shield breaks.";
}
