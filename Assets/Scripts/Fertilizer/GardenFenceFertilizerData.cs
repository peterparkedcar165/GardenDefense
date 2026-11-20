using UnityEngine;

[CreateAssetMenu(fileName = "GardenFenceFertilizer", menuName = "Scriptable Objects/Fertilizer/Garden Fence")]
public class GardenFenceFertilizerData : FertilizerData
{
    [Tooltip("leaked insects deal this much less damage to the garden, e.g. 0.25 = 25% less")]
    public float leakDamageReduction = 0.25f;

    public override FertilizerEffect CreateEffect() => new GardenFenceFertilizerEffect(this);

    public override string GetDescription() =>
        $"Reduce <b><color=red>Leak Damage</color></b> by <b><color=green>{leakDamageReduction * 100f:F0}%</color></b>.";
}
