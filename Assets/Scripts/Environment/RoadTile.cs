using UnityEngine;
using UnityEngine.Tilemaps;

// a road that lays itself (Tommy, 2026-11-27): painted on the Path map, every cell picks its piece of the road
// sheet from the road around it, so one brush draws straights, corners, T junctions, crossings and dead ends,
// and roads two or more cells wide with their inner corners. a cell holding a hand placed road piece counts as
// road too, so it joins old roads. past the map's edge (where the level's Ground map has no tile) a cell counts
// as the one beside it inside the map, as for the cliffs, so a road painted up to the edge runs on off the map
[CreateAssetMenu(fileName = "RoadTile", menuName = "Garden Defense/Road Tile")]
public class RoadTile : TileBase
{
    [Tooltip("the road sheet's pieces in order, Roads_0 to Roads_24")]
    public Sprite[] pieces;

    // the piece for the road on each side, N 1, E 2, S 4, W 8 (a lone cell shows a straight, so the brush
    // looks like a road in the palette). corners and crossings also look at the diagonals, in PieceAt
    private static readonly int[] ByMask = { 20, 11, 17, 9, 2, 12, 0, 21, 18, 10, 20, 24, 1, 22, 23, 8 };

    private static readonly Vector3Int N = new Vector3Int(0, 1, 0), E = new Vector3Int(1, 0, 0);
    private static readonly Vector3Int S = new Vector3Int(0, -1, 0), W = new Vector3Int(-1, 0, 0);

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        int piece = PieceAt(position, tilemap);
        tileData.sprite = pieces != null && piece < pieces.Length ? pieces[piece] : null;
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockColor;
        tileData.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
    }

    // painting or erasing a cell changes the pieces around it
    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                tilemap.RefreshTile(position + new Vector3Int(x, y, 0));
    }

    private int PieceAt(Vector3Int p, ITilemap map)
    {
        Tilemap ground = GroundOf(map);
        bool n = IsRoad(map, ground, p, N), e = IsRoad(map, ground, p, E);
        bool s = IsRoad(map, ground, p, S), w = IsRoad(map, ground, p, W);
        int mask = (n ? 1 : 0) | (e ? 2 : 0) | (s ? 4 : 0) | (w ? 8 : 0);
        switch (mask)
        {
            // a corner whose inside diagonal is road too is part of a wider road: no inner corner nub
            case 3: return IsRoad(map, ground, p, N + E) ? 13 : 9;
            case 6: return IsRoad(map, ground, p, S + E) ? 4 : 0;
            case 9: return IsRoad(map, ground, p, N + W) ? 14 : 10;
            case 12: return IsRoad(map, ground, p, S + W) ? 5 : 1;
            case 15:
            {
                // road all round: the middle of a wide road, one inner corner, or a crossing of narrow roads
                bool ne = IsRoad(map, ground, p, N + E), se = IsRoad(map, ground, p, S + E);
                bool sw = IsRoad(map, ground, p, S + W), nw = IsRoad(map, ground, p, N + W);
                int open = (ne ? 0 : 1) + (se ? 0 : 1) + (sw ? 0 : 1) + (nw ? 0 : 1);
                if (open == 0) return 3;
                if (open == 1) return !se ? 6 : !sw ? 7 : !ne ? 15 : 16;
                return 8;
            }
        }
        return ByMask[mask];
    }

    private bool IsRoad(ITilemap map, Tilemap ground, Vector3Int p, Vector3Int d)
    {
        Vector3Int q = p + d;
        if (ground != null && !ground.HasTile(q))
        {
            // past the map's edge: the cell beside it inside the map (for a corner cell's diagonal, the cell itself)
            Vector3Int keepX = new Vector3Int(q.x, p.y, q.z), keepY = new Vector3Int(p.x, q.y, q.z);
            if (d.x != 0 && d.y != 0 && ground.HasTile(keepX)) q = keepX;
            else if (d.x != 0 && d.y != 0 && ground.HasTile(keepY)) q = keepY;
            else q = p;
        }
        TileBase t = map.GetTile(q);
        if (t == null) return false;
        if (t == this) return true;
        return t is UnityEngine.Tilemaps.Tile tile && tile.sprite != null && pieces != null && System.Array.IndexOf(pieces, tile.sprite) >= 0;
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
