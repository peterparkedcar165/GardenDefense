using UnityEngine;

[CreateAssetMenu(fileName = "LeafRangerData", menuName = "Scriptable Objects/PlantData/LeafRanger")]
public class LeafRangerData : PlantData
{
    public float baseSkillAttackSpeedBonus;

    public override string GetAttackDescription() =>
        $"Shoots slow but precise and fierce arrows at his target, dealing <color=green><b>{baseAttackDamage}</b></color> <color=green><b>Nature</b></color> <color=#A0522D>Physical</color> damage.";

    public override string GetPassiveDescription() =>
        $"Attacks can pierce <color=green><b>{basePiercing}</b></color> enemy.";

    public override string GetSkillDescription() =>
        $"Enters a state of rapid focus, increasing his Attack Speed by <color=green><b>{baseSkillAttackSpeedBonus * 100f:F0}%</b></color> for <color=green><b>{baseSkillDuration}</b></color> seconds.";
}
