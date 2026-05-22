using UnityEngine;
using System.Collections.Generic;

public class GridManager12 : MonoBehaviour
{
    public int rows, columns;
    public GameObject sandTilePrefab;

    private List<Vector2Int> pathCoordinates = new List<Vector2Int>
    {
        // Seg 1: (0,15)→(3,15)
        new Vector2Int(0,15), new Vector2Int(1,15), new Vector2Int(2,15), new Vector2Int(3,15),
        // Seg 2: (3,15)→(3,16)
        new Vector2Int(3,16),
        // Seg 3: (3,16)→(4,16)
        new Vector2Int(4,16),
        // Seg 4: (4,16)→(4,17)
        new Vector2Int(4,17),
        // Seg 5: (4,17)→(7,17)
        new Vector2Int(5,17), new Vector2Int(6,17), new Vector2Int(7,17),
        // Seg 6: (7,17)→(7,16)
        new Vector2Int(7,16),
        // Seg 7: (7,16)→(8,16)
        new Vector2Int(8,16),
        // Seg 8: (8,16)→(8,13)
        new Vector2Int(8,15), new Vector2Int(8,14), new Vector2Int(8,13),
        // Seg 9: (8,13)→(7,13)
        new Vector2Int(7,13),
        // Seg 10: (7,13)→(7,12)
        new Vector2Int(7,12),
        // Seg 11: (7,12)→(4,12)
        new Vector2Int(6,12), new Vector2Int(5,12), new Vector2Int(4,12),
        // Seg 12: (4,12)→(4,11)
        new Vector2Int(4,11),
        // Seg 13: (4,11)→(3,11)
        new Vector2Int(3,11),
        // Seg 14: (3,11)→(3,10)
        new Vector2Int(3,10),
        // Seg 15: (3,10)→(2,10)
        new Vector2Int(2,10),
        // Seg 16: (2,10)→(2,9)
        new Vector2Int(2,9),
        // Seg 17: (2,9)→(1,9)
        new Vector2Int(1,9),
        // Seg 18: (1,9)→(1,5)
        new Vector2Int(1,8), new Vector2Int(1,7), new Vector2Int(1,6), new Vector2Int(1,5),
        // Seg 19: (1,5)→(2,5)
        new Vector2Int(2,5),
        // Seg 20: (2,5)→(2,4)
        new Vector2Int(2,4),
        // Seg 21: (2,4)→(3,4)
        new Vector2Int(3,4),
        // Seg 22: (3,4)→(3,3)
        new Vector2Int(3,3),
        // Seg 23: (3,3)→(8,3)
        new Vector2Int(4,3), new Vector2Int(5,3), new Vector2Int(6,3), new Vector2Int(7,3), new Vector2Int(8,3),
        // Seg 24: (8,3)→(8,5)
        new Vector2Int(8,4), new Vector2Int(8,5),
        // Seg 25: (8,5)→(10,5)
        new Vector2Int(9,5), new Vector2Int(10,5),
        // Seg 26: (10,5)→(10,7)
        new Vector2Int(10,6), new Vector2Int(10,7),
        // Seg 27: (10,7)→(7,7)
        new Vector2Int(9,7), new Vector2Int(8,7), new Vector2Int(7,7),
        // Seg 28: (7,7)→(7,10)
        new Vector2Int(7,8), new Vector2Int(7,9), new Vector2Int(7,10),
        // Seg 29: (7,10)→(12,10)
        new Vector2Int(8,10), new Vector2Int(9,10), new Vector2Int(10,10), new Vector2Int(11,10), new Vector2Int(12,10),
        // Seg 30: (12,10)→(12,9)
        new Vector2Int(12,9),
        // Seg 31: (12,9)→(13,9)
        new Vector2Int(13,9),
        // Seg 32: (13,9)→(13,8)
        new Vector2Int(13,8),
        // Seg 33: (13,8)→(14,9) diagonal — endpoints only
        new Vector2Int(14,9),
        // Seg 34: (14,9)→(15,9)
        new Vector2Int(15,9),
        // Seg 35: (15,9)→(15,13)
        new Vector2Int(15,10), new Vector2Int(15,11), new Vector2Int(15,12), new Vector2Int(15,13),
        // Seg 36: (15,13)→(14,13)
        new Vector2Int(14,13),
        // Seg 37: (14,13)→(14,17)
        new Vector2Int(14,14), new Vector2Int(14,15), new Vector2Int(14,16), new Vector2Int(14,17),
        // Seg 38: (14,17)→(19,17)
        new Vector2Int(15,17), new Vector2Int(16,17), new Vector2Int(17,17), new Vector2Int(18,17), new Vector2Int(19,17),
        // Seg 39: (19,17)→(19,15)
        new Vector2Int(19,16), new Vector2Int(19,15),
        // Seg 40: (19,15)→(20,15)
        new Vector2Int(20,15),
        // Seg 41: (20,15)→(20,10)
        new Vector2Int(20,14), new Vector2Int(20,13), new Vector2Int(20,12), new Vector2Int(20,11), new Vector2Int(20,10),
        // Seg 42: (20,10)→(19,10)
        new Vector2Int(19,10),
        // Seg 43: (19,10)→(19,8)
        new Vector2Int(19,9), new Vector2Int(19,8),
        // Seg 44: (19,8)→(18,8)
        new Vector2Int(18,8),
        // Seg 45: (18,8)→(18,7)
        new Vector2Int(18,7),
        // Seg 46: (18,7)→(17,7)
        new Vector2Int(17,7),
        // Seg 47: (17,7)→(17,6)
        new Vector2Int(17,6),
        // Seg 48: (17,6)→(16,6)
        new Vector2Int(16,6),
        // Seg 49: (16,6)→(16,2)
        new Vector2Int(16,5), new Vector2Int(16,4), new Vector2Int(16,3), new Vector2Int(16,2),
        // Seg 50: (16,2)→(20,2)
        new Vector2Int(17,2), new Vector2Int(18,2), new Vector2Int(19,2), new Vector2Int(20,2),
        // Seg 51: (20,2)→(20,4)
        new Vector2Int(20,3), new Vector2Int(20,4),
        // Seg 52: (20,4)→(23,4)
        new Vector2Int(21,4), new Vector2Int(22,4), new Vector2Int(23,4),
        // Seg 53: (23,4)→(23,5)
        new Vector2Int(23,5),
        // Seg 54: (23,5)→(25,5)
        new Vector2Int(24,5), new Vector2Int(25,5),
        // Seg 55: (25,5)→(25,4)
        new Vector2Int(25,4),
        // Seg 56: (25,4)→(26,4)
        new Vector2Int(26,4),
    };

    private List<Vector2Int> waterCoordinates;
    private List<Vector2Int> obstacleCoordinates;

    private List<Vector2Int> highgroundCoordinates = new List<Vector2Int>();

    private List<Vector2Int> caveCoordinates = new List<Vector2Int>();

    void Start()
    {
        InitObstacleCoordinates();
        InitWaterCoordinates();
        InitHighgroundCoordinates();
        GenerateGrid();
    }

    private void InitObstacleCoordinates()
    {
        obstacleCoordinates = new List<Vector2Int>();
        AddObstacleRange(0,19,  26,19);
        AddObstacleRange(23,15, 25,17);
        AddObstacleRange(23,8,  25,10);
        AddObstacleRange(12,3,  14,5);
        AddObstacleRange(0,0,   2,2);
        AddObstacleRange(9,12,  9,16);
        AddObstacleRange(13,12, 13,16);
        AddObstacleRange(9,12,  13,12);
        obstacleCoordinates.Add(new Vector2Int(6,0));
        obstacleCoordinates.Add(new Vector2Int(6,1));
        obstacleCoordinates.Add(new Vector2Int(12,0));
        obstacleCoordinates.Add(new Vector2Int(12,1));
    }

    private void InitWaterCoordinates()
    {
        waterCoordinates = new List<Vector2Int>();
        AddWaterRange(3,6, 5,7);
        AddWaterRange(4,8, 5,8);
        // Pool
        waterCoordinates.Add(new Vector2Int(10,13));
        waterCoordinates.Add(new Vector2Int(11,13));
        waterCoordinates.Add(new Vector2Int(12,13));
        waterCoordinates.Add(new Vector2Int(12,14));
        waterCoordinates.Add(new Vector2Int(12,15));
        waterCoordinates.Add(new Vector2Int(11,15));
        waterCoordinates.Add(new Vector2Int(10,15));
        waterCoordinates.Add(new Vector2Int(10,14));
    }

    private void InitHighgroundCoordinates()
    {
        AddHighgroundRange(10, 13, 12, 16);
        AddHighgroundRange(7, 0, 11, 1);
    }

    private void AddHighgroundRange(int x1, int y1, int x2, int y2)
    {
        for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
            for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
                highgroundCoordinates.Add(new Vector2Int(x, y));
    }

    private void AddObstacleRange(int x1, int y1, int x2, int y2)
    {
        for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
            for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
                obstacleCoordinates.Add(new Vector2Int(x, y));
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
                GameObject tile = Instantiate(sandTilePrefab, position, Quaternion.identity, transform);
                Tile t = tile.GetComponent<Tile>();
                tileMap[new Vector2Int(x, y)] = t;

                if (pathCoordinates.Contains(new Vector2Int(x, y)))
                    t.tileType = TileType.Path;
                else if (obstacleCoordinates.Contains(new Vector2Int(x, y)))
                    t.tileType = TileType.Obstacle;
                else if (waterCoordinates.Contains(new Vector2Int(x, y)))
                    t.tileType = TileType.Water;
                else if (caveCoordinates.Contains(new Vector2Int(x, y)))
                    t.tileType = TileType.Cave;
                else
                    t.tileType = TileType.Sand;

                if (highgroundCoordinates.Contains(new Vector2Int(x, y)))
                    t.isHighground = true;
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
        Gizmos.color = Color.blue;
        GUIStyle labelStyle = new GUIStyle();
        labelStyle.normal.textColor = Color.black;
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 pos = new Vector3(x - (columns + 4) / 2f, y - (rows - 1) / 2f, 0) + transform.position;
                Gizmos.DrawWireCube(pos, Vector3.one);
                UnityEditor.Handles.Label(pos + new Vector3(-0.4f, -0.2f, 0), $"({x},{y})", labelStyle);
            }
        }
    }
#endif
}
