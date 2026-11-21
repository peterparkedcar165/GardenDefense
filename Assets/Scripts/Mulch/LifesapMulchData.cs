using UnityEngine;

[CreateAssetMenu(fileName = "LifesapMulch", menuName = "Scriptable Objects/Mulch/Lifesap")]
public class LifesapMulchData : MulchData
{
    [Tooltip("share of the damage this plant deals that heals it, e.g. 0.08 = 8%")]
    public TierValue lifesteal = TierValue.Scaled(0.08f);

    public override MulchEffect CreateEffect(MulchItem item) => new LifesapMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"Increase {Kw("Lifesteal", LifestealColor)} by {Green(Pct(lifesteal.Get(r)))}.";
    }
}
