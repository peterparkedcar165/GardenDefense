using UnityEngine;
using UnityEngine.Tilemaps;

// the pine forest brush (Tommy, 2026-11-27: "fill it up with seamless groups of trees", the pine groups side by side left
// grass between them that looked free to build on). paint a forest's cells and it stands one of his pines (the 2 by 3
// pine_single) on every spot where a whole pine fits inside the forest, one cell apart both ways like his blocks, so the
// forest is one seamless mass. each cell shows every pine over it, the nearer ones (lower) over the ones behind, and the
// left one under the right one in a row: pieces[mask] holds those composites, made by scratchpad pines/forest_assets.py.
// mask bit (dy + 2) * 2 + (dx + 1) says a pine stands with its 2 by 3 box's bottom left cell at (x + dx, y + dy), dx -1
// or 0, dy -2 to 0. a forest is the brush's cells on the Collider and Non-Collider_Air maps together (a canopy over
// walkable ground goes on the air map), and past the map's edge (no Ground tile) it counts as forest, so trees along
// the edge go on past it and the camera cuts them; nothing is painted outside the map
[CreateAssetMenu(fileName = "PineForestTile", menuName = "Garden Defense/Pine Forest Tile")]
public class PineForestTile : TileBase
{
    [Tooltip("64 composites by mask (0 unused): the forest sheet's pieces in order")]
    public Sprite[] pieces;
    [Tooltip("what it shows in a tile palette")]
    public Sprite paletteIcon;

    private const int Full = 63;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        Tilemap self = tilemap.GetComponent<Tilemap>();
        Tilemap ground = Sibling(self, "Ground");
        if (ground == null || ground == self || ground.GetUsedTilesCount() == 0)
            tileData.sprite = paletteIcon;
        else
        {
            int mask = MaskAt(position, tilemap, self, ground);
            tileData.sprite = Pick(mask == 0 ? Full : mask);
        }
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockColor;
        tileData.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
    }

    // a cell's look depends on the cells 1 to each side and 2 up and down, on both maps
    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        Tilemap self = tilemap.GetComponent<Tilemap>();
        Tilemap other = Other(self);
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                Vector3Int q = position + new Vector3Int(dx, dy, 0);
                tilemap.RefreshTile(q);
                if (other != null && other.GetTile(q) == this) other.RefreshTile(q);
            }
    }

    private int MaskAt(Vector3Int p, ITilemap map, Tilemap self, Tilemap ground)
    {
        Tilemap other = Other(self);
        int mask = 0;
        for (int dy = -2; dy <= 0; dy++)
            for (int dx = -1; dx <= 0; dx++)
                if (PineFits(p.x + dx, p.y + dy, map, other, ground)) mask |= 1 << ((dy + 2) * 2 + (dx + 1));
        return mask;
    }

    // a whole pine (2 wide, 3 tall) fits with its bottom left cell here
    private bool PineFits(int x, int y, ITilemap map, Tilemap other, Tilemap ground)
    {
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 3; j++)
                if (!IsForest(x + i, y + j, map, other, ground)) return false;
        return true;
    }

    private bool IsForest(int x, int y, ITilemap map, Tilemap other, Tilemap ground)
    {
        Vector3Int q = new Vector3Int(x, y, 0);
        if (map.GetTile(q) == this) return true;
        if (other != null && other.GetTile(q) == this) return true;
        return !ground.HasTile(q);
    }

    private Sprite Pick(int mask) => pieces != null && mask >= 0 && mask < pieces.Length ? pieces[mask] : null;

    // the forest's other map: Collider for a cell on Non-Collider_Air and the other way round
    private static Tilemap Other(Tilemap self)
    {
        if (self == null) return null;
        return self.name == "Collider" ? Sibling(self, "Non-Collider_Air") : self.name == "Non-Collider_Air" ? Sibling(self, "Collider") : null;
    }

    private static Tilemap Sibling(Tilemap self, string name)
    {
        if (self == null || self.transform.parent == null) return null;
        Transform t = self.transform.parent.Find(name);
        return t != null ? t.GetComponent<Tilemap>() : null;
    }
}
