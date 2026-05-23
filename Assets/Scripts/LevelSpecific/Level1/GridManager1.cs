using UnityEngine;
using System.Collections.Generic;

public class GridManager1 : MonoBehaviour
{

public int rows, columns;
public GameObject grassTilePrefab;

private List<Vector2Int> pathCoordinates = new List<Vector2Int>
{
    // (0,7)-(3,7)
    new Vector2Int(0,7), new Vector2Int(1,7), new Vector2Int(2,7), new Vector2Int(3,7),
    // (3,6)
    new Vector2Int(3,6),
    // (3,5)-(5,5)
    new Vector2Int(3,5), new Vector2Int(4,5), new Vector2Int(5,5),
    // (5,6)
    new Vector2Int(5,6),
    // (5,7)-(10,7)
    new Vector2Int(5,7), new Vector2Int(6,7), new Vector2Int(7,7), new Vector2Int(8,7),
    new Vector2Int(9,7), new Vector2Int(10,7),
    // (10,6)
    new Vector2Int(10,6),
    // (10,5)-(18,5)
    new Vector2Int(10,5), new Vector2Int(11,5), new Vector2Int(12,5), new Vector2Int(13,5),
    new Vector2Int(14,5), new Vector2Int(15,5), new Vector2Int(16,5), new Vector2Int(17,5),
    new Vector2Int(18,5)
};

private List<Vector2Int> dirtCoordinates = new List<Vector2Int>
{
    new Vector2Int(2,6), new Vector2Int(4,6), new Vector2Int(6,6),
    new Vector2Int(4,4), new Vector2Int(8,8), new Vector2Int(11,6),
    new Vector2Int(16,6), new Vector2Int(15,4)
};

private List<Vector2Int> waterCoordinates = new List<Vector2Int>
{
    new Vector2Int(5,3), new Vector2Int(13,3),
    new Vector2Int(8,5), new Vector2Int(12,9)
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
                else if (pathCoordinates.Contains(new Vector2Int(x,y)))
                {
                    tile.GetComponent<Tile>().tileType = TileType.Path;
                }
                else if (dirtCoordinates.Contains(new Vector2Int(x, y)))
                {
                    tile.GetComponent<Tile>().tileType = TileType.Dirt;

                } else if (waterCoordinates.Contains(new Vector2Int(x, y))) {
                    tile.GetComponent<Tile>().tileType = TileType.Water;

                } else if (caveCoordinates.Contains(new Vector2Int(x,y)))
                {
                    tile.GetComponent<Tile>().tileType = TileType.Cave;
                } else {
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
    if (y == 0 || y == 1) return true;
    if (y == 11 || y == 12 || y == 13) return true;

    return (x, y) switch
    {
        (10, 2) or (1, 4) or (6, 10) or (15, 10) => true,
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
