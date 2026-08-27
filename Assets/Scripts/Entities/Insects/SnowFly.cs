using UnityEngine;

public class SnowFly : FlyingInsect
{
    public float snowArmorBonus = 25f;

    private SnowFlyData SFData => data as SnowFlyData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        if (SFData != null)
            snowArmorBonus = SFData.snowArmorBonus;
    }

    public override void UpdateStats()
    {
        bool isSnowing = WeatherManager.instance != null && WeatherManager.instance.HasWeather(WeatherType.Snow);
        float bonus = isSnowing ? snowArmorBonus : 0f;
        armorAdder += bonus;
        magicArmorAdder += bonus;
        base.UpdateStats();
        armorAdder -= bonus;
        magicArmorAdder -= bonus;

        // slowing effects can never bring movement speed below its base value
        movementSpeed = Mathf.Max(movementSpeed, baseMovementSpeed);
        flightSpeed = 2f * movementSpeed;
    }
}
