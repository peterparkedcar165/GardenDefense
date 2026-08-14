using UnityEngine;

[CreateAssetMenu(fileName = "WaterlilyData", menuName = "Scriptable Objects/PlantData/Waterlily")]
public class WaterlilyData : PlantData
{
    public float baseAoERange;
    public float baseSplashDamage = 6f;
    public float baseBubblePrisonImpactDamage;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 5f;
    public float path1AttackRangePerLevel = 0.5f;
    public float path1AttackSpeedPerLevel = 0.3f;

    [Header("Path 2 Scaling")]
    public float path2AoERangePerLevel = 0.05f;
    public float path2SplashDamageScalingPerLevel = 0.05f;
    public float baseSlowDuration = 6f;
    public int path2MaxSlowStacksPerLevel = 1;

    [Header("Path 3 Scaling")]
    public float path3BubbleDamagePerLevel = 12f;
    public float path3SkillDurationPerLevel = 2f;
    public float path3RadiusPerLevel = 0.2f;

    public override string GetAttackDescription() =>
        $"Blows little bubbles towards her target, dealing {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage.";

    public override string GetPassiveDescription() =>
        $"Attacks deal {ElementalTag(elementalType)} damage to surrounding insects within a small radius.";

    public override string GetSkillDescription() =>
        "Blows a large bubble onto a targeted area, trapping insects within and keeping them airborne for a duration.";
}
