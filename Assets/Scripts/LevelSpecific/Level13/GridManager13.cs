using UnityEngine;
using System.Collections.Generic;

public class GridManager13 : MonoBehaviour
{
    public int rows, columns;
    public GameObject sandTilePrefab;

    private List<Vector2Int> pathCoordinates = new List<Vector2Int>
    {
        // Seg 1: (0,2)→(2,2)
        new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2),
        // Seg 2: (2,2)→(2,3)
        new Vector2Int(2,3),
        // Seg 3: (2,3)→(8,3)
        new Vector2Int(3,3), new Vector2Int(4,3), new Vector2Int(5,3), new Vector2Int(6,3), new Vector2Int(7,3), new Vector2Int(8,3),
        // Seg 4: (8,3)→(8,2)
        new Vector2Int(8,2),
        // Seg 5: (8,2)→(10,2)
        new Vector2Int(9,2), new Vector2Int(10,2),
        // Seg 6: (10,2)→(10,0)
        new Vector2Int(10,1), new Vector2Int(10,0),
        // Seg 7: (10,0)→(6,0)
        new Vector2Int(9,0), new Vector2Int(8,0), new Vector2Int(7,0), new Vector2Int(6,0),
        // Seg 8: (6,0)→(6,6)
        new Vector2Int(6,1), new Vector2Int(6,2), new Vector2Int(6,3), new Vector2Int(6,4), new Vector2Int(6,5), new Vector2Int(6,6),
        // Seg 9: (6,6)→(2,6)
        new Vector2Int(5,6), new Vector2Int(4,6), new Vector2Int(3,6), new Vector2Int(2,6),
        // Seg 10: (2,6)→(2,8)
        new Vector2Int(2,7), new Vector2Int(2,8),
        // Seg 11: (2,8)→(8,8)
        new Vector2Int(3,8), new Vector2Int(4,8), new Vector2Int(5,8), new Vector2Int(6,8), new Vector2Int(7,8), new Vector2Int(8,8),
        // Seg 12: (8,8)→(8,5)
        new Vector2Int(8,7), new Vector2Int(8,6), new Vector2Int(8,5),
        // Seg 13: (8,5)→(10,5)
        new Vector2Int(9,5), new Vector2Int(10,5),
        // Seg 14: (10,5)→(10,4)
        new Vector2Int(10,4),
        // Seg 15: (10,4)→(12,4)
        new Vector2Int(11,4), new Vector2Int(12,4),
        // Seg 16: (12,4)→(12,1)
        new Vector2Int(12,3), new Vector2Int(12,2), new Vector2Int(12,1),
        // Seg 17: (12,1)→(14,1)
        new Vector2Int(13,1), new Vector2Int(14,1),
        // Seg 18: (14,1)→(14,2)
        new Vector2Int(14,2),
        // Seg 19: (14,2)→(20,2)
        new Vector2Int(15,2), new Vector2Int(16,2), new Vector2Int(17,2), new Vector2Int(18,2), new Vector2Int(19,2), new Vector2Int(20,2),
        // Seg 20: (20,2)→(20,1)
        new Vector2Int(20,1),
        // Seg 21: (20,1)→(23,1)
        new Vector2Int(21,1), new Vector2Int(22,1), new Vector2Int(23,1),
        // Seg 22: (23,1)→(23,2)
        new Vector2Int(23,2),
        // Seg 23: (23,2)→(24,2)
        new Vector2Int(24,2),
        // Seg 24: (24,2)→(24,3)
        new Vector2Int(24,3),
        // Seg 25: (24,3)→(23,3)
        new Vector2Int(23,3),
        // Seg 26: (23,3)→(23,4)
        new Vector2Int(23,4),
        // Seg 27: (23,4)→(20,4)
        new Vector2Int(22,4), new Vector2Int(21,4), new Vector2Int(20,4),
        // Seg 28: (20,4)→(20,5)
        new Vector2Int(20,5),
        // Seg 29: (20,5)→(16,5)
        new Vector2Int(19,5), new Vector2Int(18,5), new Vector2Int(17,5), new Vector2Int(16,5),
        // Seg 30: (16,5)→(16,6)
        new Vector2Int(16,6),
        // Seg 31: (16,6)→(15,6)
        new Vector2Int(15,6),
        // Seg 32: (15,6)→(15,7)
        new Vector2Int(15,7),
        // Seg 33: (15,7)→(13,7)
        new Vector2Int(14,7), new Vector2Int(13,7),
        // Seg 34: (13,7)→(13,9)
        new Vector2Int(13,8), new Vector2Int(13,9),
        // Seg 35: (13,9)→(12,9)
        new Vector2Int(12,9),
        // Seg 36: (12,9)→(12,10)
        new Vector2Int(12,10),
        // Seg 37: (12,10)→(10,10)
        new Vector2Int(11,10), new Vector2Int(10,10),
        // Seg 38: (10,10)→(10,11)
        new Vector2Int(10,11),
        // Seg 39: (10,11)→(6,11)
        new Vector2Int(9,11), new Vector2Int(8,11), new Vector2Int(7,11), new Vector2Int(6,11),
        // Seg 40: (6,11)→(6,12)
        new Vector2Int(6,12),
        // Seg 41: (6,12)→(4,12)
        new Vector2Int(5,12), new Vector2Int(4,12),
        // Seg 42: (4,12)→(4,14)
        new Vector2Int(4,13), new Vector2Int(4,14),
        // Seg 43: (4,14)→(10,14)
        new Vector2Int(5,14), new Vector2Int(6,14), new Vector2Int(7,14), new Vector2Int(8,14), new Vector2Int(9,14), new Vector2Int(10,14),
        // Seg 44: (10,14)→(10,15)
        new Vector2Int(10,15),
        // Seg 45: (10,15)→(14,15)
        new Vector2Int(11,15), new Vector2Int(12,15), new Vector2Int(13,15), new Vector2Int(14,15),
        // Seg 46: (14,15)→(14,16)
        new Vector2Int(14,16),
        // Seg 47: (14,16)→(23,16)
        new Vector2Int(15,16), new Vector2Int(16,16), new Vector2Int(17,16), new Vector2Int(18,16),
        new Vector2Int(19,16), new Vector2Int(20,16), new Vector2Int(21,16), new Vector2Int(22,16), new Vector2Int(23,16),
        // Seg 48: (23,16)→(23,17)
        new Vector2Int(23,17),
        // Seg 49: (23,17)→(26,17)
        new Vector2Int(24,17), new Vector2Int(25,17), new Vector2Int(26,17),
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
        // L-shape: (0,9)→(1,9)→(1,11)
        AddObstacleRange(0,9,  1,9);
        AddObstacleRange(1,9,  1,11);
        AddObstacleRange(0,15, 2,17);
        AddObstacleRange(6,17, 8,19);
        AddObstacleRange(16,18, 18,19);
        AddObstacleRange(24,18, 26,19);
        AddObstacleRange(24,13, 26,15);
        AddObstacleRange(17,11, 19,13);
        AddObstacleRange(21,8,  23,10);
        // Rectangle: corners (13,3)-(15,5), excluding (14,4)-(14,5) which are sand highground
        AddObstacleRange(13,3, 15,3);
        AddObstacleRange(13,4, 13,5);
        AddObstacleRange(15,4, 15,5);
    }

    private void InitWaterCoordinates()
    {
        waterCoordinates = new List<Vector2Int>();
        AddWaterRange(0,19,  6,19);
        AddWaterRange(3,10,  4,11);
        AddWaterRange(16,0,  17,0);
        AddWaterRange(19,7,  20,8);
        AddWaterRange(15,10, 16,13);
        AddWaterRange(14,11, 14,12);
    }

    private void InitHighgroundCoordinates()
    {
        highgroundCoordinates.Add(new Vector2Int(0,10));
        highgroundCoordinates.Add(new Vector2Int(0,11));
        highgroundCoordinates.Add(new Vector2Int(14,4));
        highgroundCoordinates.Add(new Vector2Int(14,5));
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
