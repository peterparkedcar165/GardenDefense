using UnityEngine;
using System.Collections.Generic;

public class GridManager8 : MonoBehaviour
{
    public int rows, columns;
    public GameObject grassTilePrefab;

    private List<Vector2Int> pathCoordinates = new List<Vector2Int>
    {
        // Seg 1: (0,17)→(8,17)
        new Vector2Int(0,17), new Vector2Int(1,17), new Vector2Int(2,17), new Vector2Int(3,17),
        new Vector2Int(4,17), new Vector2Int(5,17), new Vector2Int(6,17), new Vector2Int(7,17), new Vector2Int(8,17),
        // Seg 2: (8,17)→(8,12)
        new Vector2Int(8,16), new Vector2Int(8,15), new Vector2Int(8,14), new Vector2Int(8,13), new Vector2Int(8,12),
        // Seg 3: (8,12)→(5,12)
        new Vector2Int(7,12), new Vector2Int(6,12), new Vector2Int(5,12),
        // Seg 4: (5,12)→(5,15)
        new Vector2Int(5,13), new Vector2Int(5,14), new Vector2Int(5,15),
        // Seg 5: (5,15)→(13,15)
        new Vector2Int(6,15), new Vector2Int(7,15), new Vector2Int(8,15), new Vector2Int(9,15),
        new Vector2Int(10,15), new Vector2Int(11,15), new Vector2Int(12,15), new Vector2Int(13,15),
        // Seg 6: (13,15)→(13,7)
        new Vector2Int(13,14), new Vector2Int(13,13), new Vector2Int(13,12), new Vector2Int(13,11),
        new Vector2Int(13,10), new Vector2Int(13,9), new Vector2Int(13,8), new Vector2Int(13,7),
        // Seg 7: (13,7)→(17,7)
        new Vector2Int(14,7), new Vector2Int(15,7), new Vector2Int(16,7), new Vector2Int(17,7),
        // Seg 8: (17,7)→(17,10)
        new Vector2Int(17,8), new Vector2Int(17,9), new Vector2Int(17,10),
        // Seg 9: (17,10)→(8,10)
        new Vector2Int(16,10), new Vector2Int(15,10), new Vector2Int(14,10), new Vector2Int(13,10),
        new Vector2Int(12,10), new Vector2Int(11,10), new Vector2Int(10,10), new Vector2Int(9,10), new Vector2Int(8,10),
        // Seg 10: (8,10)→(8,8)
        new Vector2Int(8,9), new Vector2Int(8,8),
        // Seg 11: (8,8)→(5,8)
        new Vector2Int(7,8), new Vector2Int(6,8), new Vector2Int(5,8),
        // Seg 12: (5,8)→(5,6)
        new Vector2Int(5,7), new Vector2Int(5,6),
        // Seg 13: (5,6)→(7,6)
        new Vector2Int(6,6), new Vector2Int(7,6),
        // Seg 14: (7,6)→(7,3)
        new Vector2Int(7,5), new Vector2Int(7,4), new Vector2Int(7,3),
        // Seg 15: (7,3)→(11,3)
        new Vector2Int(8,3), new Vector2Int(9,3), new Vector2Int(10,3), new Vector2Int(11,3),
        // Seg 16: (11,3)→(11,5)
        new Vector2Int(11,4), new Vector2Int(11,5),
        // Seg 17: (11,5)→(13,5)
        new Vector2Int(12,5), new Vector2Int(13,5),
        // Seg 18: (13,5)→(13,2)
        new Vector2Int(13,4), new Vector2Int(13,3), new Vector2Int(13,2),
        // Seg 19: (13,2)→(18,2)
        new Vector2Int(14,2), new Vector2Int(15,2), new Vector2Int(16,2), new Vector2Int(17,2), new Vector2Int(18,2),
        // Seg 20: (18,2)→(18,5)
        new Vector2Int(18,3), new Vector2Int(18,4), new Vector2Int(18,5),
        // Seg 21: (18,5)→(21,5)
        new Vector2Int(19,5), new Vector2Int(20,5), new Vector2Int(21,5),
        // Seg 22: (21,5)→(21,12)
        new Vector2Int(21,6), new Vector2Int(21,7), new Vector2Int(21,8), new Vector2Int(21,9),
        new Vector2Int(21,10), new Vector2Int(21,11), new Vector2Int(21,12),
        // Seg 23: (21,12)→(22,12)
        new Vector2Int(22,12),
        // Seg 24: (22,12)→(22,13)
        new Vector2Int(22,13),
        // Seg 25: (22,13)→(26,13)
        new Vector2Int(23,13), new Vector2Int(24,13), new Vector2Int(25,13), new Vector2Int(26,13),
    };

    private List<Vector2Int> waterCoordinates = new List<Vector2Int>
    {
        new Vector2Int(7,1),
        new Vector2Int(7,2),
        new Vector2Int(7,7),
        new Vector2Int(6,13),
        new Vector2Int(12,13),
        new Vector2Int(15,9),
    };

    private List<Vector2Int> dirtCoordinates = new List<Vector2Int>
    {
        new Vector2Int(7,16),  new Vector2Int(7,14),  new Vector2Int(9,14),  new Vector2Int(12,14),
        new Vector2Int(7,9),   new Vector2Int(6,7),   new Vector2Int(10,4),  new Vector2Int(12,4),
        new Vector2Int(14,3),  new Vector2Int(14,9),  new Vector2Int(16,9),  new Vector2Int(20,6),
        new Vector2Int(12,9),  new Vector2Int(14,11), new Vector2Int(12,11), new Vector2Int(9,9),
        new Vector2Int(8,4),   new Vector2Int(7,13),  new Vector2Int(22,11),
        new Vector2Int(19,4),  new Vector2Int(17,3),
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
                    t.tileType = TileType.Obstacle;
                else if (pathCoordinates.Contains(new Vector2Int(x, y)))
                    t.tileType = TileType.Path;
                else if (dirtCoordinates.Contains(new Vector2Int(x, y)))
                    t.tileType = TileType.Dirt;
                else if (waterCoordinates.Contains(new Vector2Int(x, y)))
                    t.tileType = TileType.Water;
                else if (caveCoordinates.Contains(new Vector2Int(x, y)))
                    t.tileType = TileType.Cave;
                else
                    t.tileType = TileType.Grass;
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
        if (x == 7 && y == 1) return false; // water cell within obstacle zone
        if (x == 7 && y == 2) return false; // water cell within obstacle zone
        if (x >= 0  && x <= 2  && y >= 0  && y <= 16) return true; // 0,0-2,16
        if (x >= 0  && x <= 26 && y >= 18 && y <= 19) return true; // 0,18-26,19
        if (x >= 24 && x <= 26 && y >= 14 && y <= 17) return true; // 24,14-26,17
        if (x >= 24 && x <= 26 && y >= 0  && y <= 12) return true; // 24,0-26,12
        if (x >= 3  && x <= 23 && y >= 0  && y <= 1)  return true; // 3,0-23,1
        if (x >= 3  && x <= 11 && y == 2)             return true; // 3,2-11,2
        return false;
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
