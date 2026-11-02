using System.Collections.Generic;

// grants every currently-placed and future-placed plant a reference-counted DotCanCrit token
// (Entity.AddDotCanCrit/RemoveDotCanCrit) plus a flat bonus to criticalDotChance, same pattern
// as CriticalRejuvenationMulchEffect - plant-only, like every other mulch
public class CriticalRotMulchEffect : MulchEffect
{
    private readonly CriticalRotMulchData typedData;
    private readonly List<Plant> affected = new List<Plant>();

    public CriticalRotMulchEffect(CriticalRotMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
            TryApply(plant);
    }

    public override void Deactivate()
    {
        foreach (Plant plant in affected)
            if (plant != null && plant.IsAlive)
            {
                plant.RemoveDotCanCrit();
                plant.criticalDotChanceAdder -= typedData.dotCritChanceBonus;
            }
        affected.Clear();
    }

    public override void OnPlantPlaced(Plant plant) => TryApply(plant);

    private void TryApply(Plant plant)
    {
        if (plant == null || !plant.IsAlive || affected.Contains(plant)) return;
        plant.AddDotCanCrit();
        plant.criticalDotChanceAdder += typedData.dotCritChanceBonus;
        affected.Add(plant);
    }
}
