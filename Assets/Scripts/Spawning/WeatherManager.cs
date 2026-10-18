using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public enum WeatherType { Clear, Sunny, Rain, Windy, Snow, Sandstorm, Underwater }
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

    [Header("Visuals")]
    [Tooltip("assign the scene's active biome light (DayLight/NightLight/DesertLight/UnderwaterDaylight)")]
    [SerializeField] private Light2D sceneLight;
    [Tooltip("the single place every weather's look (light tint + particle effect + ambience) is authored")]
    [SerializeField] private WeatherVisualLibrary visualLibrary;
    [Tooltip("how quickly the scene light eases toward a weather change - higher is snappier")]
    [SerializeField] private float colorLerpSpeed = 1.5f;
    [Tooltip("seconds for a weather's ambience to fade fully in or out")]
    [SerializeField] private float ambienceFadeDuration = 3f;

    private Color _baseLightColor = Color.white;
    private float _baseLightIntensity = 1f;
    private Color _targetLightColor = Color.white;
    private float _targetLightIntensity = 1f;
    private readonly Dictionary<WeatherType, GameObject> _particleInstances = new();

    private class AmbienceVoice
    {
        public AudioSource source;
        public float baseVolume;
        public float targetVolume;
    }
    private readonly Dictionary<WeatherType, List<AmbienceVoice>> _ambienceVoices = new();

    private TemperatureType _temperature;

    private readonly Dictionary<WeatherType, int> activeWeather = new();
    // the natural weather a level reverts to once every active weather effect has expired
    private readonly Dictionary<WeatherType, int> baseWeather = new();

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

    void Start()
    {
        // the hand-assigned Inspector reference is kept as an optional override, but every
        // level's biome light (DayLight/NightLight/DesertLight/UnderwaterDaylight) is the
        // scene's one Light2D of type Global, so find it automatically rather than requiring
        // it to be dragged in per scene
        if (sceneLight == null)
        {
            foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsInactive.Exclude))
            {
                if (light.lightType != Light2D.LightType.Global) continue;
                sceneLight = light;
                break;
            }
        }

        if (sceneLight != null)
        {
            _baseLightColor = sceneLight.color;
            _baseLightIntensity = sceneLight.intensity;
        }
        RefreshVisuals();

        // snap to the starting weather's look instead of easing in from the base light
        if (sceneLight != null)
        {
            sceneLight.color = _targetLightColor;
            sceneLight.intensity = _targetLightIntensity;
        }
    }

    void Update()
    {
        // unscaled so weather transitions keep easing even while Time.timeScale is 0
        // (pre-wave pause, Settings, Fertilizer/Mulch popups)
        float dt = Time.unscaledDeltaTime;

        if (sceneLight != null)
        {
            float t = 1f - Mathf.Exp(-colorLerpSpeed * dt);
            sceneLight.color = Color.Lerp(sceneLight.color, _targetLightColor, t);
            sceneLight.intensity = Mathf.Lerp(sceneLight.intensity, _targetLightIntensity, t);
        }
        UpdateAmbienceFades(dt);
    }

    private void UpdateAmbienceFades(float dt)
    {
        if (_ambienceVoices.Count == 0) return;

        List<WeatherType> finished = null;
        foreach (var kvp in _ambienceVoices)
        {
            bool anyAlive = false;
            foreach (AmbienceVoice voice in kvp.Value)
            {
                if (voice.source == null) continue;
                float step = (voice.baseVolume / Mathf.Max(0.01f, ambienceFadeDuration)) * dt;
                voice.source.volume = Mathf.MoveTowards(voice.source.volume, voice.targetVolume, step);

                if (voice.targetVolume <= 0f && voice.source.volume <= 0.001f)
                {
                    Destroy(voice.source);
                    voice.source = null;
                }
                else
                {
                    anyAlive = true;
                }
            }
            if (!anyAlive) (finished ??= new List<WeatherType>()).Add(kvp.Key);
        }

        if (finished != null)
            foreach (WeatherType type in finished)
                _ambienceVoices.Remove(type);
    }

    // add or update a weather type; fires OnWeatherAdded
    public void SetWeather(WeatherType type, int intensity)
    {
        intensity = Mathf.Max(1, intensity);
        activeWeather[type] = intensity;
        Debug.Log($"[Weather] {type} set to intensity {intensity}");
        OnWeatherAdded?.Invoke(type, intensity);
        RefreshVisuals();
    }

    // remove a weather type; fires OnWeatherRemoved, then restores the level's base weather
    // if that was the last active condition
    public void RemoveWeather(WeatherType type)
    {
        if (!activeWeather.ContainsKey(type)) return;
        activeWeather.Remove(type);
        Debug.Log($"[Weather] {type} removed");
        OnWeatherRemoved?.Invoke(type);
        RefreshVisuals();
        RestoreBaseWeatherIfEmpty();
    }

    // wipe all active weather conditions without restoring the base weather
    // (used when a caller is about to set a new weather immediately after, e.g. Zinnia's skill)
    public void ClearAllWeather()
    {
        var types = new WeatherType[activeWeather.Count];
        activeWeather.Keys.CopyTo(types, 0);
        foreach (WeatherType type in types)
        {
            activeWeather.Remove(type);
            Debug.Log($"[Weather] {type} removed");
            OnWeatherRemoved?.Invoke(type);
        }
        RefreshVisuals();
    }

    // recomputes the target scene light color/intensity (eased toward in Update, not snapped),
    // enables/disables each weather's particle prefab (instantiated lazily, the first time it's
    // actually needed, then just toggled from then on), and starts/stops each weather's ambience
    // crossfade - see WeatherVisualLibrary
    private void RefreshVisuals()
    {
        Color color = _baseLightColor;
        float intensityMult = 1f;

        if (visualLibrary != null)
            foreach (var kvp in activeWeather)
            {
                WeatherVisualEntry entry = visualLibrary.Find(kvp.Key);
                if (entry == null) continue;
                color = Color.Lerp(color, entry.tintColor, entry.tintStrength);
                intensityMult *= entry.lightIntensityMultiplier;
            }

        _targetLightColor = color;
        _targetLightIntensity = _baseLightIntensity * intensityMult;

        if (visualLibrary?.entries == null) return;
        foreach (WeatherVisualEntry entry in visualLibrary.entries)
        {
            bool shouldBeActive = activeWeather.ContainsKey(entry.weather);

            if (entry.particlePrefab != null)
            {
                if (!_particleInstances.TryGetValue(entry.weather, out GameObject instance))
                {
                    if (shouldBeActive)
                    {
                        instance = Instantiate(entry.particlePrefab, transform);
                        if (entry.nightMaterial != null && IsNightScene())
                        {
                            Renderer renderer = instance.GetComponent<Renderer>();
                            if (renderer != null) renderer.material = entry.nightMaterial;
                        }
                        _particleInstances[entry.weather] = instance;
                    }
                }
                else
                {
                    instance.SetActive(shouldBeActive);
                }
            }

            UpdateAmbienceForEntry(entry, shouldBeActive);
        }
    }

    // fades a weather's ambience sources in when it becomes active and out when it stops,
    // re-targeting a still-fading-out set back up if the weather returns mid-fade
    private void UpdateAmbienceForEntry(WeatherVisualEntry entry, bool shouldBeActive)
    {
        if (entry.ambience == null || entry.ambience.sounds == null) return;

        bool alreadyPlaying = _ambienceVoices.TryGetValue(entry.weather, out List<AmbienceVoice> voices);

        if (shouldBeActive && !alreadyPlaying)
        {
            voices = new List<AmbienceVoice>();
            foreach (AmbienceEntry sound in entry.ambience.sounds)
            {
                if (sound.clip == null) continue;
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.clip = sound.clip;
                src.outputAudioMixerGroup = entry.ambience.output;
                src.loop = true;
                src.playOnAwake = false;
                src.volume = 0f;
                src.Play();
                voices.Add(new AmbienceVoice { source = src, baseVolume = sound.volume, targetVolume = sound.volume });
            }
            _ambienceVoices[entry.weather] = voices;
        }
        else if (alreadyPlaying)
        {
            float target = shouldBeActive ? 1f : 0f;
            foreach (AmbienceVoice voice in voices)
                voice.targetVolume = voice.baseVolume * target;
        }
    }

    // the light prefabs (DayLight/NightLight/DesertLight/UnderwaterDaylight) keep their prefab's
    // own name on the instance, so this needs no per-scene wiring of its own
    private bool IsNightScene() => sceneLight != null && sceneLight.gameObject.name.Contains("Night");

    // defines the natural weather this level reverts to once every active weather effect
    // has expired, and immediately applies it
    public void SetBaseWeather(IEnumerable<WeatherEntry> entries)
    {
        baseWeather.Clear();
        if (entries != null)
            foreach (WeatherEntry entry in entries)
                baseWeather[entry.type] = Mathf.Max(1, entry.intensity);

        foreach (var kvp in baseWeather)
            SetWeather(kvp.Key, kvp.Value);
    }

    private void RestoreBaseWeatherIfEmpty()
    {
        if (activeWeather.Count > 0 || baseWeather.Count == 0) return;
        foreach (var kvp in baseWeather)
            SetWeather(kvp.Key, kvp.Value);
    }

    // display name for a weather condition
    public static string GetWeatherName(WeatherType type)
    {
        switch (type)
        {
            case WeatherType.Clear:     return "<color=white>Clear</color>";
            case WeatherType.Sunny:     return "<color=orange>Sunny</color>";
            case WeatherType.Rain:      return "<color=#4FC3F7>Rain</color>";
            case WeatherType.Windy:     return "<color=#B2EBF2>Windy</color>";
            case WeatherType.Snow:      return "<color=#00FFFF>Snow</color>";
            case WeatherType.Sandstorm: return "<color=#E8D9A8>Sandstorm</color>";
            case WeatherType.Underwater: return "<color=#1B6CA8>Underwater</color>";
            default:                    return type.ToString();
        }
    }

    // player facing description; intensity scales the elemental bonus where one applies
    public static string GetWeatherDescription(WeatherType type, int intensity)
    {
        int bonus = 12 + 8 * (intensity - 1);
        switch (type)
        {
            case WeatherType.Clear:     return "No effect.";
            case WeatherType.Sunny:     return $"Sunlight empowers the <color=orange>Fire</color> damage of all plants by <color=green><b>{bonus}%</b></color> while weakening their <color=#4FC3F7>Water</color> damage by <color=red><b>{bonus}%</b></color>, and raises the Passive level of <color=orange>Fire</color> plants by <color=green><b>1</b></color>.";
            case WeatherType.Rain:      return $"Rainfall empowers the <color=#4FC3F7>Water</color> damage of all plants by <color=green><b>{bonus}%</b></color> while weakening their <color=orange>Fire</color> damage by <color=red><b>{bonus}%</b></color>, and raises the Passive level of <color=#4FC3F7>Water</color> plants by <color=green><b>1</b></color>.";
            case WeatherType.Windy:     return "Strong winds sweep across the battlefield.";
            case WeatherType.Snow:      return $"Snowfall empowers the <color=#00FFFF>Ice</color> damage of all plants by <color=green><b>{bonus}%</b></color>.";
            case WeatherType.Sandstorm: return $"A harsh sandstorm reduces the <color=green><b>Accuracy</b></color> of all plants by <color=red><b>{10 * intensity}%</b></color> and deals <color=red><b>{2 * intensity}</b></color> damage per second.";
            case WeatherType.Underwater: return $"Plants are <color=#1B6CA8><b>Submerged</b></color>: their <color=green><b>Air</b></color> depletes over time unless mitigated by <color=green><b>Respiration</b></color>. Non-<color=#4FC3F7>Water</color> plants have their Projectile Speed halved, <color=orange>Fire</color> damage is reduced by <color=red><b>50%</b></color>, and <color=#4FC3F7>Water</color> damage is increased by <color=green><b>20%</b></color>.";
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
