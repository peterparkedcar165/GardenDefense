using UnityEngine;

[CreateAssetMenu(fileName = "SnowdropData", menuName = "Scriptable Objects/PlantData/Snowdrop")]
public class SnowdropData : PlantData
{
    [Header("Passive Chill")]
    public float baseSlow    = 0.24f;
    public float scalingSlow = 0.06f;

    [Header("Passive Cooling")]
    public float coolingPerSecond = 2f;

    [Header("Skill Blizzard")]
    public float baseBlizzardDamage        = 0f;
    public float blizzardDamagePerLevel    = 15f;
    public float baseBlizzardDuration      = 5f;
    public float blizzardDurationPerLevel  = 1f;
    public float blizzardChillMultiplier   = 1.5f;
    public float blizzardCoolingMultiplier = 2f;
    public float baseBlizzardRange         = 10f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 1f;
    public float path1AttackRangePerLevel  = 0.1f;
    public float path1MaxElementalEffectChanceBonus = 0.02f;
    public float path1MaxAttackDamagePenalty        = 0.5f;
    public float path1MaxAttackSpeedBonus           = 1f;

    [Header("Path 3 Scaling")]
    public float path3BlizzardWidthPerLevel = 0.5f;
    public float path3BlizzardRangePerLevel = 0.5f;

    public override string GetAttackDescription() =>
        $"Continuously deals {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage to all ground-level insects within range.";

    public override string GetPassiveDescription() =>
        "Applies <color=#00FFFF>Chill</color> to nearby insects, slowing their movement.\n\n" +
        "Plants within the radius receive <color=#00FFFF>Cooling</color>, reducing temperature toward comfort.";

    public override string GetSkillDescription() =>
        $"Summons a strong blizzard aimed toward the targeted area, dealing {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage per second to insects caught within and applying a stronger <color=#00FFFF>Chill</color>. Plants within the blizzard also receive an enhanced <color=#00FFFF>Cooling</color> effect.";
}
