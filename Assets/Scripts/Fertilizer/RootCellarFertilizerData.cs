using UnityEngine;

[CreateAssetMenu(fileName = "RootCellarFertilizer", menuName = "Scriptable Objects/Fertilizer/Root Cellar")]
public class RootCellarFertilizerData : FertilizerData
{
    // its paper bag icon's symbol, the mulches' categories (proposal, Tommy, 2026-11-24: sun interest at the end of every wave)
    protected override MulchSymbol DefaultSymbol => MulchSymbol.Time;

    [Tooltip("share of the banked Sun paid as interest at the end of each wave, e.g. 0.25 = 25%. no cap")]
    public float interestPercent = 0.25f;

    public override FertilizerEffect CreateEffect() => new RootCellarFertilizerEffect(this);

    public override string GetDescription() =>
        $"At the end of each wave, gain {Green(Pct(interestPercent))} of your current {SunLabel} amount.";
}
