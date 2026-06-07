using UnityEngine;

// inverse of CoolingEffect: raises a plant's temperature toward 10 (comfort neutral) each tick
public class WarmingEffect : StatusEffect
{
    private readonly float warmingPerSecond;

    public WarmingEffect(Entity target, float duration, int level, Entity source, float warmingPerSecond)
        : base(target, duration, level, source)
    {
        this.warmingPerSecond = warmingPerSecond;
        effectType = Type.positive;
    }

    public override void OnApply()  { }
    public override void OnExpire() { }

    public override void OnTick(float deltaTime)
    {
        Plant plant = target as Plant;
        if (plant == null || !plant.IsAlive) return;
        plant.temperature = Mathf.Min(plant.temperature + warmingPerSecond * deltaTime, 10f);
    }

    public override string GetName() => "<color=orange>Warming</color>";
    public override string GetDescription() =>
        $"Increases temperature by <b>{warmingPerSecond:F1}</b> per second, until comfort.";
}
