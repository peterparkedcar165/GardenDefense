using UnityEngine;

[CreateAssetMenu(fileName = "GreedyGrowthMulchData", menuName = "Scriptable Objects/Mulch/Greedy Growth")]
public class GreedyGrowthMulchData : MulchData
{
    [Tooltip("extra Sun yield from insect kills, e.g. 0.4 = 40% more")]
    public float sunYieldBonus = 0.4f;
    [Tooltip("extra leak damage as a multiplier bonus, e.g. 1 = leaks deal double (1 + 1 = 2x)")]
    public float leakDamageBonus = 1f;

    public override MulchEffect CreateEffect() => new GreedyGrowthMulchEffect(this);
}
