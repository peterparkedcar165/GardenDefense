using System.Collections.Generic;

// heals with no source, so it's a plain heal: no healer stats, no heal crits, and nothing that
// reacts to one plant healing another (heal splash, golden root) fires off it
public class WateringCanMulchEffect : MulchEffect
{
    private readonly WateringCanMulchData typedData;

    public WateringCanMulchEffect(WateringCanMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() {}
    public override void Deactivate() {}

    public override void OnWaveEnded(int wave)
    {
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive) continue;
            plant.Heal(plant.maxHealth * typedData.healPercent);
        }
    }
}
