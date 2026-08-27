using UnityEngine;

/// <summary>
/// Positive effect applied by the Snowdrop to nearby plants.
/// Reduces the plant's temperature by a fixed amount per second while active.
/// </summary>
public class CoolingEffect : PlantAuraBuffEffect
{
    private readonly float coolingPerSecond;

    public CoolingEffect(Entity target, int level, Plant source, float range, float coolingPerSecond)
        : base(target, level, source, range)
    {
        this.coolingPerSecond = coolingPerSecond;
        effectType = Type.positive;
        elementalType = ElementalType.Ice;
    }

    public override string GetName() => "<color=#00FFFF>Cooling</color>";
    public override string GetDescription() => $"Reduces temperature by <b>{coolingPerSecond:F1}</b> per second.";

    public override void OnApply() { }
    public override void OnExpire() { }

    protected override void OnAuraTick(float deltaTime)
    {
        Plant plant = target as Plant;
        if (plant == null || !plant.IsAlive) return;
        plant.temperature = Mathf.Max(plant.temperature - coolingPerSecond * deltaTime, 10f);
    }
}
