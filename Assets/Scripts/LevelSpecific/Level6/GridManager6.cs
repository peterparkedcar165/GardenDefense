using UnityEngine;
using System.Collections.Generic;

public class GridManager6 : MonoBehaviour
{
    public int rows, columns;
    public GameObject grassTilePrefab;

    private List<Vector2Int> pathCoordinates = new List<Vector2Int>
    {
        // 0,3-8,3
        new Vector2Int(0,3), new Vector2Int(1,3), new Vector2Int(2,3), new Vector2Int(3,3),
        new Vector2Int(4,3), new Vector2Int(5,3), new Vector2Int(6,3), new Vector2Int(7,3), new Vector2Int(8,3),
        // 8,2
        new Vector2Int(8,2),
        // 8,1
        new Vector2Int(8,1),
        // 7,1
        new Vector2Int(7,1),
        // 6,1-6,9 (skipping 6,3 and 6,8 — already included above or below)
        new Vector2Int(6,1), new Vector2Int(6,2), new Vector2Int(6,4), new Vector2Int(6,5),
        new Vector2Int(6,6), new Vector2Int(6,7), new Vector2Int(6,9),
        // 7,9
        new Vector2Int(7,9),
        // 8,9
        new Vector2Int(8,9),
        // 9,9-9,12
        new Vector2Int(9,9), new Vector2Int(9,10), new Vector2Int(9,11), new Vector2Int(9,12),
        // 5,12-8,12
        new Vector2Int(5,12), new Vector2Int(6,12), new Vector2Int(7,12), new Vector2Int(8,12),
        // 5,13-5,15
        new Vector2Int(5,13), new Vector2Int(5,14), new Vector2Int(5,15),
        // 1,15-4,15
        new Vector2Int(1,15), new Vector2Int(2,15), new Vector2Int(3,15), new Vector2Int(4,15),
        // 1,8-1,14
        new Vector2Int(1,8), new Vector2Int(1,9), new Vector2Int(1,10), new Vector2Int(1,11),
        new Vector2Int(1,12), new Vector2Int(1,13), new Vector2Int(1,14),
        // 2,8-11,8 (skipping 6,8 — already in 6,1-6,9)
        new Vector2Int(2,8), new Vector2Int(3,8), new Vector2Int(4,8), new Vector2Int(5,8),
        new Vector2Int(6,8), new Vector2Int(7,8), new Vector2Int(8,8), new Vector2Int(9,8),
        new Vector2Int(10,8), new Vector2Int(11,8),
        // 11,4-11,7
        new Vector2Int(11,4), new Vector2Int(11,5), new Vector2Int(11,6), new Vector2Int(11,7),
        // 12,4-15,4
        new Vector2Int(12,4), new Vector2Int(13,4), new Vector2Int(14,4), new Vector2Int(15,4),
        // 15,3
        new Vector2Int(15,3),
        // 15,2-20,2
        new Vector2Int(15,2), new Vector2Int(16,2), new Vector2Int(17,2), new Vector2Int(18,2),
        new Vector2Int(19,2), new Vector2Int(20,2),
        // 20,3-20,6
        new Vector2Int(20,3), new Vector2Int(20,4), new Vector2Int(20,5), new Vector2Int(20,6),
        // 16,6-19,6
        new Vector2Int(16,6), new Vector2Int(17,6), new Vector2Int(18,6), new Vector2Int(19,6),
        // 16,7-16,11
        new Vector2Int(16,7), new Vector2Int(16,8), new Vector2Int(16,9), new Vector2Int(16,10), new Vector2Int(16,11),
        // 15,11-15,16
        new Vector2Int(15,11), new Vector2Int(15,12), new Vector2Int(15,13), new Vector2Int(15,14),
        new Vector2Int(15,15), new Vector2Int(15,16),
        // 16,16-20,16
        new Vector2Int(16,16), new Vector2Int(17,16), new Vector2Int(18,16), new Vector2Int(19,16), new Vector2Int(20,16),
        // 20,13-20,15
        new Vector2Int(20,13), new Vector2Int(20,14), new Vector2Int(20,15),
        // 13,13-19,13 (skipping 15,13 — already in 15,11-15,16)
        new Vector2Int(13,13), new Vector2Int(14,13), new Vector2Int(16,13),
        new Vector2Int(17,13), new Vector2Int(18,13), new Vector2Int(19,13),
        // 13,10-13,13 (skipping 13,13 — already above)
        new Vector2Int(13,10), new Vector2Int(13,11), new Vector2Int(13,12),
        // 14,10-26,10 (skipping 16,10 — already in 16,7-16,11)
        new Vector2Int(14,10), new Vector2Int(15,10), new Vector2Int(17,10),
        new Vector2Int(18,10), new Vector2Int(19,10), new Vector2Int(20,10), new Vector2Int(21,10),
        new Vector2Int(22,10), new Vector2Int(23,10), new Vector2Int(24,10), new Vector2Int(25,10), new Vector2Int(26,10),
    };

    private List<Vector2Int> waterCoordinates = new List<Vector2Int>
    {
        // isolated
        new Vector2Int(4,6), new Vector2Int(15,5), new Vector2Int(3,14), new Vector2Int(17,15),
        // 0,14-0,19
        new Vector2Int(0,14), new Vector2Int(0,15), new Vector2Int(0,16), new Vector2Int(0,17),
        new Vector2Int(0,18), new Vector2Int(0,19),
        // 1,19-13,19
        new Vector2Int(1,19), new Vector2Int(2,19), new Vector2Int(3,19), new Vector2Int(4,19),
        new Vector2Int(5,19), new Vector2Int(6,19), new Vector2Int(7,19), new Vector2Int(8,19),
        new Vector2Int(9,19), new Vector2Int(10,19), new Vector2Int(11,19), new Vector2Int(12,19),
        new Vector2Int(13,19),
        // 7,18-12,18
        new Vector2Int(7,18), new Vector2Int(8,18), new Vector2Int(9,18), new Vector2Int(10,18),
        new Vector2Int(11,18), new Vector2Int(12,18),
        // 10,17-12,17
        new Vector2Int(10,17), new Vector2Int(11,17), new Vector2Int(12,17),
    };

    private List<Vector2Int> dirtCoordinates = new List<Vector2Int>
    {
        new Vector2Int(5,4), new Vector2Int(7,2), new Vector2Int(7,7), new Vector2Int(2,9),
        new Vector2Int(8,11), new Vector2Int(12,5), new Vector2Int(14,3), new Vector2Int(19,3),
        new Vector2Int(17,7), new Vector2Int(14,12), new Vector2Int(16,11), new Vector2Int(16,14),
        new Vector2Int(2,3), new Vector2Int(2,13), new Vector2Int(19,5), new Vector2Int(17,9),
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
        // Rectangular clumps
        if (x >= 0  && x <= 2  && y >= 0 && y <= 1)  return true; // 0,0-2,1
        if (x >= 11 && x <= 13 && y >= 0 && y <= 1)  return true; // 11,0-13,1
        if (x >= 18 && x <= 20 && y >= 0 && y <= 1)  return true; // 18,0-20,1
        if (x >= 24 && x <= 26 && y >= 0 && y <= 9)  return true; // 24,0-26,9
        if (x >= 15 && x <= 26 && y == 19)            return true; // 15,19-26,19
        if (x >= 24 && x <= 26 && y >= 14 && y <= 18) return true; // 24,14-26,18

        return (x, y) switch
        {
            (5,17) or (6,17) or (5,16) or (6,16) => true,
            (2,12) or (3,12) or (2,11) or (3,11) => true,
            (22,18) or (23,18) or (22,17) or (23,17) => true,
            (0,7) or (0,6) or (1,7) or (1,6) => true,
            (13,7) => true,
            (20,8) or (20,7) or (21,8) or (21,7) => true,
            (22,4) => true,
            (3,1) => true,
            (9,2) or (9,1) or (10,2) or (10,1) => true,
            (21,1) => true,
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
