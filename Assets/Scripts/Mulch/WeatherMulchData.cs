using UnityEngine;

// one reusable data type for every "change weather to X" mulch (Sunny/Rain/Sandstorm/Snow) -
// make one asset per weather with the desired WeatherType set in the Inspector, rather than a
// separate C# class per weather
[CreateAssetMenu(fileName = "WeatherMulchData", menuName = "Scriptable Objects/Mulch/Change Weather")]
public class WeatherMulchData : MulchData
{
    public WeatherType weather = WeatherType.Sunny;
    public int intensity = 1;

    public override MulchEffect CreateEffect() => new WeatherMulchEffect(this);
}
