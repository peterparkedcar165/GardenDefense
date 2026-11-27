using UnityEngine;
using UnityEngine.Tilemaps;

// the pine wall brush (Tommy, 2026-11-27; gd_ground_pack trees/pine_wall): paint a column two cells wide on the Collider
// map and every cell picks its piece of the wall, a column of medium pines one every one and a half cells, each nearer
// one drawn over the one behind. sprites in reading order (left to right, top to bottom, two per row).
//   a column with both ends inside the map: 3 rows is one pine (single); 5, 8, 11.. rows TOP + MID x k + END_EVEN;
//     6, 9, 12.. rows TOP + MID x k + END_ODD (the pack's recipe); 1 or 2 rows show the crown (TOP); 4, 7, 10.. rows,
//     which no wall fits, are drawn as if cut at the top
//   cut by the map's edge (no Ground tile beyond) the wall goes on past it: cut at the top it ends in END_EVEN with
//     MIDs above, cut at the bottom it starts with TOP and MIDs below
// halves pair up along each row from the left; a row of an odd number of cells that starts at the map's left edge pairs
// from the right instead (its first cell is a right half whose left half is off the map). a column's length is read
// along its left half
[CreateAssetMenu(fileName = "PineWallTile", menuName = "Garden Defense/Pine Wall Tile")]
public class PineWallTile : TileBase
{
    [Tooltip("pine_wall_top, 2 x 2")] public Sprite[] top;
    [Tooltip("pine_wall_mid, 2 x 3, repeats")] public Sprite[] mid;
    [Tooltip("pine_wall_end_even, 2 x 3")] public Sprite[] endEven;
    [Tooltip("pine_wall_end_odd, 2 x 4")] public Sprite[] endOdd;
    [Tooltip("one pine on its own, the 2 by 3 tree (tree_0..5)")] public Sprite[] single;
    [Tooltip("what it shows in a tile palette")] public Sprite paletteIcon;

    // how far a column or row is followed
    private const int MaxScan = 64;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        Tilemap ground = GroundOf(tilemap);
        tileData.sprite = paletteIcon != null && !InLevel(tilemap) ? paletteIcon : PieceAt(position, tilemap, ground);
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockColor;
        tileData.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
    }

    // a change anywhere in a column can change every piece in it and the pairing beside it
    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = 0; dy <= MaxScan; dy++)
            {
                Vector3Int q = position + new Vector3Int(dx, dy, 0);
                tilemap.RefreshTile(q);
                if (dy > 0 && tilemap.GetTile(q) != this) break;
            }
            for (int dy = 1; dy <= MaxScan; dy++)
            {
                Vector3Int q = position + new Vector3Int(dx, -dy, 0);
                tilemap.RefreshTile(q);
                if (tilemap.GetTile(q) != this) break;
            }
        }
    }

    private bool Is(ITilemap map, int x, int y) => map.GetTile(new Vector3Int(x, y, 0)) == this;

    private static bool OffMap(Tilemap ground, int x, int y) => ground != null && !ground.HasTile(new Vector3Int(x, y, 0));

    private Sprite PieceAt(Vector3Int p, ITilemap map, Tilemap ground)
    {
        // the row's run, and which half this cell is
        int x0 = p.x, x1 = p.x;
        for (int i = 0; i < MaxScan && Is(map, x0 - 1, p.y); i++) x0--;
        for (int i = 0; i < MaxScan && Is(map, x1 + 1, p.y); i++) x1++;
        bool fromRight = (x1 - x0 + 1) % 2 == 1 && OffMap(ground, x0 - 1, p.y);
        int col = fromRight ? ((x1 - p.x) % 2 == 0 ? 1 : 0) : (p.x - x0) % 2;

        // the column's length, along its left half when that's on the map
        int sx = col == 1 && Is(map, p.x - 1, p.y) ? p.x - 1 : p.x;
        int top = p.y, bottom = p.y;
        for (int i = 0; i < MaxScan && Is(map, sx, top + 1); i++) top++;
        for (int i = 0; i < MaxScan && Is(map, sx, bottom - 1); i++) bottom--;
        bool openTop = OffMap(ground, sx, top + 1), openBottom = OffMap(ground, sx, bottom - 1);
        return Piece(top - bottom + 1, top - p.y, openTop, openBottom, col);
    }

    // the piece for row r (0 at the column's top) of a column of rows rows
    private Sprite Piece(int rows, int r, bool openTop, bool openBottom, int col)
    {
        if (!openTop && !openBottom)
        {
            if (rows == 3) return Pick(single, r, col);
            if (rows <= 2) return Pick(top, r, col);
            if ((rows - 5) % 3 == 0) return Closed(rows, r, col, endEven, 3);
            if ((rows - 6) % 3 == 0) return Closed(rows, r, col, endOdd, 4);
            openTop = true;   // no wall fits: drawn as if cut at the top
        }
        if (openTop && openBottom) return Pick(mid, r % 3, col);
        if (openTop)
        {
            int fromBottom = rows - 1 - r;
            if (fromBottom < 3) return Pick(endEven, 2 - fromBottom, col);
            return Pick(mid, 2 - (fromBottom - 3) % 3, col);
        }
        if (r < 2) return Pick(top, r, col);
        return Pick(mid, (r - 2) % 3, col);
    }

    private Sprite Closed(int rows, int r, int col, Sprite[] end, int endRows)
    {
        if (r < 2) return Pick(top, r, col);
        if (r >= rows - endRows) return Pick(end, r - (rows - endRows), col);
        return Pick(mid, (r - 2) % 3, col);
    }

    private static Sprite Pick(Sprite[] sheet, int row, int col)
    {
        int i = row * 2 + col;
        return sheet != null && i >= 0 && i < sheet.Length ? sheet[i] : null;
    }

    // the level's Ground map beside the one painted on (none in a palette)
    private static Tilemap GroundOf(ITilemap map)
    {
        Tilemap self = map.GetComponent<Tilemap>();
        if (self == null || self.transform.parent == null) return null;
        Transform g = self.transform.parent.Find("Ground");
        Tilemap ground = g != null ? g.GetComponent<Tilemap>() : null;
        return ground != null && ground != self && ground.GetUsedTilesCount() > 0 ? ground : null;
    }

    private static bool InLevel(ITilemap map) => GroundOf(map) != null;
}
