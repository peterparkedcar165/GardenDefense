using UnityEngine;

// one data type for every "set the level's weather" fertilizer (sunny, rainy and snowy day): one
// asset per weather
[CreateAssetMenu(fileName = "WeatherFertilizer", menuName = "Scriptable Objects/Fertilizer/Set Weather")]
public class WeatherFertilizerData : FertilizerData
{
    public WeatherType weather = WeatherType.Sunny;
    public int intensity = 1;

    public override FertilizerEffect CreateEffect() => new WeatherFertilizerEffect(this);

    public override string GetDescription() =>
        $"The level's weather is <b>{WeatherManager.GetWeatherName(weather)}</b> instead of its usual weather.";
}
