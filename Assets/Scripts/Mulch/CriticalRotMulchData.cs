using UnityEngine;

[CreateAssetMenu(fileName = "CriticalRotMulch", menuName = "Scriptable Objects/Mulch/Critical Rot")]
public class CriticalRotMulchData : MulchData
{
    [Tooltip("damage over time crit chance added to this plant, e.g. 0.25 = +25%")]
    public TierValue dotCritChance = TierValue.Scaled(0.25f);

    public override MulchEffect CreateEffect(MulchItem item) => new CriticalRotMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"{Kw("Damage Over Time", PoisonColor)} dealt by the plant may {Kw("critically strike", CritColor)}.\n\n" +
            $"Increase {Kw("DoT Critical Chance", PoisonColor)} by {Green(Pct(dotCritChance.Get(r)))}.";
    }
}
