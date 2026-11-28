using UnityEngine;
using UnityEngine.Tilemaps;

// streams and ponds that join themselves (Tommy, 2026-11-27: gd_ground_pack water): painted on a level's water map,
// every cell shows the piece for the sides its water goes on, up 1 + right 2 + down 4 + left 8 (0 a pond on its own,
// 5 straight up and down, 10 straight across, 15 a crossing), and plays its 16 frame ripple, every cell from the same
// moment so the joins stay seamless: frames[mask * frameCount + frame]. the pieces are see through outside the bank, so
// the grass under them shows. the pack's version stood on its own PathTile; this one stands alone, since the game's
// PathTile has its own map edge rules.
// wider water (Tommy, 2026-11-28: the desert's lakes): a piece's bank keeps a little inner corner wherever its water goes
// round a corner, which shows as a speck in the middle of a lake. with lakeFrames set, a cell whose water goes round a
// corner on both sides, with water in the cell across that corner too, shows its piece with that corner filled with
// water instead (corners: up right 1, down right 2, down left 4, up left 8)
[CreateAssetMenu(fileName = "WaterTile", menuName = "Garden Defense/Water Tile")]
public class WaterTile : TileBase
{
    [Tooltip("None: only drawn. Grid: the whole cell is solid. Sprite: only the water shape (both need a TilemapCollider2D on the map)")]
    public UnityEngine.Tilemaps.Tile.ColliderType colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
    [Tooltip("every piece's frames: piece 0's, then piece 1's, and so on (16 pieces times the frame count)")]
    public Sprite[] frames = new Sprite[0];
    [Tooltip("optional, for lakes: the pieces again with inner corners filled with water, one set of frames for every piece and corner set it can fill, in LakeIndex order (31 sets). empty: streams and ponds only")]
    public Sprite[] lakeFrames = new Sprite[0];
    public int frameCount = 16;
    [Tooltip("frames a second, times the map's Animation Frame Rate (keep that at 1). 8 is a 2 second loop; the sea tiles use the same")]
    public float fps = 8f;

    private static readonly Vector3Int[] Sides = { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };
    // the corners, each with the two sides it lies between and the cell across it
    private static readonly int[] CornerSides = { 1 | 2, 2 | 4, 4 | 8, 8 | 1 };
    private static readonly Vector3Int[] Across =
        { new Vector3Int(1, 1, 0), new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0), new Vector3Int(-1, 1, 0) };
    private static readonly Vector3Int[] Around =
    {
        Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left,
        new Vector3Int(1, 1, 0), new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0), new Vector3Int(-1, 1, 0)
    };

    public const int LakeSets = 31;
    // where each piece and corner set lies in lakeFrames: pieces in order, each one's fillable corner sets counting up,
    // none filled left out; -1 for a set the piece can't fill
    private static readonly int[] LakeOrder = BuildLakeOrder();
    private static int[] BuildLakeOrder()
    {
        int[] order = new int[16 * 16];
        int next = 0;
        for (int mask = 0; mask < 16; mask++)
        {
            int fillable = Fillable(mask);
            for (int fills = 0; fills < 16; fills++)
                order[mask * 16 + fills] = fills != 0 && (fills & ~fillable) == 0 ? next++ : -1;
        }
        return order;
    }

    // the corners a piece's water goes round on both sides
    public static int Fillable(int mask)
    {
        int corners = 0;
        for (int c = 0; c < 4; c++)
            if ((mask & CornerSides[c]) == CornerSides[c]) corners |= 1 << c;
        return corners;
    }

    // where a piece's set with these corners filled starts, counted in sets of frames (-1 when it can't fill them)
    public static int LakeIndex(int mask, int fills) => LakeOrder[(mask & 15) * 16 + (fills & 15)];

    private bool Ready => frameCount > 0 && frames != null && frames.Length >= 16 * frameCount;
    private bool LakeReady => Ready && lakeFrames != null && lakeFrames.Length >= LakeSets * frameCount;

    private int Mask(Vector3Int position, ITilemap tilemap)
    {
        int mask = 0;
        for (int i = 0; i < 4; i++)
            if (tilemap.GetTile(position + Sides[i]) == this) mask |= 1 << i;
        return mask;
    }

    private int Fills(Vector3Int position, ITilemap tilemap, int mask)
    {
        if (!LakeReady) return 0;
        int fillable = Fillable(mask), fills = 0;
        for (int c = 0; c < 4; c++)
            if ((fillable & (1 << c)) != 0 && tilemap.GetTile(position + Across[c]) == this) fills |= 1 << c;
        return fills;
    }

    // the cell's frames: its piece's, or its lake set's
    private void FramesAt(Vector3Int position, ITilemap tilemap, out Sprite[] source, out int start)
    {
        int mask = Mask(position, tilemap);
        int fills = Fills(position, tilemap, mask);
        int set = fills != 0 ? LakeIndex(mask, fills) : -1;
        source = set >= 0 ? lakeFrames : frames;
        start = (set >= 0 ? set : mask) * frameCount;
    }

    // a cell coming or going changes its water neighbours' pieces, the ones across its corners too
    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        tilemap.RefreshTile(position);
        for (int i = 0; i < Around.Length; i++)
        {
            Vector3Int next = position + Around[i];
            if (tilemap.GetTile(next) == this) tilemap.RefreshTile(next);
        }
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        if (Ready)
        {
            FramesAt(position, tilemap, out Sprite[] source, out int start);
            tileData.sprite = source[start];
        }
        else tileData.sprite = null;
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockTransform | TileFlags.LockColor;
        tileData.colliderType = colliderType;
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        if (!Ready) return false;
        FramesAt(position, tilemap, out Sprite[] source, out int start);
        Sprite[] piece = new Sprite[frameCount];
        System.Array.Copy(source, start, piece, 0, frameCount);
        tileAnimationData.animatedSprites = piece;
        tileAnimationData.animationSpeed = fps;
        tileAnimationData.animationStartTime = 0f;
        // on real time, like the game's other animated ground (the grass's sway, the old water): it ripples on while paused
        tileAnimationData.flags = TileAnimationFlags.UnscaledTime;
        return true;
    }
}
