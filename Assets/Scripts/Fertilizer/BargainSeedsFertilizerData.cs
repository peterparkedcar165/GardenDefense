using UnityEngine;

[CreateAssetMenu(fileName = "BargainSeedsFertilizer", menuName = "Scriptable Objects/Fertilizer/Bargain Seeds")]
public class BargainSeedsFertilizerData : FertilizerData
{
    [Tooltip("flat Sun taken off the first plant placed each wave")]
    public int discount = 15;

    public override FertilizerEffect CreateEffect() => new BargainSeedsFertilizerEffect(this);

    public override string GetDescription() =>
        $"The first plant placed each wave costs {Green(discount.ToString())} less {SunLabel}.";
}
