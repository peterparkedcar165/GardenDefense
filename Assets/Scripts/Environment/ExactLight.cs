using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// a still light drawn to the pixel (Tommy, 2026-11-28, "still lights": plants and lamps). its point light becomes
// three sprite lights whose pictures are made here, once and again when the light moves, changes size, steps on or off
// high ground, or the blockers change (LightMask.Version). each picture is the point light's own glow (URP's falloff,
// copied below) on the level's pixel grid, dark where something stands between the light and what's there (Heights:
// the light sits height over its ground, and a straight line to a thing is stopped only by what rises above it):
//   the ground light (the ground, cliffs, everything but bodies and obstacle art) is lit where the ground is;
//   the body light (the Insects and Plants layers) is lit where a body standing there is (Heights.Body over its
//   ground), which is what its owner can see and target: both read the owner's own lines (RayFan);
//   the art light (the Colliders and Non-Colliders_Air layers: trees, boulders, bushes) lights every obstacle whole
//   (Tommy, 2026-11-28: no shadow lines cutting across a tree), stopped only by cliffs.
// whoever owns the light keeps setting its size on the point light fields (pointLightOuterRadius and
// pointLightInnerRadius, falloffIntensity), its intensity, colour and on or off as before; this follows them. it runs
// before the lights' own late update, so a new picture and its shape go out in the same frame
[RequireComponent(typeof(Light2D))]
[DefaultExecutionOrder(-50)]
public class ExactLight : MonoBehaviour
{
    // the light stands on high ground (its owner keeps this current)
    public bool fromHigh;
    // read high ground from the tile under it instead (lamps)
    public bool highFromTile;
    // how high the light sits over its ground (Heights: a plant's Eye, a lamp's Lamp)
    public float height = Heights.Eye;
    // the owner's own lines out to at least a reach (a plant's, which its sight reads too); none: the light casts its own
    public System.Func<float, RayFan> fanSource;

    private Light2D _light;
    private Light2D _art;
    private Light2D _bodies;
    private Texture2D _groundTexture, _artTexture, _bodyTexture;
    private Sprite _groundSprite, _artSprite, _bodySprite;
    private RayFan _ownFan;
    private RayFan _fan;
    private bool _built;
    private Vector2 _origin;
    private float _z;
    private float _outer, _inner, _falloff;
    private int _version = -1;
    // pictures replaced while a cross fade (LightFader) may still show them
    private readonly List<(Object item, float at)> _retired = new List<(Object, float)>();
    private const float RetireDelay = 3f;

    // the obstacle art's light and the bodies' light, for a cross fade's copies (LightFader)
    public Light2D ArtLight => _art;
    public Light2D BodyLight => _bodies;

    public static ExactLight Attach(Light2D light, bool fromHigh, bool highFromTile = false, float height = Heights.Eye)
    {
        if (light == null) return null;
        ExactLight exact = light.GetComponent<ExactLight>();
        if (exact == null) exact = light.gameObject.AddComponent<ExactLight>();
        exact.fromHigh = fromHigh;
        exact.highFromTile = highFromTile;
        exact.height = height;
        return exact;
    }

    // the sorting layers each light covers: obstacle art, bodies, and the rest for the ground light
    private static int[] _artLayers, _bodyLayers, _groundLayers;
    private static void EnsureLayers()
    {
        if (_artLayers != null) return;
        List<int> art = new List<int>(), bodies = new List<int>(), ground = new List<int>();
        foreach (SortingLayer layer in SortingLayer.layers)
        {
            if (layer.name == "Colliders" || layer.name == "Non-Colliders_Air") art.Add(layer.id);
            else if (layer.name == "Insects" || layer.name == "Plants") bodies.Add(layer.id);
            else ground.Add(layer.id);
        }
        _artLayers = art.ToArray();
        _bodyLayers = bodies.ToArray();
        _groundLayers = ground.ToArray();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { _artLayers = null; _bodyLayers = null; _groundLayers = null; }

    private void Awake()
    {
        _light = GetComponent<Light2D>();
        // the size it starts with, read before the light type changes
        _outer = _light.pointLightOuterRadius;
        _inner = _light.pointLightInnerRadius;
        _falloff = _light.falloffIntensity;
        EnsureChildren();
    }

    // the art and body lights, found again after a script reload in play (statics and unsaved fields come back empty)
    private void EnsureChildren()
    {
        EnsureLayers();
        if (_light == null) _light = GetComponent<Light2D>();
        if (_art == null) _art = Child("ArtLight", _artLayers);
        if (_bodies == null) _bodies = Child("BodyLight", _bodyLayers);
    }

    private Light2D Child(string name, int[] layers)
    {
        Transform found = transform.Find(name);
        if (found != null && found.TryGetComponent(out Light2D existing)) return existing;
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform, false);
        Light2D light = obj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Sprite;
        light.shadowsEnabled = false;
        light.targetSortingLayers = layers;
        light.enabled = false;
        return light;
    }

    private void LateUpdate()
    {
        EnsureChildren();
        KeepUnscaled();
        // the owner drives the ground light; the others follow it. the layers are set only when they differ (setting
        // them copies the list every time)
        if (!SameLayers(_light.targetSortingLayers, _groundLayers)) _light.targetSortingLayers = _groundLayers;
        Follow(_art);
        Follow(_bodies);

        float z = (highFromTile ? TileHigh() : fromHigh) ? Heights.Cliff + height : height;
        Vector2 here = transform.position;
        float outer = _light.pointLightOuterRadius, inner = _light.pointLightInnerRadius, falloff = _light.falloffIntensity;
        if (!_built || here != _origin || Mathf.Abs(z - _z) > 1e-4f || outer != _outer || inner != _inner || falloff != _falloff || LightMask.Version != _version)
        {
            _outer = outer; _inner = inner; _falloff = falloff;
            Rebuild(here, z);
            _art.enabled = _light.enabled;
            _bodies.enabled = _light.enabled;
        }
        for (int i = _retired.Count - 1; i >= 0; i--)
        {
            if (Time.unscaledTime < _retired[i].at) continue;
            if (_retired[i].item != null) Destroy(_retired[i].item);
            _retired.RemoveAt(i);
        }
    }

    private static bool SameLayers(int[] a, int[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    private void Follow(Light2D other)
    {
        other.color = _light.color;
        other.intensity = _light.intensity;
        other.blendStyleIndex = _light.blendStyleIndex;
        other.enabled = _light.enabled && _built;
    }

    private void OnDestroy()
    {
        foreach (Object o in new Object[] { _groundSprite, _groundTexture, _artSprite, _artTexture, _bodySprite, _bodyTexture })
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

    // does this light reach a body standing at a point (gameplay, a lamp's): false in reached when something stops it
    // first. answers only while its lines are current for this spot, otherwise the caller walks the line itself
    public bool TryReaches(Vector2 point, out bool reached)
    {
        reached = true;
        if (!_built || _fan == null || !_fan.Fits(transform.position, _z, 0f)) return false;
        reached = _fan.Sees(point);
        return true;
    }

    // the light's height over low ground, for a caller walking a line from it
    public float Z => _built ? _z : ((highFromTile ? TileHigh() : fromHigh) ? Heights.Cliff + height : height);

    private void Rebuild(Vector2 origin, float z)
    {
        _built = true;
        _origin = origin;
        _z = z;
        _version = LightMask.Version;
        float r = Mathf.Max(0.05f, _outer);
        const int ppu = LightMask.PixelsPerUnit;

        RayFan fan = fanSource != null ? fanSource(r) : null;
        if (fan == null || !fan.Fits(origin, z, r))
        {
            if (_ownFan == null || !_ownFan.Fits(origin, z, r)) _ownFan = RayFan.Cast(origin, z, r, _ownFan, 2f);
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
        Color[] bodies = new Color[w * h];
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
                bool groundLit = fan.Lit(k, dist, RayFan.Kind.Ground);
                bool bodyLit = fan.Lit(k, dist, RayFan.Kind.Body);
                bool artLit = fan.Lit(k, dist, RayFan.Kind.Art);
                if (!groundLit && !bodyLit && !artLit) continue;
                float v = Mathf.Lerp(Mathf.Pow(t, e0), Mathf.Pow(t, e1), rowMix);
                Color c = new Color(v, v, v, 1f);
                int i = y * w + x;
                if (groundLit) ground[i] = c;
                if (bodyLit) bodies[i] = c;
                if (artLit) art[i] = c;
            }
        }

        Vector2 pivot = new Vector2((origin.x - corner.x) * ppu / w, (origin.y - corner.y) * ppu / h);
        Retire(_groundSprite, _groundTexture);
        Retire(_artSprite, _artTexture);
        Retire(_bodySprite, _bodyTexture);
        _groundSprite = Picture(ground, w, h, pivot, out _groundTexture);
        _artSprite = Picture(art, w, h, pivot, out _artTexture);
        _bodySprite = Picture(bodies, w, h, pivot, out _bodyTexture);

        _light.lightType = Light2D.LightType.Sprite;
        _light.lightCookieSprite = _groundSprite;
        _light.shadowsEnabled = false;
        if (!SameLayers(_light.targetSortingLayers, _groundLayers)) _light.targetSortingLayers = _groundLayers;
        _art.lightCookieSprite = _artSprite;
        _bodies.lightCookieSprite = _bodySprite;
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
