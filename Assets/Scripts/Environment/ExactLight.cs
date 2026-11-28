using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// a still light drawn to the pixel (Tommy, 2026-11-28, "still lights": plants and lamps). its point light becomes two
// sprite lights whose pictures are made here, once and again when the light moves, changes size, steps on or off high
// ground, or the blockers change (LightMask.Version). each picture is the point light's own glow (URP's falloff, copied
// below) on the level's pixel grid:
//   the ground light (everything but obstacle art: ground, cliffs, plants, insects) is dark past the first thing that
//   stops its line, along the owner's own lines (RayFan: a plant's sight reads the very same ones), so what's lit is
//   what it can see and target;
//   the art light (the Colliders and Non-Colliders_Air layers: trees, boulders, bushes) lights every obstacle whole
//   (Tommy, 2026-11-28: no shadow lines cutting across a tree), stopped only by cliffs from low ground.
// whoever owns the light keeps setting its size on the point light fields (pointLightOuterRadius and
// pointLightInnerRadius, falloffIntensity), its intensity, colour and on or off as before; this follows them. it runs
// before the lights' own late update, so a new picture and its shape go out in the same frame
[RequireComponent(typeof(Light2D))]
[DefaultExecutionOrder(-50)]
public class ExactLight : MonoBehaviour
{
    // the light stands on high ground: only obstacles on high ground stop it (its owner keeps this current)
    public bool fromHigh;
    // read high ground from the tile under it instead (lamps)
    public bool highFromTile;
    // the owner's own lines out to at least a reach (a plant's, which its sight reads too); none: the light casts its own
    public System.Func<float, RayFan> fanSource;

    private Light2D _light;
    private Light2D _art;
    private Texture2D _groundTexture, _artTexture;
    private Sprite _groundSprite, _artSprite;
    private RayFan _ownFan;
    private RayFan _fan;
    private bool _built;
    private Vector2 _origin;
    private bool _high;
    private float _outer, _inner, _falloff;
    private int _version = -1;
    // pictures replaced while a cross fade (LightFader) may still show them
    private readonly List<(Object item, float at)> _retired = new List<(Object, float)>();
    private const float RetireDelay = 3f;

    // the obstacle art's light, for a cross fade's copy (LightFader)
    public Light2D ArtLight => _art;

    public static ExactLight Attach(Light2D light, bool fromHigh, bool highFromTile = false)
    {
        if (light == null) return null;
        ExactLight exact = light.GetComponent<ExactLight>();
        if (exact == null) exact = light.gameObject.AddComponent<ExactLight>();
        exact.fromHigh = fromHigh;
        exact.highFromTile = highFromTile;
        return exact;
    }

    // the sorting layers the art light covers, and the rest for the ground light
    private static int[] _artLayers, _groundLayers;
    private static void EnsureLayers()
    {
        if (_artLayers != null) return;
        List<int> art = new List<int>(), ground = new List<int>();
        foreach (SortingLayer layer in SortingLayer.layers)
            (layer.name == "Colliders" || layer.name == "Non-Colliders_Air" ? art : ground).Add(layer.id);
        _artLayers = art.ToArray();
        _groundLayers = ground.ToArray();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { _artLayers = null; _groundLayers = null; }

    private void Awake()
    {
        _light = GetComponent<Light2D>();
        // the size it starts with, read before the light type changes
        _outer = _light.pointLightOuterRadius;
        _inner = _light.pointLightInnerRadius;
        _falloff = _light.falloffIntensity;
        EnsureLayers();
        GameObject artObj = new GameObject("ArtLight");
        artObj.transform.SetParent(transform, false);
        _art = artObj.AddComponent<Light2D>();
        _art.lightType = Light2D.LightType.Sprite;
        _art.shadowsEnabled = false;
        _art.targetSortingLayers = _artLayers;
        _art.enabled = false;
    }

    private void LateUpdate()
    {
        KeepUnscaled();
        // the owner drives the ground light; the art light follows it
        if (_light.targetSortingLayers != _groundLayers) _light.targetSortingLayers = _groundLayers;
        _art.color = _light.color;
        _art.intensity = _light.intensity;
        _art.blendStyleIndex = _light.blendStyleIndex;
        _art.enabled = _light.enabled && _built;

        bool high = highFromTile ? TileHigh() : fromHigh;
        Vector2 here = transform.position;
        float outer = _light.pointLightOuterRadius, inner = _light.pointLightInnerRadius, falloff = _light.falloffIntensity;
        if (!_built || here != _origin || high != _high || outer != _outer || inner != _inner || falloff != _falloff || LightMask.Version != _version)
        {
            _outer = outer; _inner = inner; _falloff = falloff;
            Rebuild(here, high);
            _art.enabled = _light.enabled;
        }
        for (int i = _retired.Count - 1; i >= 0; i--)
        {
            if (Time.unscaledTime < _retired[i].at) continue;
            if (_retired[i].item != null) Destroy(_retired[i].item);
            _retired.RemoveAt(i);
        }
    }

    private void OnDestroy()
    {
        foreach (Object o in new Object[] { _groundSprite, _groundTexture, _artSprite, _artTexture })
            if (o != null) Destroy(o);
        foreach ((Object item, float _) in _retired) if (item != null) Destroy(item);
        _retired.Clear();
    }

    // the pictures lie on the world's pixel grid, so they mustn't be scaled or turned with whatever they hang off
    private void KeepUnscaled()
    {
        if (transform.rotation != Quaternion.identity) transform.rotation = Quaternion.identity;
        Transform parent = transform.parent;
        if (parent == null) { if (transform.localScale != Vector3.one) transform.localScale = Vector3.one; return; }
        Vector3 s = parent.lossyScale;
        if (Mathf.Abs(s.x) < 1e-5f || Mathf.Abs(s.y) < 1e-5f) return;
        Vector3 want = new Vector3(1f / s.x, 1f / s.y, 1f);
        if ((transform.localScale - want).sqrMagnitude > 1e-8f) transform.localScale = want;
    }

    private bool TileHigh()
    {
        Tile t = Tile.At(transform.position);
        return t != null && t.isHighground;
    }

    // does this light reach a point (gameplay, a lamp's): false in reached when something stops it first. answers only
    // while its lines are current for this spot and ground, otherwise the caller walks the line itself
    public bool TryReaches(Vector2 point, bool high, out bool reached)
    {
        reached = true;
        if (!_built || _fan == null || !_fan.Fits(transform.position, high, 0f, false)) return false;
        reached = _fan.Sees(point);
        return true;
    }

    private void Rebuild(Vector2 origin, bool high)
    {
        _built = true;
        _origin = origin;
        _high = high;
        _version = LightMask.Version;
        float r = Mathf.Max(0.05f, _outer);
        const int ppu = LightMask.PixelsPerUnit;

        RayFan fan = fanSource != null ? fanSource(r) : null;
        if (fan == null || fan.Origin != origin || fan.FromHigh != high || fan.Reach < r || !fan.HasWalls)
        {
            if (_ownFan == null || !_ownFan.Fits(origin, high, r, true)) _ownFan = RayFan.Cast(origin, high, r, _ownFan, 2f, true);
            fan = _ownFan;
        }
        _fan = fan;

        // the pictures: the grid's pixels the light's circle touches
        Vector2 grid = LightMask.GridOrigin;
        int x0 = Mathf.FloorToInt((origin.x - r - grid.x) * ppu), x1 = Mathf.CeilToInt((origin.x + r - grid.x) * ppu);
        int y0 = Mathf.FloorToInt((origin.y - r - grid.y) * ppu), y1 = Mathf.CeilToInt((origin.y + r - grid.y) * ppu);
        int w = Mathf.Max(1, x1 - x0), h = Mathf.Max(1, y1 - y0);
        Vector2 corner = grid + new Vector2(x0, y0) / ppu;

        // the point light's falloff as URP draws it: one minus d over r, pushed out by the inner radius, then its falloff
        // curve (Light2DLookupTexture: two rows of pow(t, e) picked by falloffIntensity). the pictures are written the
        // way URP writes that lookup (the same sRGB format, colours through SetPixels), so the card reads both back alike
        float innerMult = 1f / Mathf.Max(1e-4f, 1f - Mathf.Clamp01(_inner / r));
        float rowAt = _falloff * 128f - 0.5f;
        int row0 = Mathf.Clamp(Mathf.FloorToInt(rowAt), 0, 127), row1 = Mathf.Clamp(Mathf.FloorToInt(rowAt) + 1, 0, 127);
        float rowMix = Mathf.Clamp01(rowAt - Mathf.Floor(rowAt));
        if (rowAt < 0f) { row0 = row1 = 0; rowMix = 0f; }
        float e0 = FalloffExponent(row0), e1 = FalloffExponent(row1);

        Color[] ground = new Color[w * h];
        Color[] art = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float cy = corner.y + (y + 0.5f) / ppu;
            for (int x = 0; x < w; x++)
            {
                float cx = corner.x + (x + 0.5f) / ppu;
                Vector2 d = new Vector2(cx - origin.x, cy - origin.y);
                float dist = d.magnitude;
                if (dist >= r) continue;
                float t = Mathf.Clamp01(innerMult * (1f - dist / r));
                if (t <= 0f) continue;
                int k = fan.Index(d);
                bool groundLit = dist <= fan.Ground(k);
                bool artLit = dist <= fan.Walls(k);
                if (!groundLit && !artLit) continue;
                float v = Mathf.Lerp(Mathf.Pow(t, e0), Mathf.Pow(t, e1), rowMix);
                Color c = new Color(v, v, v, 1f);
                if (groundLit) ground[y * w + x] = c;
                if (artLit) art[y * w + x] = c;
            }
        }

        Vector2 pivot = new Vector2((origin.x - corner.x) * ppu / w, (origin.y - corner.y) * ppu / h);
        Retire(_groundSprite, _groundTexture);
        Retire(_artSprite, _artTexture);
        _groundSprite = Picture(ground, w, h, pivot, out _groundTexture);
        _artSprite = Picture(art, w, h, pivot, out _artTexture);

        EnsureLayers();
        _light.lightType = Light2D.LightType.Sprite;
        _light.lightCookieSprite = _groundSprite;
        _light.shadowsEnabled = false;
        _light.targetSortingLayers = _groundLayers;
        _art.lightCookieSprite = _artSprite;
    }

    private void Retire(Sprite sprite, Texture2D texture)
    {
        if (sprite != null) _retired.Add((sprite, Time.unscaledTime + RetireDelay));
        if (texture != null) _retired.Add((texture, Time.unscaledTime + RetireDelay));
    }

    private static Sprite Picture(Color[] pixels, int w, int h, Vector2 pivot, out Texture2D tex)
    {
        tex = new Texture2D(w, h, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB, UnityEngine.Experimental.Rendering.TextureCreationFlags.None)
        {
            name = "ExactLight",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        tex.SetPixels(pixels);
        tex.Apply(false, true);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, LightMask.PixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.name = "ExactLight";
        return sprite;
    }

    // the falloff curve's exponent on one row of URP's lookup (Light2DLookupTexture.CreateFalloffLookupTexture)
    private static float FalloffExponent(int row)
    {
        float baseValue = (row + 64) / 256f;
        return Mathf.Log(1f - baseValue) / Mathf.Log(baseValue);
    }
}
