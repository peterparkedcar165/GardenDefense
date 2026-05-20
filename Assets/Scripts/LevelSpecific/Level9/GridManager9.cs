using UnityEngine;
using System.Collections.Generic;

public class GridManager9 : MonoBehaviour
{
    public int rows, columns;
    public GameObject grassTilePrefab;

    private List<Vector2Int> pathCoordinates = new List<Vector2Int>
    {
        // Seg 1: (0,15)→(4,15)
        new Vector2Int(0,15), new Vector2Int(1,15), new Vector2Int(2,15), new Vector2Int(3,15), new Vector2Int(4,15),
        // Seg 2: (4,15)→(4,10)
        new Vector2Int(4,14), new Vector2Int(4,13), new Vector2Int(4,12), new Vector2Int(4,11), new Vector2Int(4,10),
        // Seg 3: (4,10)→(1,10)
        new Vector2Int(3,10), new Vector2Int(2,10), new Vector2Int(1,10),
        // Seg 4: (1,10)→(1,7)
        new Vector2Int(1,9), new Vector2Int(1,8), new Vector2Int(1,7),
        // Seg 5: (1,7)→(6,7)
        new Vector2Int(2,7), new Vector2Int(3,7), new Vector2Int(4,7), new Vector2Int(5,7), new Vector2Int(6,7),
        // Seg 6: (6,7)→(6,12)
        new Vector2Int(6,8), new Vector2Int(6,9), new Vector2Int(6,10), new Vector2Int(6,11), new Vector2Int(6,12),
        // Seg 7: (6,12)→(8,12)
        new Vector2Int(7,12), new Vector2Int(8,12),
        // Seg 8: (8,12)→(8,4)
        new Vector2Int(8,11), new Vector2Int(8,10), new Vector2Int(8,9), new Vector2Int(8,8),
        new Vector2Int(8,7), new Vector2Int(8,6), new Vector2Int(8,5), new Vector2Int(8,4),
        // Seg 9: (8,4)→(11,4)
        new Vector2Int(9,4), new Vector2Int(10,4), new Vector2Int(11,4),
        // Seg 10: (11,4)→(11,16)
        new Vector2Int(11,5), new Vector2Int(11,6), new Vector2Int(11,7), new Vector2Int(11,8),
        new Vector2Int(11,9), new Vector2Int(11,10), new Vector2Int(11,11), new Vector2Int(11,12),
        new Vector2Int(11,13), new Vector2Int(11,14), new Vector2Int(11,15), new Vector2Int(11,16),
        // Seg 11: (11,16)→(15,16)
        new Vector2Int(12,16), new Vector2Int(13,16), new Vector2Int(14,16), new Vector2Int(15,16),
        // Seg 12: (15,16)→(15,3)
        new Vector2Int(15,15), new Vector2Int(15,14), new Vector2Int(15,13), new Vector2Int(15,12),
        new Vector2Int(15,11), new Vector2Int(15,10), new Vector2Int(15,9), new Vector2Int(15,8),
        new Vector2Int(15,7), new Vector2Int(15,6), new Vector2Int(15,5), new Vector2Int(15,4), new Vector2Int(15,3),
        // Seg 13: (15,3)→(19,3)
        new Vector2Int(16,3), new Vector2Int(17,3), new Vector2Int(18,3), new Vector2Int(19,3),
        // Seg 14: (19,3)→(19,14)
        new Vector2Int(19,4), new Vector2Int(19,5), new Vector2Int(19,6), new Vector2Int(19,7),
        new Vector2Int(19,8), new Vector2Int(19,9), new Vector2Int(19,10), new Vector2Int(19,11),
        new Vector2Int(19,12), new Vector2Int(19,13), new Vector2Int(19,14),
        // Seg 15: (19,14)→(22,14)
        new Vector2Int(20,14), new Vector2Int(21,14), new Vector2Int(22,14),
        // Seg 16: (22,14)→(22,7)
        new Vector2Int(22,13), new Vector2Int(22,12), new Vector2Int(22,11), new Vector2Int(22,10),
        new Vector2Int(22,9), new Vector2Int(22,8), new Vector2Int(22,7),
        // Seg 17: (22,7)→(26,7)
        new Vector2Int(23,7), new Vector2Int(24,7), new Vector2Int(25,7), new Vector2Int(26,7),
    };

    private List<Vector2Int> waterCoordinates;

    private List<Vector2Int> dirtCoordinates = new List<Vector2Int>
    {
        new Vector2Int(2,8),   new Vector2Int(3,11),  new Vector2Int(5,12),  new Vector2Int(7,11),
        new Vector2Int(9,5),   new Vector2Int(13,6),  new Vector2Int(14,8),  new Vector2Int(13,15),
        new Vector2Int(17,7),  new Vector2Int(18,4),  new Vector2Int(20,13), new Vector2Int(21,10),
        new Vector2Int(23,8),  new Vector2Int(5,8),   new Vector2Int(10,9),
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
        // Ranges
        AddWaterRange(0,2,  4,2);
        AddWaterRange(22,2, 26,2);
        AddWaterRange(0,1,  9,1);
        AddWaterRange(17,1, 26,1);
        AddWaterRange(0,0,  26,0);
        AddWaterRange(0,19, 26,19);
        AddWaterRange(0,18, 9,18);
        AddWaterRange(17,18,26,18);
        AddWaterRange(0,17, 4,17);
        AddWaterRange(22,17,26,17);
        AddWaterRange(12,9, 14,14);
        // Individual
        waterCoordinates.Add(new Vector2Int(2,9));
        waterCoordinates.Add(new Vector2Int(1,13));
        waterCoordinates.Add(new Vector2Int(7,10));
        waterCoordinates.Add(new Vector2Int(5,6));
        waterCoordinates.Add(new Vector2Int(6,6));
        waterCoordinates.Add(new Vector2Int(5,5));
        waterCoordinates.Add(new Vector2Int(6,5));
        waterCoordinates.Add(new Vector2Int(12,5));
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
