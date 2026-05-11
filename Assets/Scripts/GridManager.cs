using UnityEngine;
using System.Collections.Generic;

// READ NOTE AT BOTTOM FOR PATHING

public class GridManager : MonoBehaviour
{

public int rows, columns;
public GameObject grassTilePrefab;
public List<Vector2Int> pathCoordinates;
public List<Vector2Int> dirtCoordinates, waterCoordinates, caveCoordinates;


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
    if (y == 0 || y == 1 || y == 11 || y == 12 || y == 13)
        return x >= 0 && x <= 18;

    return (x, y) switch
    {
        (1, 4) or (6, 10) or (10, 2) or (14, 8) or (15, 10) => true,
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