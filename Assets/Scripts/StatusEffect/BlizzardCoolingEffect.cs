using UnityEngine;

// Cooling applied by the Blizzard skill zone. Unlike Snowdrop's passive CoolingEffect (a
// permanent aura tied to a stationary plant), the Blizzard is itself a short-lived, moving
// skill object that already re-checks its own beam shape every frame and destroys itself
// when its duration ends, so a short per-tick reapplication is correct here and not the
// pattern that caused the stacked-aura jitter elsewhere.
public class BlizzardCoolingEffect : StatusEffect
{
    private readonly float coolingPerSecond;

    public BlizzardCoolingEffect(Entity target, float duration, int level, Entity source, float coolingPerSecond)
        : base(target, duration, level, source)
    {
        this.coolingPerSecond = coolingPerSecond;
        effectType = Type.positive;
        elementalType = ElementalType.Ice;
    }

    public override string GetName() => "<color=#00FFFF>Cooling</color>";
    public override string GetDescription() => $"Reduces temperature by <b>{coolingPerSecond:F1}</b> per second.";

    public override void OnApply() { }

    public override void OnTick(float deltaTime)
    {
        Plant plant = target as Plant;
        if (plant == null || !plant.IsAlive) return;
        plant.temperature = Mathf.Max(plant.temperature - coolingPerSecond * deltaTime, 10f);
    }

    public override void OnExpire() { }
}
