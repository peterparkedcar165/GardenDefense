using UnityEngine;

[CreateAssetMenu(fileName = "RejuvenatorMulch", menuName = "Scriptable Objects/Mulch/Rejuvenator")]
public class RejuvenatorMulchData : MulchData
{
    [Tooltip("how much more every regeneration this plant gives heals, e.g. 0.35 = 35% more")]
    public TierValue effectivenessBonus = TierValue.Scaled(0.35f);

    public override MulchEffect CreateEffect(MulchItem item) => new RejuvenatorMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"<b><color=#FF6B81>Regeneration</color></b> this plant gives heals {Green(Pct(effectivenessBonus.Get(r)))} more.";
    }
}
