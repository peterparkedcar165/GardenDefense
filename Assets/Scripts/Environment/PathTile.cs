using UnityEngine;
using UnityEngine.Tilemaps;

// the dirt path that lays itself (Tommy, 2026-11-27: gd_ground_pack path, roads 1 cell wide): painted on the Path map,
// every cell shows the piece for the sides its road goes on, up 1 + right 2 + down 4 + left 8, in one of its versions
// picked from the cell's position (fixed, so a scene looks the same every time it opens): sprites[version * 16 + mask].
// the pieces are see through round the dirt, so the grass under them shows. at the map's edge (where the level's
// Ground map has no tile) a road end goes on past it: a cell with fewer than two road neighbours on the map joins
// the edge, straight on when it can. in a palette it shows a straight
[CreateAssetMenu(fileName = "PathTile", menuName = "Garden Defense/Path Tile")]
public class PathTile : TileBase
{
    [Tooltip("the pieces, version by version: sprites[version * 16 + mask], mask up 1 + right 2 + down 4 + left 8")]
    public Sprite[] sprites;
    [Tooltip("how many versions of each piece")]
    public int versions = 4;
    [Tooltip("another number gives every cell another version")]
    public int seed;

    private static readonly Vector3Int[] Dirs = { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };
    private const int Straight = 10;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        Tilemap ground = GroundOf(tilemap);
        int count = sprites != null ? Mathf.Clamp(versions, 1, Mathf.Max(1, sprites.Length / 16)) : 1;
        int mask = ground == null ? Straight : MaskAt(position, tilemap, ground);
        int version = ground == null ? 0 : VersionAt(position, count);
        int i = version * 16 + mask;
        tileData.sprite = sprites != null && i < sprites.Length ? sprites[i] : null;
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockColor;
        tileData.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
    }

    // painting or erasing a cell changes the pieces beside it
    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        tilemap.RefreshTile(position);
        foreach (Vector3Int d in Dirs) tilemap.RefreshTile(position + d);
    }

    private int MaskAt(Vector3Int p, ITilemap map, Tilemap ground)
    {
        int mask = 0, roads = 0, only = -1;
        for (int i = 0; i < 4; i++)
        {
            Vector3Int q = p + Dirs[i];
            if (ground.HasTile(q) && map.GetTile(q) == this) { mask |= 1 << i; roads++; only = i; }
        }
        if (roads >= 2) return mask;
        // a road end at the map's edge goes on past it, straight on if it can
        if (roads == 1 && !ground.HasTile(p + Dirs[(only + 2) % 4])) return mask | 1 << ((only + 2) % 4);
        for (int i = 0; i < 4; i++)
            if (!ground.HasTile(p + Dirs[i])) return mask | 1 << i;
        return mask;
    }

    private int VersionAt(Vector3Int cell, int count)
    {
        unchecked
        {
            uint h = (uint)cell.x * 73856093u ^ (uint)cell.y * 19349663u ^ (uint)seed * 83492791u;
            h ^= h >> 13;
            h *= 0x5bd1e995u;
            h ^= h >> 15;
            return (int)(h % (uint)count);
        }
    }

    // the level's Ground map beside the one painted on (none in a palette, or before any ground is painted)
    private static Tilemap GroundOf(ITilemap map)
    {
        Tilemap self = map.GetComponent<Tilemap>();
        if (self == null || self.transform.parent == null) return null;
        Transform g = self.transform.parent.Find("Ground");
        Tilemap ground = g != null ? g.GetComponent<Tilemap>() : null;
        return ground != null && ground != self && ground.GetUsedTilesCount() > 0 ? ground : null;
    }
}
