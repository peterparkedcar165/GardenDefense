using UnityEngine;

public enum WeatherType {Sunny, Rain, Windy, Snow, Sandstorm}
public enum TemperatureType {Hot, Warm, Normal, Chill, Cold}
public class WeatherManager : MonoBehaviour
{
    public static WeatherManager instance;
    public WeatherType weather;
    public TemperatureType temperature;

    void Awake()
    {
        instance = this;
    }
}
