using System.Collections.Generic;
using UnityEngine;

// cloud shadows over levels with sun (Tommy, 2026-11-26), made with the same particle system as the rain,
// snow and sandstorm (Tommy: same system, same logic, other numbers): one copy of the sandstorm's prefab
// per size of shadow (CloudShadowStyle.sizes: his 256, 512 and 1024 px shapes, each size packed in its own
// sheet), its numbers changed here. every shadow is one random cell of its sheet, pitch black, drawn at
// exactly its own pixels (64 a tile, like the map: a 512 px cloud covers 512 game pixels; Tommy, no
// mixels), see through at the style's opacity. they drift up and to the right at the style's angle over
// the whole camera view, on real time like the weather (on while paused, no faster at a higher game
// speed), drawn above everything in the world and under the mulch drops, indicators and ui. they cover the
// camera's fully zoomed out view (CameraFit.ZoomedOutView), set once: zooming or panning never moves or
// re-scatters them. like the rain, a shadow is born along a line just upwind of that view and lives until
// it's just past it, so none appears or goes in sight; each system is prewarmed, so the view starts full.
// where two overlap the pixel is darkened once (Custom/CloudShadow, a stencil mark per pixel; Tommy). they
// show on day, desert and snow levels, and on night and desert night levels only while Sunny weather has
// the sun out; never in the cave or underwater (the interiors). they fade out while it rains, snows or a
// sandstorm blows. made by ProceduralLevel when the level starts, built once the camera has centred
public class CloudShadows : MonoBehaviour
{
    // game pixels a tile, the map's
    private const float PixelsPerUnit = 64f;

    // one size's particle system and what it was last set up with
    private class Layer
    {
        public CloudShadowStyle.CloudSize size;
        public ParticleSystem particles;
        public ParticleSystemRenderer renderer;
        public Material material;
        public float speed = -1f, angle = -1f, count = -1f;
        public int cellPixels = -1;
    }

    private CloudShadowStyle style;
    private Biome biome;
    private readonly List<Layer> layers = new List<Layer>();
    private float shown;

    public static CloudShadows Create(LevelConfig config)
    {
        if (config == null) return null;
        Biome biome = config.CurrentBiome;
        // the interiors never get them
        if (biome == Biome.Cave || biome == Biome.Underwater) return null;
        CloudShadowStyle style = CloudShadowStyle.Instance;
        if (style.particlePrefab == null || style.sizes == null || style.sizes.Length == 0)
        {
            Debug.LogWarning("[CloudShadows] Resources/CloudShadowStyle.asset has no particle prefab or sizes (or didn't load). Right click it and Reimport.");
            return null;
        }
        GameObject go = new GameObject("CloudShadows");
        CloudShadows clouds = go.AddComponent<CloudShadows>();
        clouds.style = style;
        clouds.biome = biome;
        // built once the level's camera has centred (its zoomed out view is known), see Update
        return clouds;
    }

    // the area they cover, fixed when they're built
    private Rect view;
    private bool built;
    private float waited;

    // the camera's fully zoomed out view, or (no CameraFit in the scene) what the camera shows now
    private static bool TryGetView(bool fallback, out Rect rect)
    {
        rect = CameraFit.ZoomedOutView;
        if (rect.width > 0f) return true;
        if (!fallback) return false;
        Camera cam = Camera.main;
        float h = cam != null && cam.orthographic ? cam.orthographicSize : 8.5f;
        float w = h * (cam != null ? cam.aspect : 16f / 9f);
        Vector3 c = cam != null ? cam.transform.position : Vector3.zero;
        rect = new Rect(c.x - w, c.y - h, 2f * w, 2f * h);
        return true;
    }

    private void Build()
    {
        foreach (CloudShadowStyle.CloudSize size in style.sizes)
        {
            if (size == null || size.sheet == null) continue;
            Layer layer = BuildLayer(size);
            if (layer != null) layers.Add(layer);
        }
        shown = Wanted() ? 1f : 0f;
        ApplyAlpha();
        foreach (Layer layer in layers) layer.particles.Play();
    }

    private Layer BuildLayer(CloudShadowStyle.CloudSize size)
    {
        GameObject made = Instantiate(style.particlePrefab, transform);
        made.name = "CloudShadows " + size.cellPixels;
        ParticleSystem particles = made.GetComponentInChildren<ParticleSystem>();
        if (particles == null)
        {
            Destroy(made);
            return null;
        }
        // only the one system: any others in the prefab go
        foreach (ParticleSystem other in made.GetComponentsInChildren<ParticleSystem>(true))
            if (other != particles) other.gameObject.SetActive(false);
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.transform.rotation = Quaternion.identity;
        particles.transform.localScale = Vector3.one;
        Layer layer = new Layer { size = size, particles = particles, renderer = particles.GetComponent<ParticleSystemRenderer>() };

        // Custom/CloudShadow, so two overlapping shadows darken a pixel once (Tommy); without it, the
        // weather's own material. either given this size's sheet, black
        layer.material = style.shader != null ? new Material(style.shader) : new Material(layer.renderer.sharedMaterial);
        layer.material.name = made.name;
        layer.material.mainTexture = size.sheet;
        layer.renderer.sharedMaterial = layer.material;
        layer.renderer.renderMode = ParticleSystemRenderMode.Billboard;
        layer.renderer.sortingLayerName = style.sortingLayer;
        layer.renderer.sortingOrder = style.sortingOrder;
        // big shadows are allowed (the sandstorm's grains were held to half the screen; a 1024 px cloud is
        // the whole view when zoomed in)
        layer.renderer.maxParticleSize = 10f;
        // half of them mirrored, for more shapes
        layer.renderer.flip = new Vector3(0.5f, 0f, 0f);

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
        main.simulationSpeed = 1f;
        // never paused for looking off camera
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.randomDirectionAmount = 0f;

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        // the sandstorm halves its velocity (speed modifier 0.5): left at that, the shadows moved at half the
        // style's speed and their lives (worked out from it) ran out halfway across the view (Tommy: they
        // vanished mid screen)
        velocity.speedModifier = 1f;
        velocity.orbitalX = velocity.orbitalY = velocity.orbitalZ = 0f;
        velocity.radial = 0f;

        // no fading over a shadow's life: every one is born and dies out of sight (Configure), and a half faded
        // one would leave a lighter patch where it overlaps a whole one
        var colour = particles.colorOverLifetime;
        colour.enabled = false;

        // one random cell of the sheet each, kept for its whole life
        var cells = particles.textureSheetAnimation;
        cells.enabled = true;
        cells.mode = ParticleSystemAnimationMode.Grid;
        cells.numTilesX = size.columns;
        cells.numTilesY = size.rows;
        cells.animation = ParticleSystemAnimationType.WholeSheet;
        cells.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.9999f);
        cells.startFrame = 0f;
        cells.cycleCount = 1;

        // the sandstorm's other movers off
        var noise = particles.noise; noise.enabled = false;
        var force = particles.forceOverLifetime; force.enabled = false;
        var grow = particles.sizeOverLifetime; grow.enabled = false;
        var spin = particles.rotationOverLifetime; spin.enabled = false;
        var limit = particles.limitVelocityOverLifetime; limit.enabled = false;
        var inherit = particles.inheritVelocity; inherit.enabled = false;
        var collision = particles.collision; collision.enabled = false;
        var trails = particles.trails; trails.enabled = false;
        var lights = particles.lights; lights.enabled = false;
        var subs = particles.subEmitters; subs.enabled = false;

        Configure(layer);
        return layer;
    }

    // the numbers that follow the style live (speed, direction, how many, the cell size): only a change to
    // one of them sets a system up again; the camera never does
    private void Configure(Layer layer)
    {
        CloudShadowStyle.CloudSize size = layer.size;
        if (style.speed == layer.speed && style.angle == layer.angle && size.count == layer.count && size.cellPixels == layer.cellPixels) return;
        layer.speed = style.speed;
        layer.angle = style.angle;
        layer.count = size.count;
        layer.cellPixels = size.cellPixels;
        ParticleSystem particles = layer.particles;

        // exactly its own pixels: a cell of n pixels is n / 64 tiles across
        float tiles = size.cellPixels / PixelsPerUnit;
        Vector2 centre = view.center;
        float a = style.angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        Vector2 across = new Vector2(-dir.y, dir.x);
        // like the rain, born along a line just upwind of the view and living until just past it: the view's
        // corners measured along the drift and across it, a whole shadow clear of them, so every shadow is
        // born and dies out of sight
        float margin = tiles * 0.75f;
        float tMin = float.MaxValue, tMax = float.MinValue, sMin = float.MaxValue, sMax = float.MinValue;
        foreach (Vector2 corner in new[] { view.min, view.max, new Vector2(view.xMin, view.yMax), new Vector2(view.xMax, view.yMin) })
        {
            Vector2 d = corner - centre;
            float along = Vector2.Dot(d, dir), side = Vector2.Dot(d, across);
            tMin = Mathf.Min(tMin, along); tMax = Mathf.Max(tMax, along);
            sMin = Mathf.Min(sMin, side); sMax = Mathf.Max(sMax, side);
        }
        tMin -= margin; tMax += margin; sMin -= margin; sMax += margin;
        float length = sMax - sMin;
        float life = (tMax - tMin) / Mathf.Max(0.01f, style.speed);
        Vector2 lineCentre = centre + dir * tMin + across * ((sMin + sMax) * 0.5f);
        // so about count are over the view at once: shadows born along the line at this rate cover length *
        // speed of ground a second between them
        float rate = size.count * length * style.speed / Mathf.Max(0.01f, view.width * view.height);

        // a playing system can't change its duration: stopped and cleared, set, and played again (it's
        // prewarmed, so the view refills at once)
        bool playing = particles.isPlaying;
        if (playing) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.transform.position = new Vector3(lineCentre.x, lineCentre.y, 0f);
        var main = particles.main;
        main.duration = life;
        main.startLifetime = life;
        main.startSize = tiles;
        main.maxParticles = Mathf.Max(8, Mathf.CeilToInt(rate * life * 1.3f) + 4);
        // the line: a box only as long as the line, turned so it lies across the drift
        var shape = particles.shape;
        shape.position = Vector3.zero;
        shape.rotation = new Vector3(0f, 0f, style.angle + 90f);
        shape.scale = new Vector3(length, 0f, 0f);
        var emission = particles.emission;
        emission.enabled = rate > 0f;
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
        // real time, like the weather's particles
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        if (!built)
        {
            // the camera centres in its own Start; without a CameraFit, half a second on, what it shows
            waited += dt;
            if (!TryGetView(waited > 0.5f, out view)) return;
            built = true;
            Build();
            if (layers.Count == 0) { Destroy(gameObject); return; }
        }
        foreach (Layer layer in layers) Configure(layer);
        float target = Wanted() ? 1f : 0f;
        if (shown != target || appliedOpacity != style.opacity)
        {
            shown = Mathf.MoveTowards(shown, target, dt / style.fadeSeconds);
            ApplyAlpha();
        }
    }

    private float appliedOpacity = -1f;

    // every size's darkness: the materials' colour (the shapes are pitch black)
    private void ApplyAlpha()
    {
        appliedOpacity = style.opacity;
        Color c = new Color(0f, 0f, 0f, style.opacity * shown);
        foreach (Layer layer in layers)
        {
            if (layer.material.HasProperty("_Color")) layer.material.SetColor("_Color", c);
            if (layer.material.HasProperty("_BaseColor")) layer.material.SetColor("_BaseColor", c);
            layer.renderer.enabled = shown > 0f;
        }
    }

    private void OnDestroy()
    {
        foreach (Layer layer in layers)
            if (layer.material != null) Destroy(layer.material);
    }
}
