using UnityEngine;

public class WinterMoth : Moth
{
    public float coldAuraRadius = 2f;
    public float coldPerSecond = 1.5f;
    private const float SnowEvasionBonus = 0.15f;

    private WinterMothData WMData => data as WinterMothData;

    protected override void Awake()
    {
        base.Awake();
        if (WMData != null)
        {
            coldAuraRadius = WMData.coldAuraRadius;
            coldPerSecond  = WMData.coldPerSecond;
        }
        ApplyEffect(new ColdAuraEffect(this, 1, this, coldAuraRadius, coldPerSecond));
    }

    public override void UpdateStats()
    {
        bool isSnowing = WeatherManager.instance != null && WeatherManager.instance.HasWeather(WeatherType.Snow);
        float bonus = isSnowing ? SnowEvasionBonus : 0f;
        evasionAdder += bonus;
        base.UpdateStats();
        evasionAdder -= bonus;

        // slowing effects can never bring movement speed below its base value
        movementSpeed = Mathf.Max(movementSpeed, baseMovementSpeed);
        flightSpeed = 2f * movementSpeed;
    }
}
