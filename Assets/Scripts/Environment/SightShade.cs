using UnityEngine;

// the red part of a range circle: where its plant can't target, behind obstacles and cliffs (LineOfSight;
// Tommy, 2026-11-27). a child of the circle, so it shows, hides and scales with it, drawn with the circle's
// own material and sorting. it only rebuilds when the rays or the range change
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SightShade : MonoBehaviour
{
    // well over the circle itself (0.2), so the blocked part reads clearly (Tommy, 2026-11-27: 0.32, then 0.5, then this)
    private static readonly Color ShadeColor = new Color(1f, 0.15f, 0.15f, 0.7f);

    private Mesh _mesh;
    private MeshRenderer _renderer;
    private float[] _rays;
    private int _version = -1;
    private float _range = -1f;

    // the circle's shade, made the first time it's needed
    public static SightShade On(Transform circle)
    {
        Transform found = circle.Find("SightShade");
        if (found != null) return found.GetComponent<SightShade>();
        GameObject go = new GameObject("SightShade", typeof(MeshFilter), typeof(MeshRenderer), typeof(SightShade));
        go.layer = circle.gameObject.layer;
        go.transform.SetParent(circle, false);
        SightShade shade = go.GetComponent<SightShade>();
        shade._mesh = new Mesh { name = "SightShade" };
        shade._mesh.MarkDynamic();
        go.GetComponent<MeshFilter>().sharedMesh = shade._mesh;
        shade._renderer = go.GetComponent<MeshRenderer>();
        SpriteRenderer sprite = circle.GetComponentInChildren<SpriteRenderer>(true);
        if (sprite != null)
        {
            shade._renderer.sharedMaterial = sprite.sharedMaterial;
            shade._renderer.sortingLayerID = sprite.sortingLayerID;
            shade._renderer.sortingOrder = sprite.sortingOrder;
        }
        shade._renderer.enabled = false;
        return shade;
    }

    // rays from the circle's middle (LineOfSight.Cast), or null for a plant that sees everything; version
    // changes whenever the rays are cast again
    public void Show(float[] rays, int version, float range)
    {
        if (rays == null) { _renderer.enabled = false; _rays = null; return; }
        if (ReferenceEquals(rays, _rays) && version == _version && Mathf.Approximately(range, _range)) return;
        _rays = rays;
        _version = version;
        _range = range;
        _renderer.enabled = LineOfSight.BuildShade(_mesh, rays, range, ShadeColor);
    }

    public void Hide()
    {
        if (_renderer != null) _renderer.enabled = false;
        _rays = null;
    }

#if UNITY_EDITOR
    // for LineOfSight.LogReadout (F8): the red area's own state
    public string Describe()
    {
        Transform circle = transform.parent;
        return $"red area: active {gameObject.activeInHierarchy}, renderer {(_renderer == null ? "missing" : _renderer.enabled ? "on" : "off")}, " +
               $"mesh {(_mesh == null ? "missing" : _mesh.vertexCount + " vertices")}, built for range {_range:F2} version {_version}, " +
               $"layer {(_renderer != null ? _renderer.sortingLayerName + " " + _renderer.sortingOrder : "-")}, " +
               $"material {(_renderer != null && _renderer.sharedMaterial != null ? _renderer.sharedMaterial.name : "none")}, " +
               $"bounds {(_renderer != null ? _renderer.bounds.center.ToString("F2") + " " + _renderer.bounds.size.ToString("F2") : "-")}; " +
               $"circle '{(circle != null ? circle.name : "-")}' active {(circle != null && circle.gameObject.activeInHierarchy)}, " +
               $"scale {(circle != null ? circle.lossyScale.ToString("F2") : "-")}, children named SightShade {(circle != null ? CountShades(circle) : 0)}";
    }

    private static int CountShades(Transform circle)
    {
        int n = 0;
        foreach (Transform child in circle) if (child.name == "SightShade") n++;
        return n;
    }
#endif

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
    }
}
