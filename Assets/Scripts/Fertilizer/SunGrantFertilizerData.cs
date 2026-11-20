using UnityEngine;

// fertile start, rich loam and black gold: one data class, three assets, one per tier. the sun
// lands the moment the fertilizer is picked
[CreateAssetMenu(fileName = "SunGrantFertilizer", menuName = "Scriptable Objects/Fertilizer/Sun Grant")]
public class SunGrantFertilizerData : FertilizerData
{
    [Tooltip("Sun given the moment this fertilizer is picked")]
    public int sunAmount = 80;

    public override FertilizerEffect CreateEffect() => new SunGrantFertilizerEffect(this);

    public override string GetDescription() =>
        $"Start the level with <b><color=green>{sunAmount}</color></b> more <b><color=#FFD700>Sun</color></b>.";
}
