using UnityEngine;
using System.Collections.Generic;

public enum WeatherType { Clear, Cloudy, Sunny, Rain, Windy, Snow, Sandstorm }
public enum TemperatureType { Hot, Warm, Normal, Chill, Cold }

[System.Serializable]
public struct WeatherEntry
{
    public WeatherType type;
    [Min(1)] public int intensity;
}

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager instance;

    // fires with (type, intensity) when a weather condition is added or its intensity changes
    public static event System.Action<WeatherType, int> OnWeatherAdded;
    // fires with (type) when a weather condition is removed
    public static event System.Action<WeatherType>      OnWeatherRemoved;

    private WeatherEntry[] initialWeather;
    private TemperatureType _temperature;

    private readonly Dictionary<WeatherType, int> activeWeather = new();

    public TemperatureType temperature
    {
        get => _temperature;
        set => _temperature = value;
    }

    // backward-compat shim used by old Level1-Level14 scripts
    // clears all active weather, then sets a single type at intensity 1
    // (setting Clear just wipes everything)
    public WeatherType weather
    {
        set
        {
            ClearAllWeather();
            if (value != WeatherType.Clear)
                SetWeather(value, 1);
        }
    }

    void Awake()
    {
        instance = this;
        foreach (WeatherEntry entry in initialWeather)
            activeWeather[entry.type] = Mathf.Max(1, entry.intensity);
    }

    // add or update a weather type; fires OnWeatherAdded
    public void SetWeather(WeatherType type, int intensity)
    {
        intensity = Mathf.Max(1, intensity);
        activeWeather[type] = intensity;
        OnWeatherAdded?.Invoke(type, intensity);
    }

    // remove a weather type; fires OnWeatherRemoved
    public void RemoveWeather(WeatherType type)
    {
        if (!activeWeather.ContainsKey(type)) return;
        activeWeather.Remove(type);
        OnWeatherRemoved?.Invoke(type);
    }

    // wipe all active weather conditions
    public void ClearAllWeather()
    {
        var types = new WeatherType[activeWeather.Count];
        activeWeather.Keys.CopyTo(types, 0);
        foreach (WeatherType type in types)
            RemoveWeather(type);
    }

    public bool HasWeather(WeatherType type) => activeWeather.ContainsKey(type);
    public int GetIntensity(WeatherType type) => activeWeather.TryGetValue(type, out int i) ? i : 0;

    // iterate all currently active weather conditions
    public IEnumerable<WeatherEntry> GetActiveWeather()
    {
        foreach (var kvp in activeWeather)
            yield return new WeatherEntry { type = kvp.Key, intensity = kvp.Value };
    }
}
