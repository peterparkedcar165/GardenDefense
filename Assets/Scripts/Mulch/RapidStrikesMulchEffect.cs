using System.Collections.Generic;

// Thorn family only. the Attack Speed bonus is pushed per-plant (same pattern as
// FamilySkillCooldownMulchEffect); the bonus damage hooks Entity.OnHit directly rather than
// touching the centralized HandleOnHitEffects dispatcher, since this needs to turn on/off with
// the mulch's own Activate/Deactivate rather than being a permanent built-in check
public class RapidStrikesMulchEffect : MulchEffect
{
    private readonly RapidStrikesMulchData typedData;
    private readonly List<Plant> affected = new List<Plant>();

    public RapidStrikesMulchEffect(RapidStrikesMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
            TryApply(plant);
        Entity.OnHit += HandleHit;
    }

    public override void Deactivate()
    {
        foreach (Plant plant in affected)
            if (plant != null && plant.IsAlive)
                PlantStatApplier.Apply(plant, StatType.AttackSpeed, -typedData.attackSpeedBonus);
        affected.Clear();
        Entity.OnHit -= HandleHit;
    }

    public override void OnPlantPlaced(Plant plant) => TryApply(plant);

    private void TryApply(Plant plant)
    {
        if (plant == null || !plant.IsAlive || plant.data == null) return;
        if (plant.data.family != PlantFamily.Thorn) return;
        if (affected.Contains(plant)) return;
        PlantStatApplier.Apply(plant, StatType.AttackSpeed, typedData.attackSpeedBonus);
        affected.Add(plant);
    }

    private void HandleHit(EntityEventData hit)
    {
        if (hit.source is not Plant thornPlant || thornPlant.data == null) return;
        if (thornPlant.data.family != PlantFamily.Thorn) return;
        if (hit.target is not Insect insect || !insect.IsAlive) return;

        insect.Damage(typedData.flatOnHitDamage, thornPlant.damageType, thornPlant.elementalType,
            thornPlant, false, new DamageTag[] { DamageTag.OnHit }, false, thornPlant.onHitEffectiveness);
    }
}
