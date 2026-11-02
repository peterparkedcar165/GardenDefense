using System.Collections.Generic;

public class IronbarkCounterMulchEffect : MulchEffect
{
    private readonly IronbarkCounterMulchData typedData;
    private readonly List<Plant> affected = new List<Plant>();

    public IronbarkCounterMulchEffect(IronbarkCounterMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
            TryApply(plant);
        Entity.OnEntityHit += HandleHit;
    }

    public override void Deactivate()
    {
        Entity.OnEntityHit -= HandleHit;
        foreach (Plant plant in affected)
            if (plant != null && plant.IsAlive)
                PlantStatApplier.Apply(plant, StatType.CounterDamage, -typedData.counterDamageBonus);
        affected.Clear();
    }

    public override void OnPlantPlaced(Plant plant) => TryApply(plant);

    private void TryApply(Plant plant)
    {
        if (plant == null || !plant.IsAlive || plant.data == null) return;
        if (affected.Contains(plant)) return;
        PlantStatApplier.Apply(plant, StatType.CounterDamage, typedData.counterDamageBonus);
        affected.Add(plant);
    }

    private void HandleHit(EntityEventData hit)
    {
        if (hit.source is not Plant ironbarkPlant || ironbarkPlant.data == null) return;
        if (hit.tags == null || !System.Array.Exists(hit.tags, t => t == DamageTag.Counter)) return;

        ironbarkPlant.ApplyEffect(new IronbarkCounterShieldEffect(
            ironbarkPlant, ironbarkPlant, hit.damage * typedData.shieldPercentOfDamage, typedData.shieldDuration));
    }
}
