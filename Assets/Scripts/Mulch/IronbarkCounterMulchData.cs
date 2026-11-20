using UnityEngine;

[CreateAssetMenu(fileName = "IronbarkCounterMulch", menuName = "Scriptable Objects/Mulch/Ironclad Riposte")]
public class IronbarkCounterMulchData : MulchData
{
    [Tooltip("Counter Damage added to this plant, e.g. 0.25 = +25%")]
    public TierValue counterDamageBonus = TierValue.Scaled(0.25f);
    [Tooltip("shield when its Counter Damage lands, as a share of that hit's damage")]
    public TierValue shieldPercentOfDamage = TierValue.Scaled(0.05f);
    [Tooltip("how long that shield lasts")]
    public float shieldDuration = 4f;

    public override MulchEffect CreateEffect(MulchItem item) => new IronbarkCounterMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"This plant deals {Green(Pct(counterDamageBonus.Get(r)))} more Counter Damage. Its Counter Damage hits shield it for " +
            $"{Green(Pct(shieldPercentOfDamage.Get(r)))} of the damage dealt, for {Green(shieldDuration.ToString("0.#") + "s")}.";
    }
}
