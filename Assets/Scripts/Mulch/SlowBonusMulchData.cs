using UnityEngine;

[CreateAssetMenu(fileName = "SlowBonusMulch", menuName = "Scriptable Objects/Mulch/Hinder")]
public class SlowBonusMulchData : MulchData
{
    [Tooltip("extra Movement Speed reduction on every slow this plant applies, e.g. 0.06 = 6% more")]
    public TierValue flatBonus = TierValue.Scaled(0.06f);

    public override MulchEffect CreateEffect(MulchItem item) => new SlowBonusMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"{Kw("Slowing", IceColor)} effects are increased by a flat {Kw(flatBonus.Get(r).ToString("0.##"), IceColor)}.";
    }
}
