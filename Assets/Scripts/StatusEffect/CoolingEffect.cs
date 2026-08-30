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
        // gated continuously, not just at the moment this effect was granted: if the weather
        // ever shifts away from Hot after this was applied (e.g. leftover from a brief hot
        // spell, or a plant placed nearby right as it ended), Snowdrop should stop cooling
        // instead of leaving plants stuck with a stale reduction it's no longer meant to grant
        if (WeatherManager.instance == null || WeatherManager.instance.temperature != TemperatureType.Hot) return;

        Plant plant = target as Plant;
        if (plant == null || !plant.IsAlive) return;
        plant.temperature = Mathf.Max(plant.temperature - coolingPerSecond * deltaTime, 10f);
    }
}
