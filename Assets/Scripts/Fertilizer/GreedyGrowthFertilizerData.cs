using UnityEngine;

[CreateAssetMenu(fileName = "GreedyGrowthFertilizer", menuName = "Scriptable Objects/Fertilizer/Greedy Growth")]
public class GreedyGrowthFertilizerData : FertilizerData
{
    // its paper bag icon's symbol, the mulches' categories (proposal, Tommy, 2026-11-24: more sun from kills)
    protected override MulchSymbol DefaultSymbol => MulchSymbol.Sun;

    [Tooltip("extra Sun Yield on every plant, e.g. 0.25 = 25% more: the Sun its kills pay and the Sun it makes itself (Tommy, 2026-11-21: both)")]
    public float sunYieldBonus = 0.25f;
    [Tooltip("extra leak damage as a multiplier bonus, e.g. 1 = leaks deal double (1 + 1 = 2x)")]
    public float leakDamageBonus = 1f;

    public override FertilizerEffect CreateEffect() => new GreedyGrowthFertilizerEffect(this);

    public override string GetDescription() =>
        $"Increase {SunLabel} yield by {Green(Pct(sunYieldBonus))}, " +
        $"but increase the damage you take from insects escaping by {Red(Pct(leakDamageBonus))}.";
}
