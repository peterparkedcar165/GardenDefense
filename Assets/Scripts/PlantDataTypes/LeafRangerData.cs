using UnityEngine;

[CreateAssetMenu(fileName = "LeafRangerData", menuName = "Scriptable Objects/PlantData/LeafRanger")]
public class LeafRangerData : PlantData
{
    public float baseSkillAttackSpeedBonus;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 5f;
    public float path1AttackSpeedPerLevel = 0.05f;

    [Header("Path 2 Scaling")]
    public float baseCritChanceBonus = 0.1f;       // bonus crit chance granted at level 0 before upgrades
    public float path2CritChancePerLevel = 0.05f;  // additional crit chance per passive level

    [Header("Path 3 Scaling")]
    public float path3AttackSpeedBonusPerLevel = 0.25f;
    public float path3SkillDurationPerLevel = 0.5f;

    public override string GetAttackDescription() =>
        $"Shoots slow but precise and fierce arrows at his target, dealing <color=green><b>{baseAttackDamage}</b></color> <color=green><b>Nature</b></color> <color=#A0522D>Physical</color> damage.";

    public override string GetPassiveDescription() =>
        $"Attacks can pierce <color=green><b>{basePiercing}</b></color> enemy.";

    public override string GetSkillDescription() =>
        $"Enters a state of rapid focus, increasing his Attack Speed by <color=green><b>{baseSkillAttackSpeedBonus * 100f:F0}%</b></color> for <color=green><b>{baseSkillDuration}</b></color> seconds.";
}
