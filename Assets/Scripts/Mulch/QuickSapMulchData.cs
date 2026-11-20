using UnityEngine;

[CreateAssetMenu(fileName = "QuickSapMulch", menuName = "Scriptable Objects/Mulch/Quick Sap")]
public class QuickSapMulchData : MulchData
{
    [Tooltip("attack speed this plant gains, e.g. 0.25 = 25% faster")]
    public TierValue attackSpeedBonus = TierValue.Scaled(0.25f);
    [Tooltip("Attack Damage this plant loses, e.g. 0.25 = 25% less (of its total Attack Damage)")]
    public TierValue damagePenalty = TierValue.Scaled(0.25f);

    public override MulchEffect CreateEffect(MulchItem item) => new QuickSapMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"Increase <color=green><b>Attack Speed</b></color> by {Green(Pct(attackSpeedBonus.Get(r)))}, " +
            $"but reduce <color=green><b>Attack Damage</b></color> by {Red(Pct(damagePenalty.Get(r)))}.";
    }
}
