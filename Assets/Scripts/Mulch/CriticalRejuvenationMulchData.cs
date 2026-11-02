using UnityEngine;

// lets every plant's heals critically strike, same +50% crit-heal bonus Entity.Heal already
// rolls for any source with HealCanCrit - this just grants that flag board-wide, plus a flat
// bonus to the roll itself via criticalHealChance (stacks additively with criticalChance)
public class CriticalRejuvenationMulchData : MulchData
{
    [Tooltip("flat bonus added to criticalHealChance, e.g. 0.15 = +15% heal crit chance")]
    public float healCritChanceBonus = 0.15f;

    public override MulchEffect CreateEffect() => new CriticalRejuvenationMulchEffect(this);

    public override string GetDescription() =>
        $"Healing can critically strike. +<b><color=green>{healCritChanceBonus * 100f:F0}%</color></b> Heal Crit Chance.";
}
