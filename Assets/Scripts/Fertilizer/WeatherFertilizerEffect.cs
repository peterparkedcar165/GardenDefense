// swaps the level's weather once, on pick (before wave 1), replacing whatever the level would have.
// the weather stays for the level; nothing to undo
public class WeatherFertilizerEffect : FertilizerEffect
{
    private readonly WeatherFertilizerData typedData;

    public WeatherFertilizerEffect(WeatherFertilizerData data) : base(data)
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
