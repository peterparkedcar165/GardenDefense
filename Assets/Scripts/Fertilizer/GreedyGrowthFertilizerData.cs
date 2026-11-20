using UnityEngine;

[CreateAssetMenu(fileName = "GreedyGrowthFertilizer", menuName = "Scriptable Objects/Fertilizer/Greedy Growth")]
public class GreedyGrowthFertilizerData : FertilizerData
{
    [Tooltip("extra Sun yield from insect kills, e.g. 0.25 = 25% more")]
    public float sunYieldBonus = 0.25f;
    [Tooltip("extra leak damage as a multiplier bonus, e.g. 1 = leaks deal double (1 + 1 = 2x)")]
    public float leakDamageBonus = 1f;

    public override FertilizerEffect CreateEffect() => new GreedyGrowthFertilizerEffect(this);

    public override string GetDescription() =>
        $"Increase <b><color=#FFD700>Sun Yield</color></b> from insect kills by <b><color=green>{sunYieldBonus * 100f:F0}%</color></b>. " +
        $"Increase <b><color=red>Leak Damage</color></b> by <b><color=red>{leakDamageBonus * 100f:F0}%</color></b>.";
}
