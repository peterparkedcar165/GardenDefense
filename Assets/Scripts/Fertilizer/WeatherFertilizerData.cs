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
        $"Change the weather to {weather}.";
}
