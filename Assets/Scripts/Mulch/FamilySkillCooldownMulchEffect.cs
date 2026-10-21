using System.Collections.Generic;

// grants every currently-placed and future-placed plant of one family a flat Skill Cooldown
// Reduction bonus - the first mulch effect that pushes a per-plant stat directly (via
// PlantStatApplier) rather than nudging a MulchManager global, since this needs to apply to only
// one family's plants rather than board-wide
public class FamilySkillCooldownMulchEffect : MulchEffect
{
    private readonly FamilySkillCooldownMulchData typedData;
    private readonly List<Plant> affected = new List<Plant>();

    public FamilySkillCooldownMulchEffect(FamilySkillCooldownMulchData data) : base(data)
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
                PlantStatApplier.Apply(plant, StatType.SkillCooldown, -typedData.cooldownReductionPercent);
        affected.Clear();
    }

    public override void OnPlantPlaced(Plant plant) => TryApply(plant);

    private void TryApply(Plant plant)
    {
        if (plant == null || !plant.IsAlive || plant.data == null) return;
        if (plant.data.family != typedData.family) return;
        if (affected.Contains(plant)) return;
        PlantStatApplier.Apply(plant, StatType.SkillCooldown, typedData.cooldownReductionPercent);
        affected.Add(plant);
    }
}
