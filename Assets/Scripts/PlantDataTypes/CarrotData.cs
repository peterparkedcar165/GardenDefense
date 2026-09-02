using UnityEngine;

[CreateAssetMenu(fileName = "CarrotData", menuName = "Scriptable Objects/PlantData/Carrot")]
public class CarrotData : PlantData
{
    [Header("Path 1 Scaling (Eruption Strike)")]
    public float path1AttackDamagePerLevel = 8f;
    public float path1CritChancePerLevel = 0.05f;
    [Tooltip("radius around the struck insect that also takes the eruption's damage")]
    public float baseEruptionRadius = 1f;
    public float path1RadiusPerLevel = 0.15f;
    [Tooltip("knock up force applied to burrowed insects hit (always) and to every insect hit on a critical strike (path1 max)")]
    public float eruptionKnockUpForce = 6f;

    [Header("Path 2 Scaling (Soil Bond)")]
    [Tooltip("how much closer to its next attack Carrot gets every time the bonded plant fires")]
    public float bondCountdownBonus = 0.3f;
    public float path2CountdownBonusPerLevel = 0.05f;
    public float path2AttackRangePerLevel = 0.15f;

    [Header("Path 3 Scaling (Fault Line)")]
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
    [Tooltip("carrot width grows by this fraction per level (hitbox, spacing, and visual scale all together); knock-aside distance always matches the current width")]
    public float path3WidthPerLevel = 0.1f;

    [Header("Furrow Visuals")]
    public float visualFadeIn = 0.1f;
    public float visualHold = 0.7f;
    public float visualFadeOut = 0.5f;
    [Tooltip("random x and y offset applied to each carrot visual on spawn")]
    public float visualPositionJitter = 0.05f;

    public override string GetAttackDescription() =>
        "Erupts a chunk of earth beneath a target insect, striking it and any insects caught nearby.";

    public override string GetPassiveDescription() =>
        "Forms a Soil Bond with a chosen Shooter plant, sharing its targeting range and quickening Carrot's own attacks whenever it fires.";

    public override string GetSkillDescription() =>
        "Summons a line of earth pillars that erupt outwards, launching and pushing insects along the wave.";
}
