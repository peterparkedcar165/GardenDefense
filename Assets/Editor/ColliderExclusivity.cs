using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Collider and LowCollider never share a cell (Tommy, 2026-11-28): painting a tile on one clears that cell on the other
// one beside it under the same grid, so the newest paint wins. only a tile put down clears the other map, never one
// taken away, so the clearing doesn't bounce back. editor only (nothing paints them in play); the clearing is undone
// with the paint
[InitializeOnLoad]
public static class ColliderExclusivity
{
    static ColliderExclusivity()
    {
        Tilemap.tilemapTileChanged -= OnTilesChanged;
        Tilemap.tilemapTileChanged += OnTilesChanged;
    }

    private static void OnTilesChanged(Tilemap map, Tilemap.SyncTile[] changes)
    {
        if (map == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string otherName = map.name == "Collider" ? "LowCollider" : map.name == "LowCollider" ? "Collider" : null;
        if (otherName == null) return;
        Tilemap other = GridTilemaps.Sibling(map, otherName);
        if (other == null) return;
        bool recorded = false;
        foreach (Tilemap.SyncTile change in changes)
        {
            if (change.tile == null || !other.HasTile(change.position)) continue;
            if (!recorded)
            {
                Undo.RecordObject(other, "Paint " + map.name);
                recorded = true;
            }
            other.SetTile(change.position, null);
        }
    }
}
