using UnityEngine;

public class WaterZone : MonoBehaviour
{
    private void Start()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        // Each edge starts at the tile centre (0) and only extends toward water neighbours.
        // Non-neighbour sides stay at 0, so the collider lives entirely on the interior side.
        float minX = 0f, maxX = 0f;
        float minY = 0f, maxY = 0f;

        Vector3 p = transform.parent != null ? transform.parent.position : transform.position;

        if (Tile.allTiles.TryGetValue(Tile.TileKey(p + Vector3.right), out Tile r) && r.tileType == TileType.Water) maxX =  0.5f;
        if (Tile.allTiles.TryGetValue(Tile.TileKey(p + Vector3.left),  out Tile l) && l.tileType == TileType.Water) minX = -0.5f;
        if (Tile.allTiles.TryGetValue(Tile.TileKey(p + Vector3.up),    out Tile u) && u.tileType == TileType.Water) maxY =  0.5f;
        if (Tile.allTiles.TryGetValue(Tile.TileKey(p + Vector3.down),  out Tile d) && d.tileType == TileType.Water) minY = -0.5f;

        // Isolated tile with no water neighbours: fall back to a small centred trigger
        if (minX == 0f && maxX == 0f) { minX = -0.25f; maxX = 0.25f; }
        if (minY == 0f && maxY == 0f) { minY = -0.25f; maxY = 0.25f; }

        col.size   = new Vector2(maxX - minX, maxY - minY);
        col.offset = new Vector2((maxX + minX) * 0.5f, (maxY + minY) * 0.5f);
    }

    private void OnTriggerEnter2D(Collider2D other) => Check(other);
    private void OnTriggerStay2D(Collider2D other) => Check(other);

    private void Check(Collider2D other)
    {
        Insect insect = other.GetComponentInParent<Insect>();
        if (insect == null || insect.isFlying || insect.HasEffect<Airborne>()) return;
        if (insect.visual != null && insect.visual.localPosition.y > 0.4f) return;

        insect.Kill(insect.lastSource);
    }
}
