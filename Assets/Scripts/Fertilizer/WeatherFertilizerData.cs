using UnityEngine;

// one data type for every "set the level's weather" fertilizer (sunny, rainy and snowy day): one
// asset per weather
[CreateAssetMenu(fileName = "WeatherFertilizer", menuName = "Scriptable Objects/Fertilizer/Set Weather")]
public class WeatherFertilizerData : FertilizerData
{
    // its paper bag icon's symbol, the mulches' categories (proposal, Tommy, 2026-11-24: the weather boosts an element's damage)
    protected override MulchSymbol DefaultSymbol => MulchSymbol.Attack;

    public WeatherType weather = WeatherType.Sunny;
    public int intensity = 1;

    public override FertilizerEffect CreateEffect() => new WeatherFertilizerEffect(this);

    public override string GetDescription() =>
        $"Change the weather to {weather}.";
}
