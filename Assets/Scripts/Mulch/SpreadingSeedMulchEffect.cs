using UnityEngine;

// same static flag convention as germination chain: Rhodiola.HealTick calls TrySpread after her
// seed heals the plant it was shot at (never on a widespread bloom bounce), and the most injured
// plant beside that target gets a share of the same heal. a plain heal from her, so her heal
// bonuses apply, but no rejuvenating burgeon
public class SpreadingSeedMulchEffect : MulchEffect
{
    private static SpreadingSeedMulchData active;
    private readonly SpreadingSeedMulchData typedData;

    public SpreadingSeedMulchEffect(SpreadingSeedMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() => active = typedData;

    public override void Deactivate()
    {
        if (active == typedData) active = null;
    }

    public static void TrySpread(Rhodiola rhodiola, Entity target, float healAmount)
    {
        if (active == null || rhodiola == null || target == null || healAmount <= 0f) return;
        // a hair over the radius, so a plant exactly one tile away isn't lost to rounding
        float radius = active.radiusTiles * Tile.CellSize + 0.01f;
        Plant best = null;
        float lowest = 1f;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || plant == target || plant.health >= plant.maxHealth) continue;
            if (Vector2.Distance(plant.transform.position, target.transform.position) > radius) continue;
            float percent = plant.health / plant.maxHealth;
            if (percent < lowest) { lowest = percent; best = plant; }
        }
        if (best != null) best.Heal(healAmount * active.healShare, rhodiola);
    }
}
