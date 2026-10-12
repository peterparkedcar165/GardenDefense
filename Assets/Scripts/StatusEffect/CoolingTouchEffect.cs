using UnityEngine;

// Cooling Touch (skill tree): applied to any plant Aloe Vera's attack heals, granting a brief
// burst of Heat Resistance - mirrors SteamEffect's adder-on-apply/revert-on-expire idiom
public class CoolingTouchEffect : StatusEffect
{
    private const float HeatResistanceBonus = 0.25f;

    public CoolingTouchEffect(Entity target, Entity source) : base(target, 4f, 1, source)
    {
        effectType = Type.positive;
        elementalType = ElementalType.Water;
    }

    // heatResistanceAdder is Plant-specific (the comfort/temperature system doesn't apply to
    // insects), not a generic Entity field - target is always a Plant in practice since this is
    // only ever applied from AloeVeraProjectile's plant-heal loop
    public override void OnApply() { if (target is Plant plant) plant.heatResistanceAdder += HeatResistanceBonus; }
    public override void OnExpire() { if (target is Plant plant) plant.heatResistanceAdder -= HeatResistanceBonus; }

    public override string GetName() => "<color=#4FC3F7>Cooling Touch</color>";
    public override string GetDescription() =>
        $"Heat Resistance increased by <color=green><b>{HeatResistanceBonus * 100f:F0}%</b></color>.";
}
