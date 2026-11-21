using UnityEngine;

[CreateAssetMenu(fileName = "ScavengerMulch", menuName = "Scriptable Objects/Mulch/Scavenger")]
public class ScavengerMulchData : MulchData
{
    [Tooltip("Mulch Drop Rate added to this plant, in percentage points on top of the base drop chance: 1 = its kills drop a mulch 3% of the time instead of 2%")]
    public TierValue mulchDropRate = new TierValue(1f, 2f, 3f);

    public override MulchEffect CreateEffect(MulchItem item) => new ScavengerMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"Increase Mulch Drop Rate by {Green(mulchDropRate.Get(r).ToString("0.#") + "%")}.";
    }
}
