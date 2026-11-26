using UnityEngine;

// cloud shadows over levels with sun (Tommy, 2026-11-26), made with the same particle system as the rain,
// snow and sandstorm (Tommy: same system, same logic, other numbers): the sandstorm's prefab, its numbers
// changed here. its particles are his black cloud shapes (one random cell of CloudShadowStyle.sheet each),
// drifting up and to the right at the style's angle over the whole camera view, on real time like the
// weather (on while paused, no faster at a higher game speed), drawn above everything in the world and
// under the indicators and the ui. a shadow is born anywhere upwind of the view and lives long enough to
// cross it, fading in and out at the two ends of its life; the system is prewarmed, so the view starts
// full. they show on day, desert and snow levels, and on night and desert night levels only while Sunny
// weather has the sun out; never in the cave or underwater (the interiors). they fade out while it rains,
// snows or a sandstorm blows. built by ProceduralLevel when the level starts
public class CloudShadows : MonoBehaviour
{
    private CloudShadowStyle style;
    private Biome biome;
    private ParticleSystem particles;
    private ParticleSystemRenderer particleRenderer;
    private Material material;
    private float shown;

    public static CloudShadows Create(LevelConfig config)
    {
        if (config == null) return null;
        Biome biome = config.CurrentBiome;
        // the interiors never get them
        if (biome == Biome.Cave || biome == Biome.Underwater) return null;
        CloudShadowStyle style = CloudShadowStyle.Instance;
        if (style.particlePrefab == null || style.sheet == null)
        {
            Debug.LogWarning("[CloudShadows] Resources/CloudShadowStyle.asset has no particle prefab or sheet (or didn't load). Right click it and Reimport.");
            return null;
        }
        GameObject go = new GameObject("CloudShadows");
        CloudShadows clouds = go.AddComponent<CloudShadows>();
        clouds.style = style;
        clouds.biome = biome;
        if (!clouds.Build())
        {
            Destroy(go);
            return null;
        }
        return clouds;
    }

    private bool Build()
    {
        GameObject made = Instantiate(style.particlePrefab, transform);
        made.name = "CloudShadowParticles";
        particles = made.GetComponentInChildren<ParticleSystem>();
        if (particles == null) return false;
        // only the one system: any others in the prefab go
        foreach (ParticleSystem other in made.GetComponentsInChildren<ParticleSystem>(true))
            if (other != particles) other.gameObject.SetActive(false);
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.transform.rotation = Quaternion.identity;
        particles.transform.localScale = Vector3.one;
        particleRenderer = particles.GetComponent<ParticleSystemRenderer>();

        // the weather's own material, given the cloud sheet, black
        material = new Material(particleRenderer.sharedMaterial) { name = "CloudShadows" };
        material.mainTexture = style.sheet;
        particleRenderer.sharedMaterial = material;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortingLayerName = style.sortingLayer;
        particleRenderer.sortingOrder = style.sortingOrder;
        // big shadows are allowed (the sandstorm's grains were held to half the screen)
        particleRenderer.maxParticleSize = 5f;
        // half of them mirrored, for more shapes
        particleRenderer.flip = new Vector3(0.5f, 0f, 0f);

        var main = particles.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = false;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.startSpeed = 0f;
        main.startRotation = 0f;
        main.gravityModifier = 0f;
        main.startColor = Color.black;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.rotation = Vector3.zero;
        shape.randomDirectionAmount = 0f;

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;

        // fading in at birth and out at the end, so one never pops in or out
        var colour = particles.colorOverLifetime;
        colour.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.06f), new GradientAlphaKey(1f, 0.94f), new GradientAlphaKey(0f, 1f) });
        colour.color = new ParticleSystem.MinMaxGradient(fade);

        // one random cell of the sheet each, kept for its whole life
        var cells = particles.textureSheetAnimation;
        cells.enabled = true;
        cells.mode = ParticleSystemAnimationMode.Grid;
        cells.numTilesX = style.sheetColumns;
        cells.numTilesY = style.sheetRows;
        cells.animation = ParticleSystemAnimationType.WholeSheet;
        cells.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.9999f);
        cells.startFrame = 0f;
        cells.cycleCount = 1;

        // the sandstorm's other movers off
        var noise = particles.noise; noise.enabled = false;
        var force = particles.forceOverLifetime; force.enabled = false;
        var size = particles.sizeOverLifetime; size.enabled = false;
        var spin = particles.rotationOverLifetime; spin.enabled = false;
        var limit = particles.limitVelocityOverLifetime; limit.enabled = false;
        var inherit = particles.inheritVelocity; inherit.enabled = false;
        var collision = particles.collision; collision.enabled = false;
        var trails = particles.trails; trails.enabled = false;
        var lights = particles.lights; lights.enabled = false;
        var subs = particles.subEmitters; subs.enabled = false;

        Configure();
        shown = Wanted() ? 1f : 0f;
        ApplyAlpha();
        particles.Play();
        return true;
    }

    // the numbers that follow the style live: size, speed, direction, how many, and the area they're born in
    private float appliedScale = -1f, appliedSpeed = -1f, appliedAngle = -1f, appliedCount = -1f;
    private Vector3 appliedCamera;
    private float appliedView;

    private void Configure()
    {
        Camera cam = Camera.main;
        float h = cam != null && cam.orthographic ? cam.orthographicSize : 8.5f;
        float w = h * (cam != null ? cam.aspect : 16f / 9f);
        Vector3 centre = cam != null ? cam.transform.position : Vector3.zero;
        if (style.scale == appliedScale && style.speed == appliedSpeed && style.angle == appliedAngle && style.count == appliedCount
            && centre == appliedCamera && h == appliedView) return;
        appliedScale = style.scale;
        appliedSpeed = style.speed;
        appliedAngle = style.angle;
        appliedCount = style.count;
        appliedCamera = centre;
        appliedView = h;

        float a = style.angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        // the view and a shadow's size round it; a shadow travels its full width and height in its life, so
        // one born at the far upwind corner still crosses the whole view before it fades
        float margin = style.scale;
        float viewW = 2f * w + 2f * margin, viewH = 2f * h + 2f * margin;
        float travel = viewW + viewH;
        float life = travel / Mathf.Max(0.01f, style.speed);
        // born anywhere in the view swept back upwind by the travel
        float boxW = viewW + travel * dir.x, boxH = viewH + travel * dir.y;
        Vector2 boxCentre = new Vector2(centre.x, centre.y) - dir * (travel * 0.5f);
        // so about count are over the view at once: each place in the view is crossed by every shadow born
        // in its upwind strip, rate * life * view / box of them
        float rate = style.count * (boxW * boxH) / (life * (2f * w) * (2f * h));

        // a playing system can't change its duration: stopped and cleared, set, and played again (it's
        // prewarmed, so the view refills at once)
        bool playing = particles.isPlaying;
        if (playing) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.transform.position = new Vector3(boxCentre.x, boxCentre.y, 0f);
        var main = particles.main;
        main.duration = life;
        main.startLifetime = life;
        main.startSize = style.scale;
        main.maxParticles = Mathf.Max(16, Mathf.CeilToInt(rate * life * 1.5f) + 8);
        var shape = particles.shape;
        shape.position = Vector3.zero;
        shape.scale = new Vector3(boxW, boxH, 0f);
        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = rate;
        var velocity = particles.velocityOverLifetime;
        velocity.x = dir.x * style.speed;
        velocity.y = dir.y * style.speed;
        velocity.z = 0f;
        if (playing) particles.Play();
    }

    // the sun is out and the sky isn't overcast
    private bool Wanted()
    {
        bool sun = biome == Biome.Nighttime || biome == Biome.DesertNight
            ? DarknessManager.instance != null && DarknessManager.instance.SunlightActive
            : true;
        WeatherManager weather = WeatherManager.instance;
        bool overcast = weather != null && (weather.HasWeather(WeatherType.Rain) || weather.HasWeather(WeatherType.Snow)
            || weather.HasWeather(WeatherType.Sandstorm) || weather.HasWeather(WeatherType.Underwater));
        return sun && !overcast;
    }

    private void Update()
    {
        if (particles == null) return;
        // real time, like the weather's particles
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        Configure();
        float target = Wanted() ? 1f : 0f;
        if (shown != target || appliedOpacity != style.opacity)
        {
            shown = Mathf.MoveTowards(shown, target, dt / style.fadeSeconds);
            ApplyAlpha();
        }
    }

    private float appliedOpacity = -1f;

    // the whole system's darkness: the material's colour (the particles are black, fading at their ends)
    private void ApplyAlpha()
    {
        appliedOpacity = style.opacity;
        Color c = new Color(0f, 0f, 0f, style.opacity * shown);
        if (material.HasProperty("_Color")) material.SetColor("_Color", c);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", c);
        particleRenderer.enabled = shown > 0f;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
