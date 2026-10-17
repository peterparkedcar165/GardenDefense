using System.Collections.Generic;

// insects grant bonus Sun on kill (boosts the killing plant's own sunYieldMultiplier, which is
// exactly what SunReward already reads - see Insect.SunReward), but every leak deals extra
// damage (boosts the global MulchManager.LeakDamageMultiplier every insect leak reads)
public class GreedyGrowthMulchEffect : MulchEffect
{
    private readonly GreedyGrowthMulchData typedData;

    public GreedyGrowthMulchEffect(GreedyGrowthMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
            if (plant != null) plant.sunYieldMultiplier += typedData.sunYieldBonus;

        MulchManager.LeakDamageMultiplier += typedData.leakDamageBonus;
    }

    public override void Deactivate()
    {
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
            if (plant != null) plant.sunYieldMultiplier -= typedData.sunYieldBonus;

        MulchManager.LeakDamageMultiplier -= typedData.leakDamageBonus;
    }

    // catches plants placed after this mulch was picked - same reasoning as
    // FertilizerManager.ApplyTo catching plants placed after a mid-level fertilizer pick
    public override void OnPlantPlaced(Plant plant)
    {
        plant.sunYieldMultiplier += typedData.sunYieldBonus;
    }
}
