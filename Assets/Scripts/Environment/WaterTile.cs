using UnityEngine;
using UnityEngine.Tilemaps;

// streams and ponds that join themselves (Tommy, 2026-11-27: gd_ground_pack water): painted on a level's water map,
// every cell shows the piece for the sides its water goes on, up 1 + right 2 + down 4 + left 8 (0 a pond on its own,
// 5 straight up and down, 10 straight across, 15 a crossing), and plays its 16 frame ripple, every cell from the same
// moment so the joins stay seamless: frames[mask * frameCount + frame]. the pieces are see through outside the bank, so
// the grass under them shows. the pack's version stood on its own PathTile; this one stands alone, since the game's
// PathTile has its own map edge rules
[CreateAssetMenu(fileName = "WaterTile", menuName = "Garden Defense/Water Tile")]
public class WaterTile : TileBase
{
    [Tooltip("None: only drawn. Grid: the whole cell is solid. Sprite: only the water shape (both need a TilemapCollider2D on the map)")]
    public UnityEngine.Tilemaps.Tile.ColliderType colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
    [Tooltip("every piece's frames: piece 0's, then piece 1's, and so on (16 pieces times the frame count)")]
    public Sprite[] frames = new Sprite[0];
    public int frameCount = 16;
    [Tooltip("frames a second, times the map's Animation Frame Rate (keep that at 1). 8 is a 2 second loop; the sea tiles use the same")]
    public float fps = 8f;

    private static readonly Vector3Int[] Sides = { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };

    private bool Ready => frameCount > 0 && frames != null && frames.Length >= 16 * frameCount;

    private int Mask(Vector3Int position, ITilemap tilemap)
    {
        int mask = 0;
        for (int i = 0; i < 4; i++)
            if (tilemap.GetTile(position + Sides[i]) == this) mask |= 1 << i;
        return mask;
    }

    // a cell coming or going changes its water neighbours' pieces
    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        tilemap.RefreshTile(position);
        for (int i = 0; i < 4; i++)
        {
            Vector3Int next = position + Sides[i];
            if (tilemap.GetTile(next) == this) tilemap.RefreshTile(next);
        }
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = Ready ? frames[Mask(position, tilemap) * frameCount] : null;
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockTransform | TileFlags.LockColor;
        tileData.colliderType = colliderType;
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        if (!Ready) return false;
        Sprite[] piece = new Sprite[frameCount];
        System.Array.Copy(frames, Mask(position, tilemap) * frameCount, piece, 0, frameCount);
        tileAnimationData.animatedSprites = piece;
        tileAnimationData.animationSpeed = fps;
        tileAnimationData.animationStartTime = 0f;
        // on real time, like the game's other animated ground (the grass's sway, the old water): it ripples on while paused
        tileAnimationData.flags = TileAnimationFlags.UnscaledTime;
        return true;
    }
}
