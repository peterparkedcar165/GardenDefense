using UnityEngine;

[CreateAssetMenu(fileName = "WaterlilyData", menuName = "Scriptable Objects/PlantData/Waterlily")]
public class WaterlilyData : PlantData
{
    public float baseBubblePrisonImpactDamage;
    public float slowProcChance = 0.5f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 5f;
    public float path1AttackRangePerLevel = 0.5f;
    public float path1AttackSpeedPerLevel = 0.3f;
    public float path1MaxOnHitEffectivenessBonus = 0.5f;

    [Header("Path 2 Scaling")]
    public float baseSlowDuration = 6f;
    public int path2MaxSlowStacksPerLevel = 1;
    public float path2SlowProcChancePerLevel = 0.05f;

    [Header("Path 3 Scaling")]
    public float path3BubbleDamagePerLevel = 12f;
    public float path3SkillDurationPerLevel = 2f;
    public float path3RadiusPerLevel = 0.2f;

    public override string GetAttackDescription() =>
        $"Blows little bubbles towards her target, dealing {DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        $"Attacks have a chance to slow their target, and pierce through additional targets.";

    public override string GetSkillDescription() =>
        "Blows a large bubble onto a targeted area, trapping insects within and keeping them airborne for a duration.";
}
