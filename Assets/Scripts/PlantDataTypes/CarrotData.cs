using UnityEngine;

[CreateAssetMenu(fileName = "CarrotData", menuName = "Scriptable Objects/PlantData/Carrot")]
public class CarrotData : PlantData
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

    [Header("Path 3 Scaling (carrot furrow)")]
    [Tooltip("number of carrots in the furrow, each covers one square length")]
    public int carrotCountBase = 3;
    public int carrotsPerLevel = 1;
    public float pillarStartOffset = 1f;
    [Tooltip("seconds for the plow to cross one square")]
    public float pillarInterval = 0.12f;
    [Tooltip("half the square size, squares are spaced exactly one square apart")]
    public float pillarRadius = 0.9f;
    public float pillarHitboxMultiplier = 1.3f;
    public float skillBaseDamage = 40f;
    public float path3SkillDamagePerLevel = 8f;
    public float pillarKnockUpForce = 5f;
    public float pillarKnockbackDistance = 0.9f;
    [Tooltip("max level bonus, seconds before the second furrow follows the first")]
    public float secondFurrowDelay = 0.8f;

    [Header("Furrow Visuals")]
    public float visualFadeIn = 0.1f;
    public float visualHold = 0.7f;
    public float visualFadeOut = 0.5f;
    [Tooltip("random x and y offset applied to each carrot visual on spawn")]
    public float visualPositionJitter = 0.05f;

    public override string GetAttackDescription() =>
        "Lifts earth from the ground and hurls it at the target, damaging insects around the impact and knocking down flying insects.";

    public override string GetPassiveDescription() =>
        "Gains a bonus effect on attack and skill damage depending on the tile it is planted on.";

    public override string GetSkillDescription() =>
        "Summons a line of earth pillars that erupt outwards, launching and pushing insects along the wave.";
}
