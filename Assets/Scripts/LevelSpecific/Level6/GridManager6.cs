using UnityEngine;
using System.Collections.Generic;

public class GridManager6 : MonoBehaviour
{

public int rows, columns;
public GameObject grassTilePrefab;

private List<Vector2Int> pathCoordinates = new List<Vector2Int>
{
    // (2,0)
    new Vector2Int(2,0),
    // (2,1)-(6,1)
    new Vector2Int(2,1), new Vector2Int(3,1), new Vector2Int(4,1), new Vector2Int(5,1), new Vector2Int(6,1),
    // (6,2)-(16,2)
    new Vector2Int(6,2), new Vector2Int(7,2), new Vector2Int(8,2), new Vector2Int(9,2), new Vector2Int(10,2),
    new Vector2Int(11,2), new Vector2Int(12,2), new Vector2Int(13,2), new Vector2Int(14,2), new Vector2Int(15,2), new Vector2Int(16,2),
    // (16,3)-(16,6)
    new Vector2Int(16,3), new Vector2Int(16,4), new Vector2Int(16,5), new Vector2Int(16,6),
    // (7,6)-(15,6)
    new Vector2Int(7,6), new Vector2Int(8,6), new Vector2Int(9,6), new Vector2Int(10,6), new Vector2Int(11,6),
    new Vector2Int(12,6), new Vector2Int(13,6), new Vector2Int(14,6), new Vector2Int(15,6),
    // (7,5)
    new Vector2Int(7,5),
    // (2,4)-(7,4)
    new Vector2Int(2,4), new Vector2Int(3,4), new Vector2Int(4,4), new Vector2Int(5,4), new Vector2Int(6,4), new Vector2Int(7,4),
    // (2,5)-(2,8)
    new Vector2Int(2,5), new Vector2Int(2,6), new Vector2Int(2,7), new Vector2Int(2,8),
    // (3,8)-(15,8)
    new Vector2Int(3,8), new Vector2Int(4,8), new Vector2Int(5,8), new Vector2Int(6,8), new Vector2Int(7,8),
    new Vector2Int(8,8), new Vector2Int(9,8), new Vector2Int(10,8), new Vector2Int(11,8), new Vector2Int(12,8),
    new Vector2Int(13,8), new Vector2Int(14,8), new Vector2Int(15,8),
    // (15,9)-(15,12)
    new Vector2Int(15,9), new Vector2Int(15,10), new Vector2Int(15,11), new Vector2Int(15,12),
    // (10,12)-(14,12)
    new Vector2Int(10,12), new Vector2Int(11,12), new Vector2Int(12,12), new Vector2Int(13,12), new Vector2Int(14,12),
    // (10,13)
    new Vector2Int(10,13)
};

private List<Vector2Int> dirtCoordinates = new List<Vector2Int>
{
    new Vector2Int(6,5), new Vector2Int(7,1), new Vector2Int(17,3), new Vector2Int(16,10),
    new Vector2Int(7,3), new Vector2Int(15,3)
};

private List<Vector2Int> waterCoordinates = new List<Vector2Int>
{
    // (0,0)-(0,3)
    new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(0,3),
    // (0,9)-(0,10)
    new Vector2Int(0,9), new Vector2Int(0,10),
    // (6,10)-(7,10)
    new Vector2Int(6,10), new Vector2Int(7,10),
    // (6,11)-(7,11)
    new Vector2Int(6,11), new Vector2Int(7,11),
    // (14,4)-(15,5)
    new Vector2Int(14,4), new Vector2Int(15,4), new Vector2Int(14,5), new Vector2Int(15,5),
    // (17,0)-(18,1)
    new Vector2Int(17,0), new Vector2Int(18,0), new Vector2Int(17,1), new Vector2Int(18,1),
    new Vector2Int(8,5)
};

private List<Vector2Int> caveCoordinates = new List<Vector2Int>();

void Start()
    {
        GenerateGrid();
    }

void GenerateGrid()
    {
        Dictionary<Vector2Int, Tile> tileMap = new Dictionary<Vector2Int, Tile>();

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 position = new Vector3(x - (columns + 4) / 2f, y - (rows - 1) / 2f, 0) + transform.position;
                GameObject tile = Instantiate(grassTilePrefab, position, Quaternion.identity, transform);
                Tile t = tile.GetComponent<Tile>();
                tileMap[new Vector2Int(x, y)] = t;

                if (IsObstacle(x, y))
                {
                    tile.GetComponent<Tile>().tileType = TileType.Obstacle;
                }
                else if (pathCoordinates.Contains(new Vector2Int(x, y)))
                {
                    tile.GetComponent<Tile>().tileType = TileType.Path;
                }
                else if (dirtCoordinates.Contains(new Vector2Int(x, y)))
                {
                    tile.GetComponent<Tile>().tileType = TileType.Dirt;
                }
                else if (waterCoordinates.Contains(new Vector2Int(x, y)))
                {
                    tile.GetComponent<Tile>().tileType = TileType.Water;
                }
                else if (caveCoordinates.Contains(new Vector2Int(x, y)))
                {
                    tile.GetComponent<Tile>().tileType = TileType.Cave;
                }
                else
                {
                    tile.GetComponent<Tile>().tileType = TileType.Grass;
                }
            }
        }

        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(-1, 1),
            new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };

        foreach (var kvp in tileMap)
        {
            foreach (var dir in directions)
            {
                if (tileMap.TryGetValue(kvp.Key + dir, out Tile neighbor) && neighbor.tileType == TileType.Water)
                {
                    kvp.Value.isWaterAdjacent = true;
                    break;
                }
            }
        }
    }

private bool IsObstacle(int x, int y)
{
    if (y == 13 && x >= 0 && x <= 9) return true;
    if (y == 12 && x >= 0 && x <= 9) return true;
    if (x == 18 && y >= 2 && y <= 13) return true;

    return (x, y) switch
    {
        (2, 11) or (5, 7) or (9, 3) or (14, 1) or (12, 10) or (17, 12) => true,
        _ => false
    };
}

#if UNITY_EDITOR
private void OnDrawGizmos()
{
    Gizmos.color = Color.green;
    for (int x = 0; x < columns; x++)
    {
        for (int y = 0; y < rows; y++)
        {
            Vector3 pos = new Vector3(x - (columns + 4) / 2f, y - (rows - 1) / 2f, 0) + transform.position;
            Gizmos.DrawWireCube(pos, Vector3.one);
            UnityEditor.Handles.Label(pos + new Vector3(-0.4f, -0.4f, 0), $"({x},{y})");
        }
    }
}
#endif

}
