using UnityEngine;

[CreateAssetMenu(fileName = "FourLeafCloverMulch", menuName = "Scriptable Objects/Mulch/Four-Leaf Clover")]
public class FourLeafCloverMulchData : MulchData
{
    [Tooltip("Rare Item Drop Rate points added to this plant (bonus = X / (X + 100))")]
    public TierValue rareItemDropRate = TierValue.Scaled(25f);

    public override MulchEffect CreateEffect(MulchItem item) => new FourLeafCloverMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        float points = rareItemDropRate.Get(r);
        return $"{Green("+" + points.ToString("0"))} Rare Item Drop Rate: mulches from insects this plant kills are " +
            $"{Green(Pct(MulchManager.DropRateBonus(points)))} more likely to be {RarityLabel(MulchTier.Rare)} or {RarityLabel(MulchTier.Epic)}.";
    }
}
