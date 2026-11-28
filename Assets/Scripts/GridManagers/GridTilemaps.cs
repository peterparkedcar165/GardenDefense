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

    // an obstacle cell's shape from whichever map holds its tile (Tile.ShapeObstacle, Tommy 2026-11-28), and for a pine
    // forest cell whether a pine stands in it (light and sight, Heights.Tree)
    public static void ShapeObstacle(Tile tile, Vector3Int cell, params Tilemap[] maps)
    {
        if (tile == null || tile.tileType != TileType.Obstacle) return;
        foreach (Tilemap map in maps)
        {
            if (map == null || !map.HasTile(cell)) continue;
            Sprite sprite = map.GetSprite(cell);
            tile.ShapeObstacle(sprite, map.GetTransformMatrix(cell));
            if (map.GetTile(cell) is PineForestTile forest) tile.MarkForest(forest.PineStandsIn(sprite));
            return;
        }
    }
}
