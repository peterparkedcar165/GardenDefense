using UnityEngine;
using UnityEngine.Tilemaps;

// one brush for a set of look alike tiles (Tommy, 2026-11-27): every cell it's painted on shows one of its
// variants, picked from the cell's position, so painting, box filling or flood filling with it scatters them
// (the three clovers, the sixteen boulders). a cell always gets the same pick, so a scene looks the same every
// time it opens. an animated variant keeps its own animation (the clovers' sway, on unscaled time). to the
// game it's just a tile on its map, like the variant it shows
[CreateAssetMenu(fileName = "RandomVariantTile", menuName = "Garden Defense/Random Variant Tile")]
public class RandomVariantTile : TileBase
{
    [Tooltip("the tiles it picks from")]
    public TileBase[] variants;
    [Tooltip("another number gives every cell another pick")]
    public int seed;

    public TileBase VariantAt(Vector3Int cell)
    {
        if (variants == null || variants.Length == 0) return null;
        unchecked
        {
            uint h = (uint)cell.x * 73856093u ^ (uint)cell.y * 19349663u ^ (uint)seed * 83492791u;
            h ^= h >> 13;
            h *= 0x5bd1e995u;
            h ^= h >> 15;
            TileBase pick = variants[(int)(h % (uint)variants.Length)];
            return pick != this ? pick : null;
        }
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        TileBase pick = VariantAt(position);
        if (pick != null) pick.GetTileData(position, tilemap, ref tileData);
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        TileBase pick = VariantAt(position);
        return pick != null && pick.GetTileAnimationData(position, tilemap, ref tileAnimationData);
    }

    public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
    {
        TileBase pick = VariantAt(position);
        return pick == null || pick.StartUp(position, tilemap, go);
    }
}
