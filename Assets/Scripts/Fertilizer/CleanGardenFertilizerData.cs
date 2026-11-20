using UnityEngine;

[CreateAssetMenu(fileName = "CleanGardenFertilizer", menuName = "Scriptable Objects/Fertilizer/Clean Garden")]
public class CleanGardenFertilizerData : FertilizerData
{
    [Tooltip("every this many waves, the mulch guaranteed at the end of the wave is Epic")]
    public int everyWaves = 5;

    public override FertilizerEffect CreateEffect() => new CleanGardenFertilizerEffect(this);

    public override string GetDescription() =>
        $"Every <b><color=green>{everyWaves}</color></b> waves, the mulch guaranteed at the end of the wave is " +
        $"{MulchData.RarityLabel(MulchTier.Epic)}.";
}
