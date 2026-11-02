using UnityEngine;

[CreateAssetMenu(fileName = "CriticalRotMulchData", menuName = "Scriptable Objects/Mulch/Critical Rot")]
// lets every plant's DoT damage critically strike - mirrors CriticalRejuvenationMulchData's
// pattern but for DotCanCrit/criticalDotChance instead of HealCanCrit/criticalHealChance
public class CriticalRotMulchData : MulchData
{
    [Tooltip("flat bonus added to criticalDotChance, e.g. 0.1 = +10% DoT crit chance")]
    public float dotCritChanceBonus = 0.1f;

    public override MulchEffect CreateEffect() => new CriticalRotMulchEffect(this);

    public override string GetDescription() =>
        $"DoT Damage can critically strike. +<b><color=green>{dotCritChanceBonus * 100f:F0}%</color></b> DoT Crit Chance.";
}
