using UnityEngine;

[CreateAssetMenu(fileName = "BargainSeedsFertilizer", menuName = "Scriptable Objects/Fertilizer/Bargain Seeds")]
public class BargainSeedsFertilizerData : FertilizerData
{
    // its paper bag icon's symbol, the mulches' categories (proposal, Tommy, 2026-11-24: a cheaper plant is sun saved)
    protected override MulchSymbol DefaultSymbol => MulchSymbol.Sun;

    [Tooltip("flat Sun taken off the first plant placed each wave")]
    public int discount = 15;

    public override FertilizerEffect CreateEffect() => new BargainSeedsFertilizerEffect(this);

    public override string GetDescription() =>
        $"The first plant placed each wave costs {Green(discount.ToString())} less {SunLabel}.";
}
