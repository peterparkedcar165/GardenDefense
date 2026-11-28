using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// a still light drawn to the pixel (Tommy, 2026-11-28, "still lights": plants and lamps). its point light becomes a
// sprite light whose picture is made here, once and again when the light moves, changes size, steps on or off high
// ground, or the blockers change (LightMask.Version). the picture is the point light's own glow (URP's falloff,
// copied below) on the level's pixel grid, dark past whatever stops it: rays go out one every half pixel round its
// edge, through LightMask, and a pixel is lit when it's nearer than the first blocker on its ray. the obstacle a ray
// meets first stays lit itself (the near tree is lit, the ones behind it aren't). the same rays answer the gameplay
// question (Reaches), so what's lit and what looks lit agree.
// whoever owns the light keeps setting its size on the point light fields (pointLightOuterRadius and
// pointLightInnerRadius, falloffIntensity) as before; this follows them. it runs before the light's own late update,
// so a new picture and its shape go out in the same frame
[RequireComponent(typeof(Light2D))]
[DefaultExecutionOrder(-50)]
public class ExactLight : MonoBehaviour
{
    // the light stands on high ground: only obstacles on high ground stop it (its owner keeps this current)
    public bool fromHigh;
    // read high ground from the tile under it instead (lamps)
    public bool highFromTile;

    private Light2D _light;
    private Texture2D _texture;
    private Sprite _sprite;
    private float[] _rays;
    private int[] _hits;
    private int _rayCount;
    private float _reach;          // how far the rays were walked
    private bool _built;
    private Vector2 _origin;
    private bool _high;
    private float _outer, _inner, _falloff;
    private int _version = -1;
    // pictures replaced while a cross fade (LightFader) may still show them
    private readonly List<(Object item, float at)> _retired = new List<(Object, float)>();
    private const float RetireDelay = 3f;

    public static ExactLight Attach(Light2D light, bool fromHigh, bool highFromTile = false)
    {
        if (light == null) return null;
        ExactLight exact = light.GetComponent<ExactLight>();
        if (exact == null) exact = light.gameObject.AddComponent<ExactLight>();
        exact.fromHigh = fromHigh;
        exact.highFromTile = highFromTile;
        return exact;
    }

    private void Awake()
    {
        _light = GetComponent<Light2D>();
        // the size it starts with, read before the light type changes
        _outer = _light.pointLightOuterRadius;
        _inner = _light.pointLightInnerRadius;
        _falloff = _light.falloffIntensity;
    }

    private void LateUpdate()
    {
        KeepUnscaled();
        bool high = highFromTile ? TileHigh() : fromHigh;
        Vector2 here = transform.position;
        float outer = _light.pointLightOuterRadius, inner = _light.pointLightInnerRadius, falloff = _light.falloffIntensity;
        if (!_built || here != _origin || high != _high || outer != _outer || inner != _inner || falloff != _falloff || LightMask.Version != _version)
        {
            _outer = outer; _inner = inner; _falloff = falloff;
            Rebuild(here, high);
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
        if (_sprite != null) Destroy(_sprite);
        if (_texture != null) Destroy(_texture);
        foreach ((Object item, float _) in _retired) if (item != null) Destroy(item);
        _retired.Clear();
    }

    // the picture lies on the world's pixel grid, so it mustn't be scaled or turned with whatever it hangs off
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

    // does this light reach a point (gameplay): false in reached when something stops it first. answers only while the
    // rays are current for this spot and ground, otherwise the caller walks the line itself
    public bool TryReaches(Vector2 point, bool high, out bool reached)
    {
        reached = true;
        if (!_built || high != _high || (Vector2)transform.position != _origin || LightMask.Version != _version) return false;
        Vector2 d = point - _origin;
        float dist = d.magnitude;
        if (dist < 1e-4f) return true;
        if (dist > _reach) return false;
        reached = dist <= _rays[RayIndex(d)];
        return true;
    }

    private int RayIndex(Vector2 d)
    {
        float a = Mathf.Atan2(d.y, d.x);
        if (a < 0f) a += 2f * Mathf.PI;
        return Mathf.RoundToInt(a / (2f * Mathf.PI) * _rayCount) % _rayCount;
    }

    private void Rebuild(Vector2 origin, bool high)
    {
        _built = true;
        _origin = origin;
        _high = high;
        _version = LightMask.Version;
        float r = Mathf.Max(0.05f, _outer);
        const int ppu = LightMask.PixelsPerUnit;

        // the rays, out to the edge (gameplay asking past it walks its own line)
        _reach = r;
        _rayCount = Mathf.Max(64, Mathf.CeilToInt(2f * Mathf.PI * _reach * ppu * 2f));
        if (_rays == null || _rays.Length != _rayCount) { _rays = new float[_rayCount]; _hits = new int[_rayCount]; }
        int skip = LightMask.OwnCell(origin);
        float step = 2f * Mathf.PI / _rayCount;
        for (int i = 0; i < _rayCount; i++)
        {
            float a = i * step;
            _rays[i] = LightMask.March(origin, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), _reach, high, skip, out _hits[i]);
        }

        // the picture: the grid's pixels the light's circle touches
        Vector2 grid = LightMask.GridOrigin;
        int x0 = Mathf.FloorToInt((origin.x - r - grid.x) * ppu), x1 = Mathf.CeilToInt((origin.x + r - grid.x) * ppu);
        int y0 = Mathf.FloorToInt((origin.y - r - grid.y) * ppu), y1 = Mathf.CeilToInt((origin.y + r - grid.y) * ppu);
        int w = Mathf.Max(1, x1 - x0), h = Mathf.Max(1, y1 - y0);
        Vector2 corner = grid + new Vector2(x0, y0) / ppu;

        // the point light's falloff as URP draws it: one minus d over r, pushed out by the inner radius, then its falloff curve
        // (Light2DLookupTexture: two rows of pow(t, e) picked by falloffIntensity). the picture is written the way URP
        // writes that lookup (the same sRGB format, colours through SetPixels), so the card reads both back alike
        float innerMult = 1f / Mathf.Max(1e-4f, 1f - Mathf.Clamp01(_inner / r));
        float rowAt = _falloff * 128f - 0.5f;
        int row0 = Mathf.Clamp(Mathf.FloorToInt(rowAt), 0, 127), row1 = Mathf.Clamp(Mathf.FloorToInt(rowAt) + 1, 0, 127);
        float rowMix = Mathf.Clamp01(rowAt - Mathf.Floor(rowAt));
        if (rowAt < 0f) { row0 = row1 = 0; rowMix = 0f; }
        float e0 = FalloffExponent(row0), e1 = FalloffExponent(row1);

        Color[] pixels = new Color[w * h];
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
                int k = dist < 1e-4f ? 0 : RayIndex(d);
                bool lit = dist <= _rays[k];
                if (!lit && _hits[k] > 0) lit = LightMask.OwnerAt(new Vector2(cx, cy)) == _hits[k];
                if (!lit) continue;
                float v = Mathf.Lerp(Mathf.Pow(t, e0), Mathf.Pow(t, e1), rowMix);
                pixels[y * w + x] = new Color(v, v, v, 1f);
            }
        }

        Texture2D tex = new Texture2D(w, h, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB, UnityEngine.Experimental.Rendering.TextureCreationFlags.None)
        {
            name = "ExactLight",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        tex.SetPixels(pixels);
        tex.Apply(false, true);
        Vector2 pivot = new Vector2((origin.x - corner.x) * ppu / w, (origin.y - corner.y) * ppu / h);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
        sprite.name = "ExactLight";

        if (_sprite != null) _retired.Add((_sprite, Time.unscaledTime + RetireDelay));
        if (_texture != null) _retired.Add((_texture, Time.unscaledTime + RetireDelay));
        _texture = tex;
        _sprite = sprite;

        _light.lightType = Light2D.LightType.Sprite;
        _light.lightCookieSprite = sprite;
        _light.shadowsEnabled = false;
    }

    // the falloff curve's exponent on one row of URP's lookup (Light2DLookupTexture.CreateFalloffLookupTexture)
    private static float FalloffExponent(int row)
    {
        float baseValue = (row + 64) / 256f;
        return Mathf.Log(1f - baseValue) / Mathf.Log(baseValue);
    }
}
