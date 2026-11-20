using UnityEngine;

[CreateAssetMenu(fileName = "WideCanopyMulch", menuName = "Scriptable Objects/Mulch/Wide Canopy")]
public class WideCanopyMulchData : MulchData
{
    [Tooltip("how much bigger this plant's splash radius and skill radius get, e.g. 0.2 = 20%")]
    public TierValue areaBonus = TierValue.Scaled(0.2f);

    public override MulchEffect CreateEffect(MulchItem item) => new WideCanopyMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"This plant's splash radius and skill radius are {Green(Pct(areaBonus.Get(r)))} bigger.";
    }
}
