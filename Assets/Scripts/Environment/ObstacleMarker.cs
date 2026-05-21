using UnityEngine;

public class ObstacleMarker : MonoBehaviour
{
    private void Start()
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        foreach (Collider2D c in hits)
        {
            Tile t = c.GetComponent<Tile>();
            if (t != null)
            {
                t.tileType = TileType.Obstacle;
                break;
            }
        }
    }
}
