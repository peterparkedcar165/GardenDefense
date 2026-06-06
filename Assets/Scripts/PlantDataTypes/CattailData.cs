using UnityEngine;

[CreateAssetMenu(fileName = "CattailData", menuName = "Scriptable Objects/PlantData/Cattail")]
public class CattailData : PlantData
{
    [Header("Passive")]
    public float groundDuration          = 6f;
    public float baseCritDamageConversion = 1f;

    [Header("Path 1 Attack")]
    public float path1AttackDamagePerLevel = 5f;
    public float path1AttackSpeedPerLevel  = 0.05f;

    [Header("Path 2 Passive")]
    public float baseCritChanceBonus              = 0.1f;
    public float path2CritChancePerLevel          = 0.08f;
    public float path2GroundDurationPerLevel      = 0.5f;
    public float path2CritDamageConversionPerLevel = 0.5f;

    [Header("Path 3 Skill (Pressure Shot)")]
    public float skillLaneLength           = 30f;
    public float skillLaneWidth            = 1f;
    public float baseSkillCritGrant        = 0.5f;
    public float path3CritGrantPerLevel    = 0.1f;
    public float baseSkillAttackSpeedBonus = 2f;
    public float path3AttackSpeedPerLevel  = 0.25f;
    public float path3DurationPerLevel     = 0.5f;

    public override string GetAttackDescription() =>
        $"Fires a high-pressure water dart, dealing <color=green><b>{baseAttackDamage}</b></color> {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage to a single target at any range.";

    public override string GetPassiveDescription() =>
        $"Every <color=green><b>1%</b></color> of Critical Chance above <color=green><b>100%</b></color> will be converted to <color=green><b>1%</b></color> Critical Damage. " +
        $"The Cattail's shots against flying insects will Ground them for <color=green><b>{groundDuration:F1}s</b></color>.";

    public override string GetSkillDescription() =>
        $"Aim a direction and rain darts down the lane with massively increased Attack Speed and Critical Chance for <color=green><b>{baseSkillDuration:F0}s</b></color>.";
}
