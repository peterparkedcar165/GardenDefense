using UnityEngine;

[CreateAssetMenu(fileName = "CleanGardenFertilizer", menuName = "Scriptable Objects/Fertilizer/Clean Garden")]
public class CleanGardenFertilizerData : FertilizerData
{
    [Tooltip("every this many waves, an extra Epic mulch drops at the end of the wave, on top of the guaranteed one")]
    public int everyWaves = 5;

    public override FertilizerEffect CreateEffect() => new CleanGardenFertilizerEffect(this);

    public override string GetDescription() =>
        $"Grants an additional {MulchData.RarityLabel(MulchTier.Epic)} Mulch at the end of every " +
        $"<b><color=green>{everyWaves}</color></b> waves.";
}
