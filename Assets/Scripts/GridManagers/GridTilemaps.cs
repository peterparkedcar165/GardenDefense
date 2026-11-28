using UnityEngine;
using UnityEngine.Tilemaps;

// a tilemap found by its name beside another one under the same grid, for a map a grid manager reads without a
// field wired in every scene (LowCollider, Tommy 2026-11-28)
public static class GridTilemaps
{
    public static Tilemap Sibling(Tilemap near, string name)
    {
        if (near == null || near.transform.parent == null) return null;
        foreach (Transform child in near.transform.parent)
            if (child.name == name && child.TryGetComponent(out Tilemap map)) return map;
        return null;
    }
}
