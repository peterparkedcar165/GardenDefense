// one-shot board state change: swaps the level's current weather to whatever WeatherMulchData
// authored, replacing anything already active (same as the player picking a different weather
// entirely). nothing to undo on Deactivate - the level's own weather system carries on from here
public class WeatherMulchEffect : MulchEffect
{
    private readonly WeatherMulchData typedData;

    public WeatherMulchEffect(WeatherMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (WeatherManager.instance == null) return;
        WeatherManager.instance.ClearAllWeather();
        if (typedData.weather != WeatherType.Clear)
            WeatherManager.instance.SetWeather(typedData.weather, typedData.intensity);
    }

    public override void Deactivate() {}
}
