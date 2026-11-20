using UnityEngine;

[CreateAssetMenu(fileName = "ScavengerMulch", menuName = "Scriptable Objects/Mulch/Scavenger")]
public class ScavengerMulchData : MulchData
{
    [Tooltip("Item Drop Rate points added to this plant (bonus = X / (X + 100))")]
    public TierValue itemDropRate = TierValue.Scaled(30f);

    public override MulchEffect CreateEffect(MulchItem item) => new ScavengerMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        float points = itemDropRate.Get(r);
        return $"{Green("+" + points.ToString("0"))} Item Drop Rate: insects this plant kills drop mulches " +
            $"{Green(Pct(MulchManager.DropRateBonus(points)))} more often.";
    }
}
