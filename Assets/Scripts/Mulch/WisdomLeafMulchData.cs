using UnityEngine;

[CreateAssetMenu(fileName = "WisdomLeafMulch", menuName = "Scriptable Objects/Mulch/Wisdom Leaf")]
public class WisdomLeafMulchData : MulchData
{
    [Tooltip("extra share of exp this plant's type earns for the level")]
    public TierValue expBonus = new TierValue(0.1f, 0.2f, 0.3f);

    public override MulchEffect CreateEffect(MulchItem item) => new WisdomLeafMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"At the end of the level, this plant's type earns {Green(Pct(expBonus.Get(r)))} more EXP for the whole level. " +
            $"Wisdom Leafs on plants of the same type add up. The plant must still be alive at the end.";
    }
}
