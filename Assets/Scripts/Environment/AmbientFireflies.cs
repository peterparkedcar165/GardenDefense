using UnityEngine;

// ambient fireflies for the night levels (tommy's pick 1B, meadow drift lit pulse, 2026-11-19).
// visual only: small glowing dots that wander over the map, fade in, brighten and dim while they
// drift, then fade out and come back somewhere else. no colliders, no light, no gameplay: they never
// block clicks and never light the ground (a Light2D or DarknessManager.RegisterLightSource would show
// light where pitch black levels keep insects hidden). the glow is drawn into the sprite and added on
// top of the scene by the additive FireflyGlow material.
// the script owns every position and brightness; the ParticleSystem only draws them
[RequireComponent(typeof(ParticleSystem))]
public class AmbientFireflies : MonoBehaviour
{
    [Header("Area and amount")]
    [Tooltip("where they wander: the standard 23 by 17 map (tile (x, y) centre = (x - 13.51268, y - 8.00399))")]
    [SerializeField] private Rect area = new Rect(-14.013f, -8.504f, 23f, 17f);
    [Tooltip("fireflies per map tile: 0.14 is about 55 on the 23 by 17 map")]
    [SerializeField] private float densityPerTile = 0.14f;

    [Header("Life (seconds)")]
    [SerializeField] private float lifeMin = 4f;
    [SerializeField] private float lifeMax = 7f;
    [SerializeField] private float fadeIn = 0.7f;
    [SerializeField] private float fadeOut = 1f;

    [Header("Drift")]
    [Tooltip("units per second (0.1 to 0.22 is 6 to 14 px per second)")]
    [SerializeField] private float speedMin = 0.1f;
    [SerializeField] private float speedMax = 0.22f;
    [Tooltip("how far the heading swings while meandering, in radians")]
    [SerializeField] private float turn = 1f;
    [SerializeField] private float bobAmplitude = 0.025f;
    [SerializeField] private float bobPeriodMin = 1.8f;
    [SerializeField] private float bobPeriodMax = 2.6f;

    [Header("Pulse")]
    [Tooltip("the dimmest point of each pulse, as a share of full brightness")]
    [SerializeField] private float pulseMin = 0.25f;
    [Tooltip("seconds per rise and fall, each firefly picks its own")]
    [SerializeField] private float pulsePeriodMin = 1.6f;
    [SerializeField] private float pulsePeriodMax = 2.6f;

    [Header("Drawing")]
    [Tooltip("game speed changes and pausing don't affect them")]
    [SerializeField] private bool useUnscaledTime = true;
    [Tooltip("4 px at 64 px per unit")]
    [SerializeField] private float size = 0.0625f;
    [Tooltip("above the ground tiles, plants, insects and hit particles, below the PlantWidget bars (Plants 50)")]
    [SerializeField] private string sortingLayer = "Plants";
    [SerializeField] private int sortingOrder = 40;

    private const float SnapPixelsPerUnit = 64f;
    private const float Life = 100000f;

    private struct Fly
    {
        public Vector2 pos;
        public float life, age, speed;
        public float h0, w1, w2, p1, p2, t;
        public float bobPeriod, bobPhase;
        public float pulsePeriod, pulsePhase;
    }

    private ParticleSystem _ps;
    private ParticleSystem.Particle[] _particles;
    private Fly[] _flies;
    private float _sunFade = 1f;

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        // the system only draws: no emission, no movement of its own, particles that never expire
        var main = _ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startLifetime = Life;
        main.gravityModifier = 0f;
        main.loop = true;
        main.playOnAwake = true;
        var emission = _ps.emission; emission.enabled = false;
        var shape = _ps.shape; shape.enabled = false;
        var colour = _ps.colorOverLifetime; colour.enabled = false;
        var sizeOver = _ps.sizeOverLifetime; sizeOver.enabled = false;
        var velocity = _ps.velocityOverLifetime; velocity.enabled = false;
        var limit = _ps.limitVelocityOverLifetime; limit.enabled = false;
        var noise = _ps.noise; noise.enabled = false;
        var rotation = _ps.rotationOverLifetime; rotation.enabled = false;
        var texture = _ps.textureSheetAnimation; texture.enabled = false;
        var trails = _ps.trails; trails.enabled = false;
        var collision = _ps.collision; collision.enabled = false;
        var sub = _ps.subEmitters; sub.enabled = false;
        var lights = _ps.lights; lights.enabled = false;

        var psRenderer = GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.sortingLayerName = sortingLayer;
        psRenderer.sortingOrder = sortingOrder;
    }

    void Start()
    {
        // day levels never show fireflies
        if (DarknessManager.instance == null || !DarknessManager.instance.IsDarkLevel)
        {
            gameObject.SetActive(false);
            return;
        }

        int count = Mathf.Max(0, Mathf.RoundToInt(densityPerTile * area.width * area.height));
        var main = _ps.main;
        main.maxParticles = count;
        _flies = new Fly[count];
        _particles = new ParticleSystem.Particle[count];
        for (int i = 0; i < count; i++)
        {
            Spawn(ref _flies[i]);
            // start the field mid cycle instead of everything fading in at once
            _flies[i].age = Random.Range(0f, _flies[i].life);
        }
        _ps.Clear();
        _ps.Emit(count);
    }

    private void Spawn(ref Fly f)
    {
        f.pos = new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));
        f.life = Random.Range(lifeMin, lifeMax);
        f.age = 0f;
        f.speed = Random.Range(speedMin, speedMax);
        f.h0 = Random.Range(0f, Mathf.PI * 2f);
        f.w1 = Random.Range(0.35f, 0.7f);
        f.w2 = Random.Range(0.9f, 1.6f);
        f.p1 = Random.Range(0f, Mathf.PI * 2f);
        f.p2 = Random.Range(0f, Mathf.PI * 2f);
        f.t = Random.Range(0f, 50f);
        f.bobPeriod = Random.Range(bobPeriodMin, bobPeriodMax);
        f.bobPhase = Random.Range(0f, Mathf.PI * 2f);
        f.pulsePeriod = Random.Range(pulsePeriodMin, pulsePeriodMax);
        f.pulsePhase = Random.Range(0f, Mathf.PI * 2f);
    }

    void LateUpdate()
    {
        if (_flies == null || _flies.Length == 0) return;
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // sunny weather turns a night level into day: fade out with the daylight, come back after it,
        // with the same ease DarknessManager uses
        float target = DarknessManager.instance != null && DarknessManager.instance.SunlightActive ? 0f : 1f;
        float lerpSpeed = WeatherManager.instance != null ? WeatherManager.instance.LightLerpSpeed : 1.5f;
        _sunFade = Mathf.Lerp(_sunFade, target, 1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime));

        int alive = _ps.GetParticles(_particles);
        if (alive < _flies.Length)
        {
            _ps.Emit(_flies.Length - alive);
            alive = _ps.GetParticles(_particles);
        }
        Vector2 centre = area.center;
        for (int i = 0; i < _flies.Length && i < alive; i++)
        {
            ref Fly f = ref _flies[i];
            f.age += dt;
            if (f.age >= f.life) Spawn(ref f);

            // pulse: the phase is integrated, so changing the period in the Inspector never jumps
            f.pulsePhase += 2f * Mathf.PI * dt / f.pulsePeriod;
            float pulse = pulseMin + (1f - pulseMin) * (0.5f + 0.5f * Mathf.Sin(f.pulsePhase));
            float env = Mathf.Min(1f, f.age / fadeIn) * Mathf.Min(1f, (f.life - f.age) / fadeOut);
            float b = env * pulse * _sunFade;

            // meander, turning back toward the middle near the area's edges
            f.t += dt;
            if (f.pos.x < area.xMin + 0.3f || f.pos.x > area.xMax - 0.3f || f.pos.y < area.yMin + 0.3f || f.pos.y > area.yMax - 0.3f)
            {
                Vector2 to = centre - f.pos;
                float want = Mathf.Atan2(to.y, to.x);
                float diff = Mathf.DeltaAngle(f.h0 * Mathf.Rad2Deg, want * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                f.h0 += Mathf.Clamp(diff, -2.5f * dt, 2.5f * dt);
            }
            float h = f.h0 + turn * (0.65f * Mathf.Sin(f.w1 * f.t + f.p1) + 0.35f * Mathf.Sin(f.w2 * f.t + f.p2));
            f.pos += f.speed * new Vector2(Mathf.Cos(h), Mathf.Sin(h)) * dt;

            float yOffset = bobAmplitude * Mathf.Sin(2f * Mathf.PI * f.age / f.bobPeriod + f.bobPhase);
            float x = Mathf.Round(f.pos.x * SnapPixelsPerUnit) / SnapPixelsPerUnit;
            float y = Mathf.Round((f.pos.y + yOffset) * SnapPixelsPerUnit) / SnapPixelsPerUnit;

            _particles[i].position = new Vector3(x, y, 0f);
            _particles[i].startColor = new Color(1f, 1f, 1f, b < 0.02f ? 0f : b);
            _particles[i].startSize = size;
            _particles[i].remainingLifetime = Life;
        }
        _ps.SetParticles(_particles, alive);
    }
}
