using UnityEngine;

[CreateAssetMenu(fileName = "HollyData", menuName = "Scriptable Objects/PlantData/Holly")]
public class HollyData : PlantData
{
    public float baseRetaliationHollyPercent;
    public float baseRetaliationInsectPercent;
    public float baseHealthBonusMP;
    public float baseFrozenRageReduction;
    public float baseFrozenRageReductionMP;

    public override string GetAttackDescription() =>
        $"Releases icy thorns dealing <color=green><b>{baseAttackDamage:F0}</b></color> <color=#00FFFF>Ice</color> <color=#FFB6C1>Magic</color> damage to all insects within range.";

    public override string GetPassiveDescription() =>
        $"Insects that attack Holly retaliate for <color=green><b>{baseRetaliationHollyPercent * 100f:F0}%</b></color> of Holly's Attack Damage + " +
        $"<color=green><b>{baseRetaliationInsectPercent * 100f:F0}%</b></color> of the attacker's Attack Damage. " +
        $"Increases Max Health with <color=#FFB6C1><b>{baseHealthBonusMP * 100f:F0}%</b></color> Magic Power.";

    public override string GetSkillDescription() =>
        $"Enter a taunting state for <color=green><b>{baseSkillDuration:F0}s</b></color>. Insects within range are afflicted with " +
        $"<color=#00FFFF><b>Frozen Rage</b></color>, forcing them to target Holly and reducing their Physical Resistance by " +
        $"<color=green><b>{baseFrozenRageReduction * 100f:F0}%</b></color>. " +
        $"Scales with <color=#FFB6C1><b>{baseFrozenRageReductionMP * 100f:F0}%</b></color> Magic Power.";
}
