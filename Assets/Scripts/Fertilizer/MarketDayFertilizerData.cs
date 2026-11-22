using UnityEngine;

[CreateAssetMenu(fileName = "MarketDayFertilizer", menuName = "Scriptable Objects/Fertilizer/Market Day")]
public class MarketDayFertilizerData : FertilizerData
{
    [Tooltip("path upgrades cost this much less, e.g. 0.25 = 25% cheaper")]
    public float upgradeCostReduction = 0.15f;
    [Tooltip("placing a plant costs this much more, e.g. 0.25 = 25% more")]
    public float placementCostIncrease = 0.15f;

    public override FertilizerEffect CreateEffect() => new MarketDayFertilizerEffect(this);

    public override string GetDescription() =>
        $"Increase plant cost by {Red(Pct(placementCostIncrease))}, but reduce upgrade cost by {Green(Pct(upgradeCostReduction))}.";
}
