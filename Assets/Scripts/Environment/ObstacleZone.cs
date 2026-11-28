using UnityEngine;

// an obstacle tile's box (Tile.SpawnObstacleZone), on the Obstacle layer: insects can't be pushed into it and flyers
// don't land on it. blocksSight off for a LowCollider cell (Tommy, 2026-11-28): line of sight, shots and skills pass
// over it (LineOfSight skips it)
public class ObstacleZone : MonoBehaviour
{
    public bool blocksSight = true;
}
