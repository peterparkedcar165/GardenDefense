using UnityEngine;

// an obstacle tile's box (Tile.SpawnObstacleZone), on the Obstacle layer: insects can't be pushed into it and flyers
// don't land on it. blocksSight off for a LowCollider cell (Tommy, 2026-11-28): line of sight, shots and skills pass
// over it (LineOfSight skips it). once the cell's tile is known (Tile.ShapeObstacle) a boulder's or bush's box becomes
// its art's pixel outline (insect pushes), and art keeps the exact pixels for line of sight (LightMask)
public class ObstacleZone : MonoBehaviour
{
    public bool blocksSight = true;
    [System.NonSerialized] public PixelMask.Shape art;
    [System.NonSerialized] public Matrix4x4 artMatrix = Matrix4x4.identity;
    // a pine forest cell (PineForestTile), and whether a pine stands in it: a cell that only shows the tops of pines
    // standing below it holds nothing on its ground, so it stops no line of sight (LightMask, Heights.Tree)
    [System.NonSerialized] public bool forest;
    [System.NonSerialized] public bool pineStandsHere = true;

    private void OnEnable() => LightMask.MarkDirty();
    private void OnDisable() => LightMask.MarkDirty();
}
