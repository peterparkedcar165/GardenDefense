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

    // display name for a weather condition
    public static string GetWeatherName(WeatherType type)
    {
        switch (type)
        {
            case WeatherType.Clear:     return "<color=white>Clear</color>";
            case WeatherType.Cloudy:    return "<color=#B0B0B0>Cloudy</color>";
            case WeatherType.Sunny:     return "<color=orange>Sunny</color>";
            case WeatherType.Rain:      return "<color=#4FC3F7>Rain</color>";
            case WeatherType.Windy:     return "<color=#B2EBF2>Windy</color>";
            case WeatherType.Snow:      return "<color=#00FFFF>Snow</color>";
            case WeatherType.Sandstorm: return "<color=#E8D9A8>Sandstorm</color>";
            default:                    return type.ToString();
        }
    }

    // player facing description; intensity scales the elemental bonus where one applies
    public static string GetWeatherDescription(WeatherType type, int intensity)
    {
        int bonus = Mathf.RoundToInt((0.24f + 0.12f * (intensity - 1)) * 100f);
        switch (type)
        {
            case WeatherType.Clear:     return "No effect.";
            case WeatherType.Cloudy:    return "No effect.";
            case WeatherType.Sunny:     return $"Sunlight empowers the <color=orange>Fire</color> damage of all plants by <color=green><b>{bonus}%</b></color>, and raises the Passive level of <color=orange>Fire</color> plants by <color=green><b>1</b></color>.";
            case WeatherType.Rain:      return $"Rainfall empowers the <color=#4FC3F7>Water</color> damage of all plants by <color=green><b>{bonus}%</b></color>, and raises the Passive level of <color=#4FC3F7>Water</color> plants by <color=green><b>1</b></color>.";
            case WeatherType.Windy:     return "Strong winds sweep across the battlefield.";
            case WeatherType.Snow:      return $"Snowfall empowers the <color=#00FFFF>Ice</color> damage of all plants by <color=green><b>{bonus}%</b></color>.";
            case WeatherType.Sandstorm: return "A harsh sandstorm blankets the area.";
            default:                    return "";
        }
    }

    public static string GetTemperatureName(TemperatureType type)
    {
        switch (type)
        {
            case TemperatureType.Hot:    return "<color=orange>Hot</color>";
            case TemperatureType.Warm:   return "<color=yellow>Warm</color>";
            case TemperatureType.Normal: return "Normal";
            case TemperatureType.Chill:  return "<color=#40E0D0>Chill</color>";
            case TemperatureType.Cold:   return "<color=#00FFFF>Cold</color>";
            default:                     return type.ToString();
        }
    }

    public static string GetTemperatureDescription(TemperatureType type)
    {
        switch (type)
        {
            case TemperatureType.Hot:    return "The heat steadily raises plant temperature. Plants that exceed their comfort range take damage.";
            case TemperatureType.Warm:   return "Pleasantly warm. No significant effect on plants.";
            case TemperatureType.Normal: return "Conditions normal";
            case TemperatureType.Chill:  return "Pleasantly cool. No significant effect on plants.";
            case TemperatureType.Cold:   return "The cold steadily lowers plant temperature. Plants that fall below their comfort range take damage.";
            default:                     return "";
        }
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
