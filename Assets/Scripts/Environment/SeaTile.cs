using UnityEngine;
using UnityEngine.Tilemaps;

// one sea tile (Tommy, 2026-11-27: gd_ground_pack water): open water, or open water with a bank on one side (top,
// bottom, left or right), painted by hand for seas and coasts. it plays the same ripple as the WaterTile, in step with
// it (every cell starts at the same moment). SeaOpen, SeaBankTop, SeaBankBottom, SeaBankLeft and SeaBankRight are in
// dayFiles/Water/Sea. no corners yet, so a coast runs straight off the map
[CreateAssetMenu(fileName = "SeaTile", menuName = "Garden Defense/Sea Tile")]
public class SeaTile : TileBase
{
    [Tooltip("the frames in order; one frame is a still tile (open water)")]
    public Sprite[] frames = new Sprite[0];
    [Tooltip("frames a second, times the map's Animation Frame Rate. keep it the same as the WaterTile's")]
    public float fps = 8f;
    [Tooltip("None: only drawn. Grid: the whole cell is solid. Sprite: only the water shape (both need a TilemapCollider2D on the map)")]
    public UnityEngine.Tilemaps.Tile.ColliderType colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockTransform | TileFlags.LockColor;
        tileData.colliderType = colliderType;
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        if (frames == null || frames.Length < 2) return false;
        tileAnimationData.animatedSprites = frames;
        tileAnimationData.animationSpeed = fps;
        tileAnimationData.animationStartTime = 0f;
        // on real time, like the game's other animated ground (the grass's sway, the old water): it ripples on while paused
        tileAnimationData.flags = TileAnimationFlags.UnscaledTime;
        return true;
    }
}
