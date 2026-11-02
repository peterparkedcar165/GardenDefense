using UnityEngine;

[CreateAssetMenu(fileName = "WaterlilyData", menuName = "Scriptable Objects/PlantData/Waterlily")]
public class WaterlilyData : PlantData
{
    public float baseAoERange;
    [Tooltip("splash damage as a fraction of the bubble's own attack damage, e.g. 0.5 = half")]
    public float splashDamageRatio = 0.5f;
    public float baseBubblePrisonImpactDamage;
    [Tooltip("base number of nearest insects around the main target that splash hits (the main target is always hit on top)")]
    public int splashMaxExtraTargets = 2;
    [Tooltip("Soaked stack cap, flat, no per level scaling")]
    public int maxSoakStacks = 4;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 5f;
    public float path1AttackRangePerLevel = 0.5f;
    public int path1SplashTargetsPerLevel = 1;
    [Tooltip("attack path max bonus: damaging an insect already at max Soaked stacks stuns it for this long")]
    public float maxSoakStunDuration = 0.5f;

    [Header("Path 2 Scaling")]
    public float path2AoERangePerLevel = 0.05f;
    public float baseSoakDuration = 4f;
    public float path2SoakDurationPerLevel = 0.5f;
    [Tooltip("seconds of Soaked duration per 100 Magic Power")]
    public float soakDurationMP = 1f;

    [Header("Path 3 Scaling")]
    [Tooltip("extra damage imprisoned insects take from all sources, e.g. 0.08 = +8%")]
    public float baseBubbleDamageTaken = 0.08f;
    public float path3DamageTakenPerLevel = 0.04f;
    [Tooltip("extra damage taken per 100 Magic Power, e.g. 0.05 = +5%")]
    public float bubbleDamageTakenMP = 0.05f;
    [Tooltip("seconds of Bubble Prison duration per 100 Magic Power")]
    public float bubbleDurationMP = 2f;
    public float path3SkillDurationPerLevel = 2f;
    public float path3RadiusPerLevel = 0.2f;

    public override string GetAttackDescription() =>
        $"Blows little bubbles towards her target, dealing {DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        $"Attacks splash surrounding insects for half damage. Everyone hit is <color=#4FC3F7>Soaked</color>, a stacking slow that also grounds flying insects.";

    public override string GetSkillDescription() =>
        "Blows a large bubble onto a targeted area, trapping insects within and keeping them airborne for a duration. Trapped insects take increased damage from all sources.";
}
