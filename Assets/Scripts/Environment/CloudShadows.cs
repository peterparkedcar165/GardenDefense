using System.Collections.Generic;
using UnityEngine;

// cloud shadows over levels with sun (Tommy, 2026-11-26): his black cloud shapes (CloudShadowStyle)
// drift right and a little up over the whole camera view, drawn above everything in the world and
// under the indicators and the ui, see through at the style's opacity. they show on day, desert and
// snow levels, and on night and desert night levels only while Sunny weather has the sun out; never in
// the cave or underwater (the interiors). they fade out while it rains, snows or a sandstorm blows. like
// the rain they run on real time: on while the game is paused and no faster at a higher game speed.
// each one leaving the view on the right or the top comes back in on the left or the bottom, at a new
// random spot, with a new random shape. built in code by ProceduralLevel when the level starts
public class CloudShadows : MonoBehaviour
{
    // art pixels a world unit, the map's: positions snap to them so the edges never shimmer
    private const float PixelsPerUnit = 64f;

    private class Shadow
    {
        public SpriteRenderer renderer;
        public Vector2 position;
    }

    private CloudShadowStyle style;
    private Biome biome;
    private readonly List<Shadow> shadows = new List<Shadow>();
    private float shown;
    private float margin;

    public static CloudShadows Create(LevelConfig config)
    {
        if (config == null) return null;
        Biome biome = config.CurrentBiome;
        // the interiors never get them
        if (biome == Biome.Cave || biome == Biome.Underwater) return null;
        CloudShadowStyle style = CloudShadowStyle.Instance;
        if (style.shapes == null || style.shapes.Length == 0)
        {
            // the settings asset didn't load (it can be imported before its script compiles: reimport it)
            Debug.LogWarning("[CloudShadows] no cloud shapes: Resources/CloudShadowStyle.asset didn't load or has none. Right click it and Reimport.");
            return null;
        }
        if (style.count <= 0) return null;
        GameObject go = new GameObject("CloudShadows");
        CloudShadows clouds = go.AddComponent<CloudShadows>();
        clouds.style = style;
        clouds.biome = biome;
        clouds.Build();
        return clouds;
    }

    private void Build()
    {
        // room outside the view for the biggest shape, so they come and go out of sight
        appliedScale = Mathf.Max(1, style.scale);
        foreach (Sprite shape in style.shapes)
            if (shape != null) margin = Mathf.Max(margin, shape.bounds.extents.magnitude * appliedScale);
        margin += 0.5f;

        Rect area = Area();
        // spread over the area: one in each cell of a rough grid, at a random spot in it, so they start
        // apart instead of in clumps
        int columns = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(style.count * area.width / Mathf.Max(0.01f, area.height))));
        int rows = Mathf.Max(1, Mathf.CeilToInt(style.count / (float)columns));
        var cells = new List<int>();
        for (int i = 0; i < columns * rows; i++) cells.Add(i);
        for (int i = 0; i < style.count; i++)
        {
            int pick = Random.Range(0, cells.Count);
            int cell = cells[pick];
            cells.RemoveAt(pick);
            float cx = area.xMin + (cell % columns + Random.value) * area.width / columns;
            float cy = area.yMin + (cell / columns + Random.value) * area.height / rows;
            AddShadow(new Vector2(cx, cy));
        }
        shown = Wanted() ? 1f : 0f;
        ApplyAlpha();
    }

    private void AddShadow(Vector2 position)
    {
        GameObject go = new GameObject("CloudShadow", typeof(SpriteRenderer));
        go.transform.SetParent(transform, false);
        Shadow shadow = new Shadow { renderer = go.GetComponent<SpriteRenderer>(), position = position };
        shadow.renderer.sortingLayerName = style.sortingLayer;
        shadow.renderer.sortingOrder = style.sortingOrder;
        if (style.material != null) shadow.renderer.sharedMaterial = style.material;
        go.transform.localScale = Vector3.one * Mathf.Max(1, style.scale);
        NewShape(shadow);
        Place(shadow);
        shadows.Add(shadow);
    }

    // the count and the size follow the style live, for tuning while playing: new ones appear anywhere
    // over the view, extra ones go, and every one takes the new whole number size
    private int appliedScale = 1;

    private void FollowStyle()
    {
        int scale = Mathf.Max(1, style.scale);
        if (scale != appliedScale)
        {
            appliedScale = scale;
            margin = 0f;
            foreach (Sprite shape in style.shapes)
                if (shape != null) margin = Mathf.Max(margin, shape.bounds.extents.magnitude * scale);
            margin += 0.5f;
            foreach (Shadow shadow in shadows) shadow.renderer.transform.localScale = Vector3.one * scale;
        }
        if (shadows.Count == style.count) return;
        Rect area = Area();
        while (shadows.Count < style.count)
            AddShadow(new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax)));
        while (shadows.Count > Mathf.Max(0, style.count))
        {
            Shadow last = shadows[shadows.Count - 1];
            shadows.RemoveAt(shadows.Count - 1);
            Destroy(last.renderer.gameObject);
        }
        ApplyAlpha();
    }


    // the camera's view and the margin round it, in world units
    private Rect Area()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return new Rect(-15f - margin, -9f - margin, 30f + 2f * margin, 18f + 2f * margin);
        float h = cam.orthographicSize, w = h * cam.aspect;
        Vector3 c = cam.transform.position;
        return new Rect(c.x - w - margin, c.y - h - margin, 2f * (w + margin), 2f * (h + margin));
    }

    // a random shape, flipped or not
    private void NewShape(Shadow shadow)
    {
        Sprite shape = null;
        for (int tries = 0; tries < 8 && shape == null; tries++) shape = style.shapes[Random.Range(0, style.shapes.Length)];
        shadow.renderer.sprite = shape;
        shadow.renderer.flipX = Random.value < 0.5f;
    }

    private void Place(Shadow shadow)
    {
        Vector2 p = shadow.position;
        shadow.renderer.transform.position = new Vector3(Mathf.Round(p.x * PixelsPerUnit) / PixelsPerUnit,
            Mathf.Round(p.y * PixelsPerUnit) / PixelsPerUnit, 0f);
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
        // real time, so a pause or the game speed never changes them; a long hitch doesn't jump them
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        FollowStyle();

        float target = Wanted() ? 1f : 0f;
        // the opacity follows the style live, so it can be tuned in the inspector while playing
        if (shown != target || appliedOpacity != style.opacity)
        {
            shown = Mathf.MoveTowards(shown, target, dt / style.fadeSeconds);
            ApplyAlpha();
        }

        float a = style.angle * Mathf.Deg2Rad;
        Vector2 step = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (style.speed * dt);
        Rect area = Area();
        foreach (Shadow shadow in shadows)
        {
            shadow.position += step;
            // out on the right: back in on the left, at any height. out at the top: back in at the bottom
            if (shadow.position.x > area.xMax)
            {
                shadow.position = new Vector2(area.xMin, Random.Range(area.yMin, area.yMax));
                NewShape(shadow);
            }
            else if (shadow.position.y > area.yMax)
            {
                shadow.position = new Vector2(Random.Range(area.xMin, area.xMax), area.yMin);
                NewShape(shadow);
            }
            // the view moved (the camera fitting the screen): keep them over it
            else if (shadow.position.x < area.xMin - 1f || shadow.position.y < area.yMin - 1f)
                shadow.position = new Vector2(Mathf.Max(shadow.position.x, area.xMin), Mathf.Max(shadow.position.y, area.yMin));
            Place(shadow);
        }
    }

    private float appliedOpacity = -1f;

    private void ApplyAlpha()
    {
        appliedOpacity = style.opacity;
        Color c = new Color(0f, 0f, 0f, style.opacity * shown);
        foreach (Shadow shadow in shadows)
        {
            shadow.renderer.color = c;
            shadow.renderer.enabled = shown > 0f;
        }
    }
}
