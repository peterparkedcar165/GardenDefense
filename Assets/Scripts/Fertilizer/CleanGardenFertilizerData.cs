using UnityEngine;

[CreateAssetMenu(fileName = "CleanGardenFertilizer", menuName = "Scriptable Objects/Fertilizer/Clean Garden")]
public class CleanGardenFertilizerData : FertilizerData
{
    // its paper bag icon's symbol, the mulches' categories (proposal, Tommy, 2026-11-24: its epic mulches come every few waves)
    protected override MulchSymbol DefaultSymbol => MulchSymbol.Time;

    [Tooltip("every this many waves, an extra Epic mulch drops at the end of the wave, on top of the guaranteed one")]
    public int everyWaves = 5;

    public override FertilizerEffect CreateEffect() => new CleanGardenFertilizerEffect(this);

    public override string GetDescription() =>
        $"Grants an additional {MulchData.RarityLabel(MulchTier.Epic)} Mulch at the end of every " +
        $"<b><color=green>{everyWaves}</color></b> waves.";
}
