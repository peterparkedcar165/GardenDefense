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

    // overhead: a light from the sky (Sunray, Zinnia's Artificial Sun) that walls don't block
    private struct DynamicSource { public Transform t; public float radius; public bool overhead; }
    private static readonly List<DynamicSource> _dynamicSources = new List<DynamicSource>();

    [Header("Light shadows (Tommy, 2026-11-28)")]
    [Tooltip("how dark a light's shadow behind a wall or cliff is, 1 = none of its light gets past (proposal)")]
    [Range(0f, 1f)] [SerializeField] private float lightShadowStrength = 0.9f;
    [Tooltip("how soft a light's shadow edge is (proposal)")]
    [Range(0f, 1f)] [SerializeField] private float lightShadowSoftness = 0.3f;

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
        UpdateLightShadows();
    }

    // the visible glow is blocked where the gameplay light is (Tommy, 2026-11-28): every point light on low ground casts
    // shadows from the obstacle cells and cliff edges (LightBlockers). swept every ShadowSweepInterval, so a light made
    // in between (a shot, a burn) goes without for a moment
    private const float ShadowSweepInterval = 0.1f;
    private float _shadowSweepTimer;

    private void UpdateLightShadows()
    {
        if (!_isDarkLevel) return;
        _shadowSweepTimer -= Time.unscaledDeltaTime;
        if (_shadowSweepTimer > 0f) return;
        _shadowSweepTimer = ShadowSweepInterval;
        foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsInactive.Exclude))
        {
            if (light.lightType != Light2D.LightType.Point) continue;
            bool cast = CastsShadows(light);
            if (light.shadowsEnabled != cast) light.shadowsEnabled = cast;
            if (!cast) continue;
            light.shadowIntensity = lightShadowStrength;
            light.shadowSoftness = lightShadowSoftness;
        }
    }

    // a light on low ground casts. one on high ground doesn't (it sees down past cliffs; raised obstacles near it
    // still block its gameplay light), nor one on a flying insect, one from the sky (Sunray, the Artificial Sun), or one
    // inside an obstacle's cell (a lamp tile), whose own caster would black out everything around it
    private bool CastsShadows(Light2D light)
    {
        Transform t = light.transform;
        for (int i = 0; i < _dynamicSources.Count; i++)
        {
            DynamicSource source = _dynamicSources[i];
            if (source.overhead && source.t != null && (t == source.t || t.IsChildOf(source.t))) return false;
        }
        Plant plant = light.GetComponentInParent<Plant>();
        if (plant != null) return !plant.OnHighground;
        Insect insect = light.GetComponentInParent<Insect>();
        if (insect != null && insect.isFlying) return false;
        Tile ground = Tile.At(t.position);
        return ground == null || (!ground.isHighground && ground.tileType != TileType.Obstacle);
    }

    private const float SunSweepInterval = 0.1f;
    private float _sunSweepTimer;

    private void UpdateSunLights(float ease)
    {
        bool sunnyDay = _sunnyOverride && _isDarkLevel;
        _sunBlend = Mathf.Lerp(_sunBlend, sunnyDay ? 1f : 0f, ease);

        if (_sunBlend > 0.5f)
        {
            // swept at once when the sun takes over, then every SunSweepInterval, so lights made during
            // the sun (new projectiles, burns) go too. it was every frame, and the sweep searches every
            // object in the scene and makes a new array each time
            _sunSweepTimer -= Time.unscaledDeltaTime;
            if (!_lightsHiddenForSun || _sunSweepTimer <= 0f)
            {
                _sunSweepTimer = SunSweepInterval;
                foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsInactive.Exclude))
                {
                    if (light.lightType == Light2D.LightType.Global || !light.enabled) continue;
                    light.enabled = false;
                    _sunHiddenLights.Add(light);
                }
            }
            _lightsHiddenForSun = true;
        }
        else if (_lightsHiddenForSun)
        {
            _lightsHiddenForSun = false;
            _sunSweepTimer = 0f;
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

        GatherLights();
        float best = 0f;
        for (int i = 0; i < _lights.Count; i++)
        {
            LitSource light = _lights[i];
            best = Mathf.Max(best, LightFalloff(position, light.position, light.radius));
            if (best >= 1f) return 1f;
        }
        return best;
    }

    // every light source IsIlluminated and LightAmount count (plants, insects, friendlies and the
    // registered lights), gathered once per frame. both are asked about very many points a frame
    // (every candidate insect of every plant's target search on a dark level, every rain and snow
    // particle), and each call read every plant's, insect's and light's position again. a light that
    // appears or moves later in the frame counts from the next frame
    private struct LitSource
    {
        public Vector3 position;
        public float radius;
        public float sqrRadius;
        // who checks whether it reaches a point (IsIlluminated): a plant from its own rays, anything else by a line
        // from its ground (a flying insect's from above), an overhead light not at all
        public Plant plant;
        public bool fromHigh;
        public bool overhead;
    }

    private readonly List<LitSource> _lights = new List<LitSource>();
    private int _lightsFrame = -1;

    private void GatherLights()
    {
        if (_lightsFrame == Time.frameCount) return;
        _lightsFrame = Time.frameCount;
        _lights.Clear();
        _litCache.Clear();
        foreach (Plant plant in Plant.allPlants)
            if (plant != null && plant.lightEmissionRange > 0) AddLight(plant.transform.position, plant.lightEmissionRange, plant);
        foreach (Insect insect in Insect.allInsects)
            if (insect != null && insect.IsAlive && insect.lightEmissionRange > 0) AddLight(insect.transform.position, insect.lightEmissionRange, null, insect.isFlying);
        foreach (Insect insect in Insect.friendlyInsects)
            if (insect != null && insect.IsAlive && insect.lightEmissionRange > 0) AddLight(insect.transform.position, insect.lightEmissionRange, null, insect.isFlying);
        for (int i = _dynamicSources.Count - 1; i >= 0; i--)
        {
            DynamicSource source = _dynamicSources[i];
            if (source.t == null) { _dynamicSources.RemoveAt(i); continue; }
            Tile ground = Tile.At(source.t.position);
            AddLight(source.t.position, source.radius, null, ground != null && ground.isHighground, source.overhead);
        }
    }

    private void AddLight(Vector3 position, float radius, Plant plant = null, bool fromHigh = false, bool overhead = false) =>
        _lights.Add(new LitSource { position = position, radius = radius, sqrRadius = radius * radius, plant = plant, fromHigh = fromHigh, overhead = overhead });

    // Collider cells and cliffs block light as they block sight (Tommy, 2026-11-28: every light source; the glow
    // itself still draws round, gameplay first)
    private static bool Reaches(LitSource light, Vector3 point)
    {
        if (light.overhead) return true;
        if (light.plant != null) return light.plant.LightReaches(point);
        return LineOfSight.LightReaches(light.position, point, light.fromHigh);
    }

    // the answers this frame by point: the same insect is asked about by every plant's target search
    private readonly Dictionary<Vector3, bool> _litCache = new Dictionary<Vector3, bool>();

    private static float LightFalloff(Vector3 point, Vector3 source, float radius)
    {
        float dx = point.x - source.x, dy = point.y - source.y;
        float sqr = dx * dx + dy * dy;
        if (sqr >= radius * radius) return 0f;
        float inner = radius <= 1.2f ? radius * 0.4f : 1.2f;
        return 1f - Mathf.SmoothStep(0f, 1f, (Mathf.Sqrt(sqr) - inner) / (radius - inner));
    }

    public static void RegisterLightSource(Transform t, float radius, bool overhead = false) =>
        _dynamicSources.Add(new DynamicSource { t = t, radius = radius, overhead = overhead });

    public static void UnregisterLightSource(Transform t) => _dynamicSources.RemoveAll(s => s.t == t);

    public bool IsIlluminated(Vector3 position)
    {
        if (!isDark) return true;

        GatherLights();
        if (_litCache.TryGetValue(position, out bool known)) return known;
        bool lit = false;
        for (int i = 0; i < _lights.Count && !lit; i++)
        {
            LitSource light = _lights[i];
            if ((position - light.position).sqrMagnitude <= light.sqrRadius && Reaches(light, position)) lit = true;
        }
        _litCache[position] = lit;
        return lit;
    }
}
