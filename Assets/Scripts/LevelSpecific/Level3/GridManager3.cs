using UnityEngine;
using System.Collections.Generic;

// READ NOTE AT BOTTOM FOR PATHING

public class GridManager3 : MonoBehaviour
{

public int rows, columns;
public GameObject grassTilePrefab;

private List<Vector2Int> pathCoordinates = new List<Vector2Int>
{
    // top horizontal (2,12)-(10,12)
    new Vector2Int(2,12), new Vector2Int(3,12), new Vector2Int(4,12), new Vector2Int(5,12),
    new Vector2Int(6,12), new Vector2Int(7,12), new Vector2Int(8,12), new Vector2Int(9,12), new Vector2Int(10,12),
    // (10,11)
    new Vector2Int(10,11),
    // (10,10)-(12,10)
    new Vector2Int(10,10), new Vector2Int(11,10), new Vector2Int(12,10),
    // (12,11)
    new Vector2Int(12,11),
    // (12,12)-(18,12)
    new Vector2Int(12,12), new Vector2Int(13,12), new Vector2Int(14,12), new Vector2Int(15,12),
    new Vector2Int(16,12), new Vector2Int(17,12), new Vector2Int(18,12),
    // (2,9)-(2,11)
    new Vector2Int(2,9), new Vector2Int(2,10), new Vector2Int(2,11),
    // (3,9)
    new Vector2Int(3,9),
    // (4,3)-(4,9)
    new Vector2Int(4,3), new Vector2Int(4,4), new Vector2Int(4,5), new Vector2Int(4,6),
    new Vector2Int(4,7), new Vector2Int(4,8), new Vector2Int(4,9),
    // (5,3)-(11,3)
    new Vector2Int(5,3), new Vector2Int(6,3), new Vector2Int(7,3), new Vector2Int(8,3),
    new Vector2Int(9,3), new Vector2Int(10,3), new Vector2Int(11,3),
    // (11,4)-(15,4)
    new Vector2Int(11,4), new Vector2Int(12,4), new Vector2Int(13,4), new Vector2Int(14,4), new Vector2Int(15,4),
    // (15,5)-(18,5)
    new Vector2Int(15,5), new Vector2Int(16,5), new Vector2Int(17,5), new Vector2Int(18,5)
};

private List<Vector2Int> dirtCoordinates = new List<Vector2Int>
{
    new Vector2Int(3,8), new Vector2Int(3,10), new Vector2Int(5,4),
    new Vector2Int(9,11), new Vector2Int(13,11), new Vector2Int(14,5)
};

private List<Vector2Int> waterCoordinates = new List<Vector2Int>
{
    // left column x=0
    new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(0,3),
    new Vector2Int(0,4), new Vector2Int(0,5), new Vector2Int(0,6), new Vector2Int(0,7),
    new Vector2Int(0,8), new Vector2Int(0,9), new Vector2Int(0,10), new Vector2Int(0,11),
    new Vector2Int(0,12), new Vector2Int(0,13),
    // bottom row x=13-18
    new Vector2Int(13,0), new Vector2Int(14,0), new Vector2Int(15,0),
    new Vector2Int(16,0), new Vector2Int(17,0), new Vector2Int(18,0),
    // individual
    new Vector2Int(3,11), new Vector2Int(11,11),
    new Vector2Int(7,8), new Vector2Int(8,9),
    new Vector2Int(8,5), new Vector2Int(14,8)
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
    if (y == 0 && x >= 1 && x <= 12) return true;
    if (x == 18 && y >= 6 && y <= 11) return true;

    return (x, y) switch
    {
        (1, 13) or (2, 5) or (6, 10) or (8, 6) or (10, 8) or (14, 2) or (15, 7) => true,
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