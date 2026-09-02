using UnityEngine;

[CreateAssetMenu(fileName = "AcornSproutData", menuName = "Scriptable Objects/PlantData/AcornSprout")]
public class AcornSproutData : PlantData
{
    public float stunChance;
    public float stunDuration;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 8f;
    public float path1AttackSpeedPerLevel = 0.05f;
    public int path1ArmorPerLevel = 4;

    [Header("Path 2 Scaling")]
    public float path2StunChancePerLevel = 0.05f;
    public float path2StunDurationPerLevel = 0.1f;

    [Header("Path 3 Scaling")]
    public float path3DamageMultiplierPerLevel = 0.25f;
    public float path3SkillDurationPerLevel = 2f;
    public float path3HealthPerLevel = 50f;
    public float path3RadiusPerLevel = 0.15f;

    public override string GetAttackDescription() =>
        $"Shoots acorns towards his target, dealing {DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        "Attacks have a chance to stun targets. While healthy, its delicious skin keeps attackers from wandering off.";

    public override string GetSkillDescription() =>
        $"Hurls a giant acorn from the sky at a targeted location, dealing {DamageTypeLabel(damageType)} and stunning all insects in the impact radius. The acorn then sits on the ground, blocking ground insects who stop to gnaw at it.";
}
