using UnityEngine;

[CreateAssetMenu(fileName = "GardenFenceMulchData", menuName = "Scriptable Objects/Mulch/Garden Fence")]
public class GardenFenceMulchData : MulchData
{
    [Tooltip("leaked insects deal this much less damage to the garden, e.g. 0.25 = 25% less. adds " +
        "onto MulchManager.LeakDamageMultiplier the same way Greedy Growth's increase does")]
    public float leakDamageReduction = 0.25f;

    public override MulchEffect CreateEffect() => new GardenFenceMulchEffect(this);

    public override string GetDescription() =>
        $"Reduce <b><color=red>Leak Damage</color></b> by <b><color=green>{leakDamageReduction * 100f:F0}%</color></b>.";
}
