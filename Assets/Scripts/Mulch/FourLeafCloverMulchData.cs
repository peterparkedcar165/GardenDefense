using UnityEngine;

[CreateAssetMenu(fileName = "FourLeafCloverMulch", menuName = "Scriptable Objects/Mulch/Four-Leaf Clover")]
public class FourLeafCloverMulchData : MulchData
{
    [Tooltip("Rare Item Drop Rate added to this plant, a plain percent: 25 = Rare and Epic weigh 25% more on its drops")]
    public TierValue rareItemDropRate = TierValue.Scaled(25f);

    public override MulchEffect CreateEffect(MulchItem item) => new FourLeafCloverMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"Increase Rare Item Drop Rate by {Green(rareItemDropRate.Get(r).ToString("0") + "%")}.";
    }
}
