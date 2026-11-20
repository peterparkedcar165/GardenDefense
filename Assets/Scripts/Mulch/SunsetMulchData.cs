using UnityEngine;

[CreateAssetMenu(fileName = "SunsetMulch", menuName = "Scriptable Objects/Mulch/Sunset")]
public class SunsetMulchData : MulchData
{
    [Tooltip("extra sun ticks this plant makes when a wave ends")]
    public TierValue extraTicks = new TierValue(1f, 2f, 3f);

    public override MulchEffect CreateEffect(MulchItem item) => new SunsetMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        int n = extraTicks.GetInt(r);
        string ticks = n == 1 ? "one extra time" : $"{Green(n.ToString())} extra times";
        return $"When a wave ends, this plant makes its {SunLabel} {ticks}. Only works on a plant that makes {SunLabel} (sun sap counts).";
    }
}
