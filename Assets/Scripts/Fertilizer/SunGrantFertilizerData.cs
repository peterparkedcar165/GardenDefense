using UnityEngine;

// fertile start: offered on every level, always first in its list (Tommy, 2026-11-21). the sun
// lands the moment the fertilizer is picked
[CreateAssetMenu(fileName = "SunGrantFertilizer", menuName = "Scriptable Objects/Fertilizer/Sun Grant")]
public class SunGrantFertilizerData : FertilizerData
{
    [Tooltip("Sun given the moment this fertilizer is picked")]
    public int sunAmount = 60;

    public override FertilizerEffect CreateEffect() => new SunGrantFertilizerEffect(this);

    public override string GetDescription() =>
        $"Start the level with {Green(sunAmount.ToString())} additional {SunLabel}.";
}
