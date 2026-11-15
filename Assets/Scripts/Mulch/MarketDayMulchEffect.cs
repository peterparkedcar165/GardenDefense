// both costs are read live (MulchManager.GetPlacementCost and Plant.PathCost), so the change shows
// on every price the moment it's picked and goes away the moment it's removed
public class MarketDayMulchEffect : MulchEffect
{
    private readonly MarketDayMulchData typedData;

    public MarketDayMulchEffect(MarketDayMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        MulchManager.UpgradeCostMultiplier -= typedData.upgradeCostReduction;
        MulchManager.PlacementCostMultiplier += typedData.placementCostIncrease;
    }

    public override void Deactivate()
    {
        MulchManager.UpgradeCostMultiplier += typedData.upgradeCostReduction;
        MulchManager.PlacementCostMultiplier -= typedData.placementCostIncrease;
    }
}
