using System.Collections.Generic;

// grants every currently-placed and future-placed plant a reference-counted HealCanCrit token
// (Entity.AddHealCanCrit/RemoveHealCanCrit), so Entity.Heal's existing crit roll (source.HealCanCrit
// && Random.value < source.criticalChance, +50% on a crit) now applies to any heal a plant gives -
// plant-only, like every other mulch, so an insect healing another insect is never affected
public class CriticalRejuvenationMulchEffect : MulchEffect
{
    private readonly List<Plant> affected = new List<Plant>();

    public CriticalRejuvenationMulchEffect(CriticalRejuvenationMulchData data) : base(data) {}

    public override void Activate()
    {
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
            TryApply(plant);
    }

    public override void Deactivate()
    {
        foreach (Plant plant in affected)
            if (plant != null && plant.IsAlive)
                plant.RemoveHealCanCrit();
        affected.Clear();
    }

    public override void OnPlantPlaced(Plant plant) => TryApply(plant);

    private void TryApply(Plant plant)
    {
        if (plant == null || !plant.IsAlive || affected.Contains(plant)) return;
        plant.AddHealCanCrit();
        affected.Add(plant);
    }
}
