using UnityEngine;

[CreateAssetMenu(fileName = "QuickSapMulch", menuName = "Scriptable Objects/Mulch/Quick Sap")]
public class QuickSapMulchData : MulchData
{
    [Tooltip("attack speed this plant gains, e.g. 0.25 = 25% faster")]
    public TierValue attackSpeedBonus = TierValue.Scaled(0.25f);
    [Tooltip("damage this plant loses, e.g. 0.25 = 25% less")]
    public TierValue damagePenalty = TierValue.Scaled(0.25f);

    public override MulchEffect CreateEffect(MulchItem item) => new QuickSapMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"This plant attacks {Green(Pct(attackSpeedBonus.Get(r)))} faster but deals {Red(Pct(damagePenalty.Get(r)))} less damage.";
    }
}
