using UnityEngine;
using System.Collections.Generic;

public class GridManager11 : MonoBehaviour
{
    public int rows, columns;
    public GameObject grassTilePrefab;

    private List<Vector2Int> pathCoordinates = new List<Vector2Int>
    {
        // Seg 1: (0,15)→(4,15)
        new Vector2Int(0,15), new Vector2Int(1,15), new Vector2Int(2,15), new Vector2Int(3,15), new Vector2Int(4,15),
        // Seg 2: (4,15)→(4,17)
        new Vector2Int(4,16), new Vector2Int(4,17),
        // Seg 3: (4,17)→(6,17)
        new Vector2Int(5,17), new Vector2Int(6,17),
        // Seg 4: (6,17)→(6,10)
        new Vector2Int(6,16), new Vector2Int(6,15), new Vector2Int(6,14), new Vector2Int(6,13),
        new Vector2Int(6,12), new Vector2Int(6,11), new Vector2Int(6,10),
        // Seg 5: (6,10)→(7,10)
        new Vector2Int(7,10),
        // Seg 6: (7,10)→(7,9)
        new Vector2Int(7,9),
        // Seg 7: (7,9)→(8,9)
        new Vector2Int(8,9),
        // Seg 8: (8,9)→(8,10)
        new Vector2Int(8,10),
        // Seg 9: (8,10)→(9,10)
        new Vector2Int(9,10),
        // Seg 10: (9,10)→(9,13)
        new Vector2Int(9,11), new Vector2Int(9,12), new Vector2Int(9,13),
        // Seg 11: (9,13)→(2,13)
        new Vector2Int(8,13), new Vector2Int(7,13), new Vector2Int(6,13), new Vector2Int(5,13),
        new Vector2Int(4,13), new Vector2Int(3,13), new Vector2Int(2,13),
        // Seg 12: (2,13)→(2,8)
        new Vector2Int(2,12), new Vector2Int(2,11), new Vector2Int(2,10), new Vector2Int(2,9), new Vector2Int(2,8),
        // Seg 13: (2,8)→(3,8)
        new Vector2Int(3,8),
        // Seg 14: (3,8)→(3,7)
        new Vector2Int(3,7),
        // Seg 15: (3,7)→(4,7)
        new Vector2Int(4,7),
        // Seg 16: (4,7)→(4,6)
        new Vector2Int(4,6),
        // Seg 17: (4,6)→(6,6)
        new Vector2Int(5,6), new Vector2Int(6,6),
        // Seg 18: (6,6)→(6,1)
        new Vector2Int(6,5), new Vector2Int(6,4), new Vector2Int(6,3), new Vector2Int(6,2), new Vector2Int(6,1),
        // Seg 19: (6,1)→(2,1)
        new Vector2Int(5,1), new Vector2Int(4,1), new Vector2Int(3,1), new Vector2Int(2,1),
        // Seg 20: (2,1)→(2,4)
        new Vector2Int(2,2), new Vector2Int(2,3), new Vector2Int(2,4),
        // Seg 21: (2,4)→(11,4)
        new Vector2Int(3,4), new Vector2Int(4,4), new Vector2Int(5,4), new Vector2Int(6,4),
        new Vector2Int(7,4), new Vector2Int(8,4), new Vector2Int(9,4), new Vector2Int(10,4), new Vector2Int(11,4),
        // Seg 22: (11,4)→(11,5)
        new Vector2Int(11,5),
        // Seg 23: (11,5)→(12,5)
        new Vector2Int(12,5),
        // Seg 24: (12,5)→(12,15)
        new Vector2Int(12,6), new Vector2Int(12,7), new Vector2Int(12,8), new Vector2Int(12,9), new Vector2Int(12,10),
        new Vector2Int(12,11), new Vector2Int(12,12), new Vector2Int(12,13), new Vector2Int(12,14), new Vector2Int(12,15),
        // Seg 25: (12,15)→(11,15)
        new Vector2Int(11,15),
        // Seg 26: (11,15)→(11,18)
        new Vector2Int(11,16), new Vector2Int(11,17), new Vector2Int(11,18),
        // Seg 27: (11,18)→(14,18)
        new Vector2Int(12,18), new Vector2Int(13,18), new Vector2Int(14,18),
        // Seg 28: (14,18)→(14,17)
        new Vector2Int(14,17),
        // Seg 29: (14,17)→(15,17)
        new Vector2Int(15,17),
        // Seg 30: (15,17)→(15,15)
        new Vector2Int(15,16), new Vector2Int(15,15),
        // Seg 31: (15,15)→(14,15)
        new Vector2Int(14,15),
        // Seg 32: (14,15)→(14,12)
        new Vector2Int(14,14), new Vector2Int(14,13), new Vector2Int(14,12),
        // Seg 33: (14,12)→(15,12)
        new Vector2Int(15,12),
        // Seg 34: (15,12)→(15,11)
        new Vector2Int(15,11),
        // Seg 35: (15,11)→(16,11)
        new Vector2Int(16,11),
        // Seg 36: (16,11)→(16,6)
        new Vector2Int(16,10), new Vector2Int(16,9), new Vector2Int(16,8), new Vector2Int(16,7), new Vector2Int(16,6),
        // Seg 37: (16,6)→(15,6)
        new Vector2Int(15,6),
        // Seg 38: (15,6)→(15,5)
        new Vector2Int(15,5),
        // Seg 39: (15,5)→(14,5)
        new Vector2Int(14,5),
        // Seg 40: (14,5)→(14,2)
        new Vector2Int(14,4), new Vector2Int(14,3), new Vector2Int(14,2),
        // Seg 41: (14,2)→(15,2)
        new Vector2Int(15,2),
        // Seg 42: (15,2)→(15,1)
        new Vector2Int(15,1),
        // Seg 43: (15,1)→(18,1)
        new Vector2Int(16,1), new Vector2Int(17,1), new Vector2Int(18,1),
        // Seg 44: (18,1)→(18,2)
        new Vector2Int(18,2),
        // Seg 45: (18,2)→(19,2)
        new Vector2Int(19,2),
        // Seg 46: (19,2)→(19,5)
        new Vector2Int(19,3), new Vector2Int(19,4), new Vector2Int(19,5),
        // Seg 47: (19,5)→(18,5)
        new Vector2Int(18,5),
        // Seg 48: (18,5)→(18,6)
        new Vector2Int(18,6),
        // Seg 49: (18,6)→(17,6)
        new Vector2Int(17,6),
        // Seg 50: (17,6)→(17,14)
        new Vector2Int(17,7), new Vector2Int(17,8), new Vector2Int(17,9), new Vector2Int(17,10),
        new Vector2Int(17,11), new Vector2Int(17,12), new Vector2Int(17,13), new Vector2Int(17,14),
        // Seg 51: (17,14)→(22,14)
        new Vector2Int(18,14), new Vector2Int(19,14), new Vector2Int(20,14), new Vector2Int(21,14), new Vector2Int(22,14),
        // Seg 52: (22,14)→(22,9)
        new Vector2Int(22,13), new Vector2Int(22,12), new Vector2Int(22,11), new Vector2Int(22,10), new Vector2Int(22,9),
        // Seg 53: (22,9)→(26,9)
        new Vector2Int(23,9), new Vector2Int(24,9), new Vector2Int(25,9), new Vector2Int(26,9),
    };

    private List<Vector2Int> waterCoordinates;

    private List<Vector2Int> dirtCoordinates = new List<Vector2Int>
    {
        new Vector2Int(5,16),  new Vector2Int(5,14),  new Vector2Int(3,12),  new Vector2Int(5,12),
        new Vector2Int(7,12),  new Vector2Int(8,11),  new Vector2Int(4,8),   new Vector2Int(5,7),
        new Vector2Int(5,5),   new Vector2Int(11,6),  new Vector2Int(3,3),   new Vector2Int(5,2),
        new Vector2Int(11,14), new Vector2Int(12,16), new Vector2Int(13,17), new Vector2Int(14,16),
        new Vector2Int(15,10), new Vector2Int(15,7),  new Vector2Int(15,4),  new Vector2Int(16,2),
        new Vector2Int(18,4),  new Vector2Int(17,5),  new Vector2Int(18,13), new Vector2Int(20,13),
        new Vector2Int(23,8),
    };

    private List<Vector2Int> caveCoordinates = new List<Vector2Int>();

    void Start()
    {
        InitWaterCoordinates();
        GenerateGrid();
    }

    private void InitWaterCoordinates()
    {
        waterCoordinates = new List<Vector2Int>();
        // Obstacles (borders)
        AddWaterRange(0,0,  26,0);
        AddWaterRange(26,0, 26,7);
        AddWaterRange(0,19, 26,19);
        // Water
        AddWaterRange(26,13, 26,18);
        // Individual
        waterCoordinates.Add(new Vector2Int(4,11));
        waterCoordinates.Add(new Vector2Int(4,3));
        waterCoordinates.Add(new Vector2Int(8,7));
        waterCoordinates.Add(new Vector2Int(7,11));
        waterCoordinates.Add(new Vector2Int(13,16));
        waterCoordinates.Add(new Vector2Int(17,4));
        waterCoordinates.Add(new Vector2Int(21,12));
    }

    private void AddWaterRange(int x1, int y1, int x2, int y2)
    {
        for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
            for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
                waterCoordinates.Add(new Vector2Int(x, y));
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

                if (pathCoordinates.Contains(new Vector2Int(x, y)))
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
