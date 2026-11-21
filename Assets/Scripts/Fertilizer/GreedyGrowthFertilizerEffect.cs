using System.Collections.Generic;

// every plant's sunYieldMultiplier goes up, which Insect.SunReward reads for the sun a kill pays and
// Plant.GenerateSun for the sun a plant makes (both, Tommy 2026-11-21), but every leak hurts more
// (FertilizerManager.LeakDamageMultiplier)
public class GreedyGrowthFertilizerEffect : FertilizerEffect
{
    private readonly GreedyGrowthFertilizerData typedData;
    private readonly List<Plant> affected = new List<Plant>();

    public GreedyGrowthFertilizerEffect(GreedyGrowthFertilizerData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
            OnPlantPlaced(plant);
        FertilizerManager.LeakDamageMultiplier += typedData.leakDamageBonus;
    }

    public override void Deactivate()
    {
        foreach (Plant plant in affected)
            if (plant != null) plant.sunYieldMultiplier -= typedData.sunYieldBonus;
        affected.Clear();
        FertilizerManager.LeakDamageMultiplier -= typedData.leakDamageBonus;
    }

    public override void OnPlantPlaced(Plant plant)
    {
        if (plant == null || affected.Contains(plant)) return;
        plant.sunYieldMultiplier += typedData.sunYieldBonus;
        affected.Add(plant);
    }
}
