using UnityEngine;

[CreateAssetMenu(fileName = "WisdomLeafMulch", menuName = "Scriptable Objects/Mulch/Wisdom Leaf")]
public class WisdomLeafMulchData : MulchData
{
    [Tooltip("extra share of exp this plant's type earns for the level")]
    public TierValue expBonus = new TierValue(0.1f, 0.2f, 0.4f);

    public override MulchEffect CreateEffect(MulchItem item) => new WisdomLeafMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"When the level ends, increase this plant's total {Kw("EXP", "green")} gained by {Green(Pct(expBonus.Get(r)))}.";
    }
}
