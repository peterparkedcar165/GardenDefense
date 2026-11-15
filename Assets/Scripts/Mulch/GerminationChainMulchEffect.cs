using UnityEngine;

// same static flag convention as transplant: GerminateEffect checks IsActive when a bloom
// detonates, and asks IsInsideBegoniaAura whether the bloom sits in any living begonia's attack
// radius
public class GerminationChainMulchEffect : MulchEffect
{
    public static bool IsActive;

    public GerminationChainMulchEffect(GerminationChainMulchData data) : base(data) {}

    public override void Activate() => IsActive = true;
    public override void Deactivate() => IsActive = false;

    public static bool IsInsideBegoniaAura(Vector3 position)
    {
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant is not Begonia begonia || begonia == null || !begonia.IsAlive) continue;
            if (Vector2.Distance(begonia.transform.position, position) <= begonia.attackRange) return true;
        }
        return false;
    }
}
