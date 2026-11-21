using UnityEngine;

[CreateAssetMenu(fileName = "RejuvenatorMulch", menuName = "Scriptable Objects/Mulch/Rejuvenator")]
public class RejuvenatorMulchData : MulchData
{
    [Tooltip("how much more every regeneration this plant receives heals it, e.g. 0.35 = 35% more")]
    public TierValue effectivenessBonus = TierValue.Scaled(0.35f);

    public override MulchEffect CreateEffect(MulchItem item) => new RejuvenatorMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"{Kw("Regeneration", "green")} effects received on this plant are {Green(Pct(effectivenessBonus.Get(r)))} more effective.";
    }
}
