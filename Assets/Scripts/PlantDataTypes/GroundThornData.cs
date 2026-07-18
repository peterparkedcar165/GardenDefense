using UnityEngine;

[CreateAssetMenu(fileName = "GroundThornData", menuName = "Scriptable Objects/PlantData/GroundThorn")]
public class GroundThornData : PlantData
{
    [Header("Attack")]
    public float splashRadius = 1f;
    public float splashMultiplier = 0.75f;
    public float groundedDuration = 3f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 23f;
    public float path1AttackRangePerLevel = 0.2f;
    public float knockbackDistance = 0.6f;

    [Header("Path 2 Scaling (tile passive)")]
    public float grassChanceBase = 0.75f;
    public float grassChancePerLevel = 0.05f;
    public float grassFireResReduction = 0.35f;
    public float grassFireResDuration = 4f;
    public float dirtDamageBonus = 0.25f;
    public float dirtBonusPerLevel = 0.05f;
    public float sandChanceBase = 0.5f;
    public float sandChancePerLevel = 0.1f;
    public float blindDuration = 4f;
    public float blindAccuracyPenalty = 0.5f;
    public float caveChanceBase = 0.5f;
    public float caveChancePerLevel = 0.05f;
    public float stunDuration = 1f;
    public float stunDurationPerLevel = 0.2f;
    public float snowSlowPercent = 0.15f;
    public float snowSlowPerLevel = 0.03f;
    public float snowSlowDuration = 3f;
    public float sunderPercent = 0.35f;
    public float sunderDuration = 8f;

    [Header("Path 3 Scaling (earth pillars)")]
    public int   pillarCountBase = 3;
    public float pillarStartOffset = 1f;
    public float pillarSpacing = 1f;
    public float pillarInterval = 0.12f;
    public float pillarRadius = 0.9f;
    public float pillarHitboxMultiplier = 1.2f;
    public float pillarDamageGrowth = 0.1f;
    public float skillBaseDamage = 40f;
    public float path3SkillDamagePerLevel = 8f;
    public float pillarKnockUpForce = 5f;
    public float pillarKnockbackDistance = 0.9f;
    public int   pillarStunHitThreshold = 3;
    public float pillarStunDuration = 1f;

    public override string GetAttackDescription() =>
        "Lifts earth from the ground and hurls it at the target, damaging insects around the impact and knocking down flying insects.";

    public override string GetPassiveDescription() =>
        "Gains a bonus effect on attack and skill damage depending on the tile it is planted on.";

    public override string GetSkillDescription() =>
        "Summons a line of earth pillars that erupt outwards, launching and pushing insects along the wave.";
}
