using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class DarknessManager : MonoBehaviour
{
    public static DarknessManager instance;
    public bool isDark = false;
    // true only for cave levels: insects standing outside any light are not rendered.
    // leave false for nighttime/desert-night, where insects stay visible in shadow
    public bool pitchBlack = false;

    [Header("Setup-phase lighting (cave levels)")]
    [Tooltip("if true, the NightLight is lit during wave 0 (setup) and fades to black just before wave 1")]
    [SerializeField] private bool litDuringSetup = false;
    [SerializeField] private Light2D nightLight;          // assign the scene's NightLight
    [Tooltip("NightLight intensity during the setup phase")]
    [SerializeField] private float setupIntensity = 0.4f;
    [Tooltip("the light reaches 0 this many seconds before wave 1 begins")]
    [SerializeField] private float darkByCountdown = 1f;
    [Tooltip("how long (seconds) the fade from the setup intensity down to 0 takes")]
    [SerializeField] private float fadeDuration = 0.4f;

    private static readonly List<(Transform t, float radius)> _dynamicSources = new List<(Transform, float)>();

    // the darkness this level would have absent any weather override - whatever the scene author
    // set isDark/pitchBlack to (or, on a litDuringSetup cave level, whatever the setup-phase fade
    // just computed). isDark/pitchBlack themselves become read-only derived values once Sunny
    // weather is active, so every existing reader of isDark/pitchBlack keeps working unchanged
    private bool _naturalDark;
    private bool _naturalPitchBlack;
    private bool _sunnyOverride;

    // sunny weather on a dark level should look exactly like sunny on a day level, not just play
    // like one. WeatherManager swaps the scene light for daylight (see IsDarkLevel), and this
    // switches off every point light (plant glows, projectile and burn lights, glowing insects)
    // once the daylight is halfway in, since a day level has none of them, then switches them
    // back on halfway through the fade back to night
    private bool _isDarkLevel;
    private float _sunBlend;
    private bool _lightsHiddenForSun;
    private readonly List<Light2D> _sunHiddenLights = new List<Light2D>();
    // a cave's light is driven here, not by WeatherManager. after the sun goes it eases back down
    // to the cave's own level instead of snapping to black
    private bool _easingBackFromSun;

    // true on a night or cave level, read by WeatherManager to swap in daylight while it's sunny
    public bool IsDarkLevel => _isDarkLevel;
    public bool SunlightActive => _sunnyOverride;
    // false on a day level (no DarknessManager) and while sunlight has the point lights switched off
    public static bool LightsVisible => instance != null && !instance._lightsHiddenForSun;

    // WeatherManager leaves this light's intensity alone while it returns true: a cave's setup
    // fade and darkness are timed here. while it's sunny the weather system takes it back
    public bool DrivesLightIntensity(Light2D light) =>
        litDuringSetup && nightLight != null && light == nightLight && !_sunnyOverride;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        _dynamicSources.Clear();
        SceneManager.sceneLoaded += (_, __) => _dynamicSources.Clear();
    }

    void Awake()
    {
        instance = this;
        _naturalDark = isDark;
        _naturalPitchBlack = pitchBlack;
        _isDarkLevel = isDark || pitchBlack || litDuringSetup;
    }

    void OnEnable()
    {
        WeatherManager.OnWeatherAdded += HandleWeatherAdded;
        WeatherManager.OnWeatherRemoved += HandleWeatherRemoved;
    }

    void OnDisable()
    {
        WeatherManager.OnWeatherAdded -= HandleWeatherAdded;
        WeatherManager.OnWeatherRemoved -= HandleWeatherRemoved;
    }

    private void HandleWeatherAdded(WeatherType type, int intensity)
    {
        if (type != WeatherType.Sunny) return;
        _sunnyOverride = true;
        ApplyOverride();
    }

    private void HandleWeatherRemoved(WeatherType type)
    {
        if (type != WeatherType.Sunny) return;
        _sunnyOverride = false;
        _easingBackFromSun = true;
        ApplyOverride();
    }

    private void ApplyOverride()
    {
        isDark = _sunnyOverride ? false : _naturalDark;
        pitchBlack = _sunnyOverride ? false : _naturalPitchBlack;
    }

    void Update()
    {
        // unscaled, at the weather system's own speed, so the point lights switch while the scene
        // light is visibly halfway through its own fade, mulch picker pause included
        float ease = 1f - Mathf.Exp(-(WeatherManager.instance != null ? WeatherManager.instance.LightLerpSpeed : 1.5f) * Time.unscaledDeltaTime);

        if (litDuringSetup)
        {
            bool inSetup = GameManager.instance == null || GameManager.instance.currentWave == 0;
            float countdown = GameHUD.instance != null ? GameHUD.instance.NextWaveCountdown : float.PositiveInfinity;

            // held at setupIntensity through setup, then fades to 0 over fadeDuration, reaching 0 when
            // darkByCountdown seconds remain before wave 1. stays 0 from then on (pitch black for the wave)
            float target;
            if (!inSetup)
                target = 0f;
            else
                target = setupIntensity * Mathf.Clamp01((countdown - darkByCountdown) / Mathf.Max(fadeDuration, 0.0001f));

            _naturalDark = target <= 0.001f;   // darkness gameplay kicks in once the NightLight is fully out

            // while it's sunny, WeatherManager eases this light up to daylight instead
            if (nightLight != null && !_sunnyOverride)
            {
                if (_easingBackFromSun)
                {
                    nightLight.intensity = Mathf.Lerp(nightLight.intensity, target, ease);
                    if (Mathf.Abs(nightLight.intensity - target) < 0.005f) _easingBackFromSun = false;
                }
                else nightLight.intensity = target;
            }
        }

        ApplyOverride();
        UpdateSunLights(ease);
    }

    private void UpdateSunLights(float ease)
    {
        bool sunnyDay = _sunnyOverride && _isDarkLevel;
        _sunBlend = Mathf.Lerp(_sunBlend, sunnyDay ? 1f : 0f, ease);

        if (_sunBlend > 0.5f)
        {
            _lightsHiddenForSun = true;
            // swept every frame, so lights made during the sun (new projectiles, burns) go too
            foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsInactive.Exclude))
            {
                if (light.lightType == Light2D.LightType.Global || !light.enabled) continue;
                light.enabled = false;
                _sunHiddenLights.Add(light);
            }
        }
        else if (_lightsHiddenForSun)
        {
            _lightsHiddenForSun = false;
            foreach (Light2D light in _sunHiddenLights)
                if (light != null) light.enabled = true;
            _sunHiddenLights.Clear();
        }
    }

    // how lit a point is by the light sources IsIlluminated counts (plants, insects, registered
    // lights): 1 within a light's bright core, easing to 0 at its edge, like a plant's point light
    // (full out to 1.2, or 40% of a small radius, then falloff). 1 everywhere when the level isn't
    // dark. for visuals only (rain brightening under a light); gameplay keeps using IsIlluminated
    public float LightAmount(Vector3 position)
    {
        if (!isDark) return 1f;

        float best = 0f;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || plant.lightEmissionRange <= 0) continue;
            best = Mathf.Max(best, LightFalloff(position, plant.transform.position, plant.lightEmissionRange));
            if (best >= 1f) return 1f;
        }
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || insect.lightEmissionRange <= 0) continue;
            best = Mathf.Max(best, LightFalloff(position, insect.transform.position, insect.lightEmissionRange));
        }
        foreach (Insect insect in Insect.friendlyInsects)
        {
            if (insect == null || !insect.IsAlive || insect.lightEmissionRange <= 0) continue;
            best = Mathf.Max(best, LightFalloff(position, insect.transform.position, insect.lightEmissionRange));
        }
        foreach (var (t, radius) in _dynamicSources)
        {
            if (t == null) continue;
            best = Mathf.Max(best, LightFalloff(position, t.position, radius));
        }
        return Mathf.Min(1f, best);
    }

    private static float LightFalloff(Vector3 point, Vector3 source, float radius)
    {
        float dx = point.x - source.x, dy = point.y - source.y;
        float sqr = dx * dx + dy * dy;
        if (sqr >= radius * radius) return 0f;
        float inner = radius <= 1.2f ? radius * 0.4f : 1.2f;
        return 1f - Mathf.SmoothStep(0f, 1f, (Mathf.Sqrt(sqr) - inner) / (radius - inner));
    }

    public static void RegisterLightSource(Transform t, float radius) => _dynamicSources.Add((t, radius));

    public static void UnregisterLightSource(Transform t) => _dynamicSources.RemoveAll(s => s.t == t);

    public bool IsIlluminated(Vector3 position)
    {
        if (!isDark) return true;

        foreach (Plant plant in Plant.allPlants)
        {
            if (plant.lightEmissionRange <= 0) continue;
            if (Vector3.Distance(position, plant.transform.position) <= plant.lightEmissionRange)
                return true;
        }

        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || insect.lightEmissionRange <= 0) continue;
            if (Vector3.Distance(position, insect.transform.position) <= insect.lightEmissionRange)
                return true;
        }

        foreach (Insect insect in Insect.friendlyInsects)
        {
            if (insect == null || !insect.IsAlive || insect.lightEmissionRange <= 0) continue;
            if (Vector3.Distance(position, insect.transform.position) <= insect.lightEmissionRange)
                return true;
        }

        for (int i = _dynamicSources.Count - 1; i >= 0; i--)
        {
            var (t, radius) = _dynamicSources[i];
            if (t == null) { _dynamicSources.RemoveAt(i); continue; }
            if (Vector3.Distance(position, t.position) <= radius)
                return true;
        }

        return false;
    }
}
