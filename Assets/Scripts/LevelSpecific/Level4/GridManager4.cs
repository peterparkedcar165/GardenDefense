using UnityEngine;
using System.Collections.Generic;

// READ NOTE AT BOTTOM FOR PATHING

public class GridManager4 : MonoBehaviour
{

public int rows, columns;
public GameObject grassTilePrefab;

private List<Vector2Int> pathCoordinates = new List<Vector2Int>
{
    // (0,3), (1,3)
    new Vector2Int(0,3), new Vector2Int(1,3),
    // (1,2)-(16,2)
    new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(3,2), new Vector2Int(4,2),
    new Vector2Int(5,2), new Vector2Int(6,2), new Vector2Int(7,2), new Vector2Int(8,2),
    new Vector2Int(9,2), new Vector2Int(10,2), new Vector2Int(11,2), new Vector2Int(12,2),
    new Vector2Int(13,2), new Vector2Int(14,2), new Vector2Int(15,2), new Vector2Int(16,2),
    // (16,3)
    new Vector2Int(16,3),
    // (1,4)-(16,4)
    new Vector2Int(1,4), new Vector2Int(2,4), new Vector2Int(3,4), new Vector2Int(4,4),
    new Vector2Int(5,4), new Vector2Int(6,4), new Vector2Int(7,4), new Vector2Int(8,4),
    new Vector2Int(9,4), new Vector2Int(10,4), new Vector2Int(11,4), new Vector2Int(12,4),
    new Vector2Int(13,4), new Vector2Int(14,4), new Vector2Int(15,4), new Vector2Int(16,4),
    // (1,5)-(16,5)
    new Vector2Int(1,5), new Vector2Int(2,5), new Vector2Int(3,5), new Vector2Int(4,5),
    new Vector2Int(5,5), new Vector2Int(6,5), new Vector2Int(7,5), new Vector2Int(8,5),
    new Vector2Int(9,5), new Vector2Int(10,5), new Vector2Int(11,5), new Vector2Int(12,5),
    new Vector2Int(13,5), new Vector2Int(14,5), new Vector2Int(15,5), new Vector2Int(16,5),
    // (16,6)
    new Vector2Int(16,6),
    // (2,7)-(16,7)
    new Vector2Int(2,7), new Vector2Int(3,7), new Vector2Int(4,7), new Vector2Int(5,7),
    new Vector2Int(6,7), new Vector2Int(7,7), new Vector2Int(8,7), new Vector2Int(9,7),
    new Vector2Int(10,7), new Vector2Int(11,7), new Vector2Int(12,7), new Vector2Int(13,7),
    new Vector2Int(14,7), new Vector2Int(15,7), new Vector2Int(16,7),
    // (2,8)
    new Vector2Int(2,8),
    // (2,9)-(15,9)
    new Vector2Int(2,9), new Vector2Int(3,9), new Vector2Int(4,9), new Vector2Int(5,9),
    new Vector2Int(6,9), new Vector2Int(7,9), new Vector2Int(8,9), new Vector2Int(9,9),
    new Vector2Int(10,9), new Vector2Int(11,9), new Vector2Int(12,9), new Vector2Int(13,9),
    new Vector2Int(14,9), new Vector2Int(15,9),
    // (15,10)
    new Vector2Int(15,10),
    // (5,11)-(15,11)
    new Vector2Int(5,11), new Vector2Int(6,11), new Vector2Int(7,11), new Vector2Int(8,11),
    new Vector2Int(9,11), new Vector2Int(10,11), new Vector2Int(11,11), new Vector2Int(12,11),
    new Vector2Int(13,11), new Vector2Int(14,11), new Vector2Int(15,11),
    // (5,12), (5,13)
    new Vector2Int(5,12), new Vector2Int(5,13)
};

private List<Vector2Int> dirtCoordinates = new List<Vector2Int>
{
    new Vector2Int(6,12), new Vector2Int(14,10), new Vector2Int(3,8),
    new Vector2Int(8,8), new Vector2Int(10,6), new Vector2Int(15,6),
    new Vector2Int(2,3), new Vector2Int(8,3), new Vector2Int(15,3)
};

private List<Vector2Int> waterCoordinates = new List<Vector2Int>
{
    new Vector2Int(11,10), new Vector2Int(6,6), new Vector2Int(11,3)
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
    if (y == 0) return true;
    if (x == 18) return true;
    if (y == 13 && x >= 7 && x <= 17) return true;
    if (x == 0 && y >= 11 && y <= 13) return true;
    if (x == 1 && y >= 11 && y <= 13) return true;

    return (x, y) switch
    {
        (2, 11) or (0, 8) or (16, 11) or (5, 6) or (12, 6) or (7, 3) => true,
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

/* Pathing:
the pathing is a bit weird,
for each level, in the GridManager inspector, you'll see Path Coordinates. for
EVERY SINGLE TILE that is a PATH or something else, you're gonna have to
enter that coordinate, BUT NOT THE WORLD COORDINATE
you will enter the tile relative to the bottom corner of the grid.
will add water later so we can plant aquatic plants, or maybe aquatic plants
around int */