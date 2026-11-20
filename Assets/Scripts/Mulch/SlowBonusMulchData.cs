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
        return $"Every slow this plant applies takes away {Green(Pct(flatBonus.Get(r)))} more <b><color=green>Movement Speed</color></b>.";
    }
}
