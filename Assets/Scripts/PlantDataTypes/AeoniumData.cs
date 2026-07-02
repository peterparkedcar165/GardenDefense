using UnityEngine;

[CreateAssetMenu(fileName = "AeoniumData", menuName = "Scriptable Objects/PlantData/Aeonium")]
public class AeoniumData : PlantData
{
    [Header("Passive Sun Generation")]
    public int   baseSunGenerated      = 6;
    public float baseSunTimerReduction = 0.3f;
    public float sunTimerMPMultiplier  = 0.003f;

    [Header("Skill Blessing of the Sun")]
    public float baseSunYieldBonus     = 0.5f;   // +50% sun yield
    public float path3SunYieldPerLevel = 0.05f;  // extra sun yield per skill level

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 4f;
    public float path1AttackSpeedPerLevel  = 0.04f;
    public float path1AttackRangePerLevel  = 0.2f;

    [Header("Path 2 Scaling")]
    public int   path2SunPerLevel               = 1;
    public float path2SunTimerReductionPerLevel = 0.1f;

    public override string GetAttackDescription() =>
        $"Launches an energy ball dealing {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage to the first insect hit.";

    public override string GetPassiveDescription() =>
        "Generates <color=yellow>Sun</color> periodically. Each projectile attack hit by a plant within her radius reduces the generation timer.";

    public override string GetSkillDescription() =>
        $"Grants all plants within her radius <color=green>Blessing of the Sun</color>, increasing their Sun yield for the duration.";
}
