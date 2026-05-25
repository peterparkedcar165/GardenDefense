using UnityEngine;

[CreateAssetMenu(fileName = "AcornSproutData", menuName = "Scriptable Objects/PlantData/AcornSprout")]
public class AcornSproutData : PlantData
{
    public float stunChance;
    public float stunDuration;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 8f;

    [Header("Path 2 Scaling")]
    public float path2StunChancePerLevel = 0.05f;
    public float path2StunDurationPerLevel = 0.1f;

    [Header("Path 3 Scaling")]
    public float path3DamageMultiplierPerLevel = 0.25f;
    public float path3SkillDurationPerLevel = 2f;
    public float path3HealthPerLevel = 50f;
    public float path3RadiusPerLevel = 0.15f;

    public override string GetAttackDescription() =>
        $"Shoots acorns towards his target, dealing <color=green><b>{baseAttackDamage}</b></color> <color=green>Nature</color> <color=#A0522D>Physical</color> damage.";

    public override string GetPassiveDescription() =>
        $"Attacks have a <color=green><b>{stunChance * 100f}%</b></color> chance to stun targets for <color=green><b>{stunDuration}</b></color> second.";

    public override string GetSkillDescription()
    {
        float impactDamage = baseAttackDamage * baseSkillDamageMultiplier;
        return $"Hurls a giant acorn from the sky at a targeted location, dealing <color=green><b>{impactDamage:F0}</b></color> <color=green>Nature</color> <color=#A0522D>Physical</color> damage and stunning all insects in the impact radius for <color=green><b>2</b></color> seconds. The acorn then sits on the ground for <color=green><b>{baseSkillDuration}</b></color> seconds, blocking ground insects who stop to gnaw at it. The acorn can sustain <color=green><b>{baseSkillHealth:F0}</b></color> damage.";
    }
}
