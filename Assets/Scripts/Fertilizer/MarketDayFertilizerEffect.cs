// both costs are read live (FertilizerManager.GetPlacementCost and Plant.PathCost), so every price
// shows the change at once
public class MarketDayFertilizerEffect : FertilizerEffect
{
    private readonly MarketDayFertilizerData typedData;

    public MarketDayFertilizerEffect(MarketDayFertilizerData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        FertilizerManager.UpgradeCostMultiplier -= typedData.upgradeCostReduction;
        FertilizerManager.PlacementCostMultiplier += typedData.placementCostIncrease;
    }

    public override void Deactivate()
    {
        FertilizerManager.UpgradeCostMultiplier += typedData.upgradeCostReduction;
        FertilizerManager.PlacementCostMultiplier -= typedData.placementCostIncrease;
    }
}
