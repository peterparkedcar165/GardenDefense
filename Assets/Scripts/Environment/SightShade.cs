using UnityEngine;

// the red part of a range circle: where its plant can't target, behind obstacles and cliffs (LineOfSight;
// Tommy, 2026-11-27). a child of the circle, so it shows, hides and scales with it, drawn as a sprite like the circle
// itself, with the circle's own material and sorting. it used to be a mesh, but the sprite shader takes its colour from
// the sprite renderer, so on a mesh it often drew nothing (Tommy saw no red on placed plants). its picture is painted
// from the rays, and only again when they or the range change
public class SightShade : MonoBehaviour
{
    // well over the circle itself (0.2), so the blocked part reads clearly (Tommy, 2026-11-27: 0.32, then 0.5, then this)
    private static readonly Color32 ShadeColor = new Color32(255, 38, 38, 179);
    // texels across the circle: 256 keeps the edge smooth on the widest ranges
    private const int Size = 256;

    // per texel: its direction as a turn (0 to 1) and how far it is from the middle (circle space: the edge is at 0.5);
    // shared by every shade, like the paint buffer
    private static float[] _texelTurn;
    private static float[] _texelDist;
    private static Color32[] _paint;

    private SpriteRenderer _renderer;
    private Texture2D _texture;
    private Sprite _sprite;
    private RayFan _fan;
    private int _version = -1;
    private float _range = -1f;
    private int _blocked;

    // the circle's shade, made the first time it's needed
    public static SightShade On(Transform circle)
    {
        Transform found = circle.Find("SightShade");
        if (found != null && found.TryGetComponent(out SightShade existing) && existing._renderer != null) return existing;
        GameObject go = new GameObject("SightShade", typeof(SpriteRenderer), typeof(SightShade));
        go.layer = circle.gameObject.layer;
        go.transform.SetParent(circle, false);
        SightShade shade = go.GetComponent<SightShade>();
        shade._texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "SightShade", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
        };
        // one unit across, like the circle's own sprite, which its transform scales to twice the range
        shade._sprite = Sprite.Create(shade._texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect);
        shade._renderer = go.GetComponent<SpriteRenderer>();
        shade._renderer.sprite = shade._sprite;
        SpriteRenderer circleSprite = circle.GetComponent<SpriteRenderer>();
        if (circleSprite == null) circleSprite = circle.GetComponentInChildren<SpriteRenderer>(true);
        if (circleSprite != null && circleSprite != shade._renderer)
        {
            shade._renderer.sharedMaterial = circleSprite.sharedMaterial;
            shade._renderer.sortingLayerID = circleSprite.sortingLayerID;
            shade._renderer.sortingOrder = circleSprite.sortingOrder;
        }
        shade._renderer.enabled = false;
        return shade;
    }

    // the lines from the circle's middle (RayFan), or null for a plant that sees everything; version changes whenever
    // they're cast again
    public void Show(RayFan fan, int version, float range)
    {
        if (fan == null) { Hide(); return; }
        if (ReferenceEquals(fan, _fan) && version == _version && Mathf.Approximately(range, _range)) return;
        _fan = fan;
        _version = version;
        _range = range;
        _renderer.enabled = Paint(fan, range);
    }

    public void Hide()
    {
        if (_renderer != null) _renderer.enabled = false;
        _fan = null;
        _version = -1;
    }

    // a texel is red when it's inside the circle and a body standing there would be hidden from the middle (RayFan,
    // with heights). false if nothing's hidden
    private bool Paint(RayFan fan, float range)
    {
        EnsureTables();
        float toWorld = 2f * range;
        _blocked = 0;
        for (int i = 0; i < _paint.Length; i++)
        {
            float d = _texelDist[i];
            bool hidden = d <= 0.5f && !fan.Lit(fan.Index01(_texelTurn[i]), d * toWorld, RayFan.Kind.Body);
            _paint[i] = hidden ? ShadeColor : default;
            if (hidden) _blocked++;
        }
        if (_blocked == 0) return false;
        _texture.SetPixels32(_paint);
        _texture.Apply(false);
        return true;
    }

    private static void EnsureTables()
    {
        if (_texelTurn != null) return;
        _texelTurn = new float[Size * Size];
        _texelDist = new float[Size * Size];
        _paint = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float u = (x + 0.5f) / Size - 0.5f, v = (y + 0.5f) / Size - 0.5f;
                int i = y * Size + x;
                _texelDist[i] = Mathf.Sqrt(u * u + v * v);
                float turn = Mathf.Atan2(v, u) / (2f * Mathf.PI);
                _texelTurn[i] = turn < 0f ? turn + 1f : turn;
            }
        }
    }

    private void OnDestroy()
    {
        if (_sprite != null) Destroy(_sprite);
        if (_texture != null) Destroy(_texture);
    }

#if UNITY_EDITOR
    // for LineOfSight.LogReadout (F8): the red area's own state
    public string Describe()
    {
        Transform circle = transform.parent;
        return $"red area: active {gameObject.activeInHierarchy}, renderer {(_renderer == null ? "missing" : _renderer.enabled ? "on" : "off")}, " +
               $"{_blocked} red texels of {Size * Size}, built for range {_range:F2} version {_version}, " +
               $"layer {(_renderer != null ? _renderer.sortingLayerName + " " + _renderer.sortingOrder : "-")}, " +
               $"material {(_renderer != null && _renderer.sharedMaterial != null ? _renderer.sharedMaterial.name : "none")}, " +
               $"bounds {(_renderer != null ? _renderer.bounds.center.ToString("F2") + " " + _renderer.bounds.size.ToString("F2") : "-")}; " +
               $"circle '{(circle != null ? circle.name : "-")}' active {(circle != null && circle.gameObject.activeInHierarchy)}, " +
               $"scale {(circle != null ? circle.lossyScale.ToString("F2") : "-")}";
    }
#endif
}
