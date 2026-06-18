using UnityEngine;

public class DrizzleBarrierEffect : ShieldEffect
{
    private const float Cap = 120f;
    private const float BaseDuration = 16f;
    private readonly float _scaledDuration;

    public DrizzleBarrierEffect(Entity target, AloeVera source)
        : base(target, BaseDuration * (1f + source.skillDurationMultiplier) + source.skillDurationAdder, 1, source, 0f)
    {
        _scaledDuration = BaseDuration * (1f + source.skillDurationMultiplier) + source.skillDurationAdder;
    }

    public void RefreshDuration() => duration = _scaledDuration;

    public void AddShield(float overflow)
    {
        float added = Mathf.Min(overflow, Cap - amount);
        if (added <= 0f) return;
        amount += added;
        ShieldIndicator.Spawn(target.transform.position, added);
    }

    public override void OnTick(float deltaTime)
    {
        if (WeatherManager.instance?.temperature == TemperatureType.Hot && target is Plant plant)
            plant.temperature = Mathf.Min(plant.temperature, 10f);
    }

    protected override float ShieldCap => Cap;
    protected override string GetShieldDetails() => "Shielded and protected from hot weather.";

    public override string GetName() => "<color=#4FC3F7><b>Drizzle Barrier</b></color>";
}
