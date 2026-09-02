using UnityEngine;

[CreateAssetMenu(fileName = "OldCarrotData", menuName = "Scriptable Objects/PlantData/OldCarrot")]
public class OldCarrotData : PlantData
{
    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 23f;
    public float path1AttackRangePerLevel = 0.2f;
    public int path1PiercingPerLevel = 1;
    [Tooltip("max level bonus, extra hits granted to a projectile when it switches targets")]
    public int path1TargetSwitchBonusHits = 1;

    [Header("Path 2 Scaling (Psionic Bond)")]
    public float psionicCooldownBase = 3f;
    public float psionicCooldownReductionPerLevel = 0.3f;
    public float psionicDamageBase = 30f;
    public float psionicDamagePerLevel = 20f;
    public float psionicDamageMPScaling = 0.5f;

    [Header("Path 3 Scaling (oldCarrot furrow)")]
    [Tooltip("number of oldCarrots in the furrow, each covers one square length")]
    public int oldCarrotCountBase = 3;
    public int oldCarrotsPerLevel = 1;
    public float pillarStartOffset = 1f;
    [Tooltip("seconds for the plow to cross one square")]
    public float pillarInterval = 0.12f;
    [Tooltip("half the square size, squares are spaced exactly one square apart")]
    public float pillarRadius = 0.9f;
    public float pillarHitboxMultiplier = 1.3f;
    public float skillBaseDamage = 40f;
    public float path3SkillDamagePerLevel = 8f;
    public float pillarKnockUpForce = 5f;
    [Tooltip("oldCarrot width grows by this fraction per level (hitbox, spacing, and visual scale all together); knock-aside distance always matches the current width")]
    public float path3WidthPerLevel = 0.1f;

    [Header("Furrow Visuals")]
    public float visualFadeIn = 0.1f;
    public float visualHold = 0.7f;
    public float visualFadeOut = 0.5f;
    [Tooltip("random x and y offset applied to each oldCarrot visual on spawn")]
    public float visualPositionJitter = 0.05f;

    public override string GetAttackDescription() =>
        "Lifts earth from the ground and hurls it at a single target.";

    public override string GetPassiveDescription() =>
        "Forms a Psionic Bond with a chosen Shooter plant: every time that plant fires, OldCarrot also fires a Psionic OldCarrot at the same target.";

    public override string GetSkillDescription() =>
        "Summons a line of earth pillars that erupt outwards, launching and pushing insects along the wave.";
}
