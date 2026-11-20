using UnityEngine;

[CreateAssetMenu(fileName = "MarketDayFertilizer", menuName = "Scriptable Objects/Fertilizer/Market Day")]
public class MarketDayFertilizerData : FertilizerData
{
    [Tooltip("path upgrades cost this much less, e.g. 0.25 = 25% cheaper")]
    public float upgradeCostReduction = 0.25f;
    [Tooltip("placing a plant costs this much more, e.g. 0.25 = 25% more")]
    public float placementCostIncrease = 0.25f;

    public override FertilizerEffect CreateEffect() => new MarketDayFertilizerEffect(this);

    public override string GetDescription() =>
        $"Path upgrades cost <b><color=green>{upgradeCostReduction * 100f:F0}%</color></b> less <b><color=#FFD700>Sun</color></b>. " +
        $"Placing plants costs <b><color=red>{placementCostIncrease * 100f:F0}%</color></b> more.";
}
