using UnityEngine;
using System.Collections.Generic;

// READ NOTE AT BOTTOM FOR PATHING

public class GridManager2 : MonoBehaviour
{

public int rows, columns;
public GameObject grassTilePrefab;

private List<Vector2Int> pathCoordinates = new List<Vector2Int>
{
    new Vector2Int(2,13), new Vector2Int(2,12),
    new Vector2Int(3,12), new Vector2Int(4,12), new Vector2Int(4,11),
    new Vector2Int(3,7), new Vector2Int(3,8), new Vector2Int(3,9), new Vector2Int(3,10), new Vector2Int(3,11),
    new Vector2Int(4,7), new Vector2Int(5,7), new Vector2Int(6,7), new Vector2Int(7,7),
    new Vector2Int(7,8), new Vector2Int(8,8), new Vector2Int(9,8), new Vector2Int(10,8), new Vector2Int(11,8), new Vector2Int(12,8),
    new Vector2Int(12,7), new Vector2Int(12,6), new Vector2Int(12,5),
    new Vector2Int(9,5), new Vector2Int(10,5), new Vector2Int(11,5),
    new Vector2Int(9,2), new Vector2Int(9,3), new Vector2Int(9,4),
    new Vector2Int(10,2), new Vector2Int(11,2), new Vector2Int(12,2), new Vector2Int(13,2), new Vector2Int(14,2), new Vector2Int(15,2),
    new Vector2Int(15,1), new Vector2Int(15,0)
};

private List<Vector2Int> dirtCoordinates = new List<Vector2Int>
{
    new Vector2Int(2,11), new Vector2Int(4,10), new Vector2Int(4,8),
    new Vector2Int(8,7), new Vector2Int(10,4), new Vector2Int(14,1)
};

private List<Vector2Int> waterCoordinates = new List<Vector2Int>
{
    new Vector2Int(7,10), new Vector2Int(7,5), new Vector2Int(14,4)
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
    if (x == 0) return true;
    if (x == 17 || x == 18) return true;
    if (x == 1 && y <= 7) return true;
    if (y == 0 && x >= 2 && x <= 13) return true;
    if (x == 16 && y >= 3) return true;
    if (y == 12 && x >= 7 && x <= 15) return true;
    if (y == 13 && x >= 7 && x <= 15) return true;

    return (x, y) switch
    {
        (8, 11) or (14, 9) or (5, 4) => true,
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