using UnityEngine;

// applied to a plant after a Gloriosa ember heals it (path 2 max level)
// regenerates health and warms temperature over 2 seconds in 0.5s ticks for half the amount restored
public class HeatedComfortEffect : StatusEffect
{
    private const float Duration     = 4f;
    private const float TickInterval = 0.5f;

    private readonly float _healPerTick;
    private readonly float _tempPerTick;
    private float _tickTimer;

    public HeatedComfortEffect(Entity target, float totalHeal, float totalTemp, Entity source)
        : base(target, Duration, 1, source)
    {
        int ticks      = Mathf.RoundToInt(Duration / TickInterval);
        _healPerTick   = totalHeal / ticks;
        _tempPerTick   = totalTemp / ticks;
        effectType      = Type.positive;
        elementalType   = ElementalType.Fire;
        sourceStackable = true;
    }

    public override void OnApply()  { }
    public override void OnExpire() { }

    public override void OnTick(float deltaTime)
    {
        _tickTimer += deltaTime;
        if (_tickTimer < TickInterval) return;
        _tickTimer -= TickInterval;
        target.Heal(_healPerTick, source);
        Plant plant = target as Plant;
        if (plant != null && WeatherManager.instance?.temperature == TemperatureType.Cold)
            plant.temperature = Mathf.Min(plant.temperature + _tempPerTick, plant.comfortMax);
    }

    public override string GetName() => "<color=orange><b>Heated Comfort</b></color>";
    public override string GetDescription() =>
        $"Recovering <color=green><b>{_healPerTick:F1}</b></color> health and warming <color=orange><b>{_tempPerTick:F2}°</b></color> every <color=green><b>{TickInterval}s</b></color>.";
}
