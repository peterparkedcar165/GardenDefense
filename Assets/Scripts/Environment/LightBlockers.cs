using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// the visible side of light and walls for lights that move (Tommy, 2026-11-28; still lights are drawn exactly by
// ExactLight): obstacle cells (their art's outline) and cliff edges get URP 2D shadow casters, so a light's glow shows
// where it's blocked, as gameplay has it (LightMask). a caster's shape can only be given
// before it wakes (its path field is private), so it's made on an inactive object, shaped, then switched on. its shadow
// falls on everything but the obstacles' own art (the Colliders and Non-Colliders_Air layers), so a tree or boulder isn't
// darkened by its own shadow. which lights cast shadows is DarknessManager's business
public static class LightBlockers
{
    private static readonly FieldInfo ShapePath = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo Layers = typeof(ShadowCaster2D).GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
    private static int[] _layers;

    private static int[] ShadowedLayers()
    {
        if (_layers != null) return _layers;
        List<int> ids = new List<int>();
        foreach (SortingLayer layer in SortingLayer.layers)
            if (layer.name != "Colliders" && layer.name != "Non-Colliders_Air") ids.Add(layer.id);
        return _layers = ids.ToArray();
    }

    // a caster under parent with this outline, in the parent's local space. none on a URP without the path field
    // (no shadow rather than a wrong one)
    public static ShadowCaster2D Add(Transform parent, Vector3[] path, string name = "Shadow")
    {
        if (ShapePath == null || parent == null || path == null || path.Length < 3) return null;
        GameObject go = new GameObject(name);
        go.SetActive(false);
        go.transform.SetParent(parent, false);
        ShadowCaster2D caster = go.AddComponent<ShadowCaster2D>();
        ShapePath.SetValue(caster, path);
        Layers?.SetValue(caster, ShadowedLayers());
        caster.castingOption = ShadowCaster2D.ShadowCastingOptions.CastShadow;
        go.SetActive(true);
        return caster;
    }

    // a square cell, a whole cell wide so a row of them leaves no seam for light to slip through
    public static Vector3[] Cell => new[] { new Vector3(-0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f), new Vector3(0.5f, -0.5f) };

    // a thin wall along a segment (a cliff edge), in local space
    public static Vector3[] Wall(Vector2 a, Vector2 b, float thickness)
    {
        Vector2 n = (b - a).normalized;
        n = new Vector2(-n.y, n.x) * (thickness * 0.5f);
        return new Vector3[] { a - n, a + n, b + n, b - n };
    }

}
