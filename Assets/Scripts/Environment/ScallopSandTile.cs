using UnityEngine;
using UnityEngine.Tilemaps;

// the desert's sand (Tommy, 2026-11-28: his scallop tile, sheet 5 row 3 col 1, made into an edge matched set of 81
// tiles, gd_scallop_sand). every edge between two cells has one of 3 kinds, worked out from the edge's place and the
// seed, and a cell shows the one of the 81 sprites whose four edges have those kinds: neighbours always meet on the
// same kind, so every join is seamless, and repainting a cell gives the same sprite. paint or box fill it like the
// grass brush. the desert cliff brush is the same tile with a palette icon: painted on HighgroundGround it raises the
// sand, and the cliffs draw themselves around it (CliffBuilder); in a tile palette it shows its icon instead
[CreateAssetMenu(fileName = "ScallopSandTile", menuName = "Garden Defense/Scallop Sand Tile")]
public class ScallopSandTile : TileBase
{
    public const int Kinds = 3;

    [Tooltip("the 81 sprites in sheet order (scallop_sand_00 to 80): a cell's is ((N * 3 + E) * 3 + S) * 3 + W")]
    public Sprite[] sprites = new Sprite[Kinds * Kinds * Kinds * Kinds];
    [Tooltip("another seed gives another arrangement (3, 4 and 9 are the three maps on the preview page)")]
    public int seed = 3;
    [Tooltip("what it shows in a tile palette instead of its sand (the cliff brush); none: its sand")]
    public Sprite paletteIcon;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        bool icon = paletteIcon != null && !RaisedGroundTile.InLevel(tilemap);
        int i = SpriteIndex(position.x, position.y, seed);
        tileData.sprite = icon ? paletteIcon : sprites != null && i < sprites.Length ? sprites[i] : null;
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockColor;
        tileData.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
    }

    // which of the 81 sprites cell (x, y) shows
    public static int SpriteIndex(int x, int y, int seed)
    {
        int n = (int)(Hash(x, y, seed, 1) % Kinds);       // the edge above the cell
        int s = (int)(Hash(x, y - 1, seed, 1) % Kinds);   // the edge below: the one above the cell below
        int e = (int)(Hash(x, y, seed, 2) % Kinds);       // the edge on its right
        int w = (int)(Hash(x - 1, y, seed, 2) % Kinds);   // the edge on its left: the right one of the cell on the left
        return ((n * Kinds + e) * Kinds + s) * Kinds + w;
    }

    private static uint Hash(int x, int y, int seed, uint salt)
    {
        unchecked
        {
            uint h = (uint)x * 73856093u ^ (uint)y * 19349663u ^ (uint)seed * 83492791u ^ salt * 2654435761u;
            h ^= h >> 13;
            h *= 1274126177u;
            h ^= h >> 16;
            return h;
        }
    }
}
