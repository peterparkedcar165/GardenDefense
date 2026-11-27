using UnityEngine;

[CreateAssetMenu(fileName = "CompostTeaMulch", menuName = "Scriptable Objects/Mulch/Compost Tea")]
public class CompostTeaMulchData : MulchData
{
    [Tooltip("share of this plant's max health it heals for each kill it helped with, e.g. 0.02 = 2%")]
    public TierValue healPercentPerKill = TierValue.Scaled(0.02f);

    public override MulchEffect CreateEffect(MulchItem item) => new CompostTeaMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        // kills and assists alike, as the effect counts them (Tommy, 2026-11-27: the text said assists only)
        return $"Kills and assists scored by this plant heal it for {Green(Pct(healPercentPerKill.Get(r)))} of its {Kw("Max Health", HealthColor)}.";
    }
}
