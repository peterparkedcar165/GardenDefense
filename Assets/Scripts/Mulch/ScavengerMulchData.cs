using UnityEngine;

[CreateAssetMenu(fileName = "ScavengerMulch", menuName = "Scriptable Objects/Mulch/Scavenger")]
public class ScavengerMulchData : MulchData
{
    [Tooltip("Item Drop Rate added to this plant, a plain percent: 30 = its kills drop mulches 30% more often")]
    public TierValue itemDropRate = TierValue.Scaled(30f);

    public override MulchEffect CreateEffect(MulchItem item) => new ScavengerMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"Increase Item Drop Rate by {Green(itemDropRate.Get(r).ToString("0") + "%")}.";
    }
}
