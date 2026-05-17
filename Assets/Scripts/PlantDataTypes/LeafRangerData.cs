using UnityEngine;

[CreateAssetMenu(fileName = "LeafRangerData", menuName = "Scriptable Objects/PlantData/LeafRanger")]
public class LeafRangerData : PlantData
{
    public override string GetAttackDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Shoots slow but precise and fierce arrows at his target, dealing <color=green><b>{s.attackDamage}</b></color> <color=green><b>Nature</b></color> <color=#A0522D>Physical</color> damage.";
    }

    public override string GetPassiveDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Attacks can pierce <color=green><b>{s.piercing}</b></color> enemy.";
    }

    public override string GetSkillDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Enters a state of rapid focus, increasing his Attack Speed by <color=green><b>{s.skillAttackSpeedBonus}%</b></color> for <color=green><b>{s.skillDuration}</b></color> seconds.";
    }
}
