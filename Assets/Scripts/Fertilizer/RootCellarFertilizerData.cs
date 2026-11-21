using UnityEngine;

[CreateAssetMenu(fileName = "RootCellarFertilizer", menuName = "Scriptable Objects/Fertilizer/Root Cellar")]
public class RootCellarFertilizerData : FertilizerData
{
    [Tooltip("share of the banked Sun paid as interest at the end of each wave, e.g. 0.25 = 25%. no cap")]
    public float interestPercent = 0.25f;

    public override FertilizerEffect CreateEffect() => new RootCellarFertilizerEffect(this);

    public override string GetDescription() =>
        $"At the end of each wave, gain {Green(Pct(interestPercent))} of your current {SunLabel} amount.";
}
