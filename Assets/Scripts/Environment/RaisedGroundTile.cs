using UnityEngine;
using UnityEngine.Tilemaps;

// the cliff brush (Tommy, 2026-11-27): painted on HighgroundGround it raises the ground there, so the cliffs draw
// themselves around it (CliffBuilder). in a level it's ground like the clover brush (one of its variants per
// cell); in a tile palette (no level Ground map beside it) it shows its icon, a cliff, so it's easy to find
[CreateAssetMenu(fileName = "RaisedGroundTile", menuName = "Garden Defense/Raised Ground Tile")]
public class RaisedGroundTile : RandomVariantTile
{
    [Tooltip("what it shows in a tile palette")]
    public Sprite paletteIcon;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        if (paletteIcon == null || InLevel(tilemap))
        {
            base.GetTileData(position, tilemap, ref tileData);
            return;
        }
        tileData.sprite = paletteIcon;
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockColor;
        tileData.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        return (paletteIcon == null || InLevel(tilemap)) && base.GetTileAnimationData(position, tilemap, ref tileAnimationData);
    }

    // a level's tilemaps sit beside its Ground map; a palette's one tilemap doesn't
    private static bool InLevel(ITilemap map)
    {
        Tilemap self = map.GetComponent<Tilemap>();
        if (self == null || self.transform.parent == null) return true;
        Transform ground = self.transform.parent.Find("Ground");
        return ground != null && ground.GetComponent<Tilemap>() != null;
    }
}
