using UnityEngine;
using System.Collections.Generic;

public class GridManager7 : MonoBehaviour
{
    public int rows, columns;
    public GameObject grassTilePrefab;

    private List<Vector2Int> pathCoordinates = new List<Vector2Int>
    {
        // 0,10-2,10
        new Vector2Int(0,10), new Vector2Int(1,10), new Vector2Int(2,10),
        // 2,11
        new Vector2Int(2,11),
        // 2,12-6,12
        new Vector2Int(2,12), new Vector2Int(3,12), new Vector2Int(4,12), new Vector2Int(5,12), new Vector2Int(6,12),
        // 6,6-6,11
        new Vector2Int(6,6), new Vector2Int(6,7), new Vector2Int(6,8), new Vector2Int(6,9), new Vector2Int(6,10), new Vector2Int(6,11),
        // 5,6 / 4,6 / 4,7
        new Vector2Int(5,6), new Vector2Int(4,6), new Vector2Int(4,7),
        // 4,8-11,8
        new Vector2Int(4,8), new Vector2Int(5,8), new Vector2Int(6,8), new Vector2Int(7,8),
        new Vector2Int(8,8), new Vector2Int(9,8), new Vector2Int(10,8), new Vector2Int(11,8),
        // 11,9-11,12
        new Vector2Int(11,9), new Vector2Int(11,10), new Vector2Int(11,11), new Vector2Int(11,12),
        // 12,12-15,12
        new Vector2Int(12,12), new Vector2Int(13,12), new Vector2Int(14,12), new Vector2Int(15,12),
        // 15,5-15,11
        new Vector2Int(15,5), new Vector2Int(15,6), new Vector2Int(15,7), new Vector2Int(15,8),
        new Vector2Int(15,9), new Vector2Int(15,10), new Vector2Int(15,11),
        // 9,5-14,5
        new Vector2Int(9,5), new Vector2Int(10,5), new Vector2Int(11,5), new Vector2Int(12,5),
        new Vector2Int(13,5), new Vector2Int(14,5),
        // 9,6-9,14
        new Vector2Int(9,6), new Vector2Int(9,7), new Vector2Int(9,8), new Vector2Int(9,9),
        new Vector2Int(9,10), new Vector2Int(9,11), new Vector2Int(9,12), new Vector2Int(9,13), new Vector2Int(9,14),
        // 10,14-17,14
        new Vector2Int(10,14), new Vector2Int(11,14), new Vector2Int(12,14), new Vector2Int(13,14),
        new Vector2Int(14,14), new Vector2Int(15,14), new Vector2Int(16,14), new Vector2Int(17,14),
        // 17,2-17,13
        new Vector2Int(17,2), new Vector2Int(17,3), new Vector2Int(17,4), new Vector2Int(17,5),
        new Vector2Int(17,6), new Vector2Int(17,7), new Vector2Int(17,8), new Vector2Int(17,9),
        new Vector2Int(17,10), new Vector2Int(17,11), new Vector2Int(17,12), new Vector2Int(17,13),
        // 18,2-22,2
        new Vector2Int(18,2), new Vector2Int(19,2), new Vector2Int(20,2), new Vector2Int(21,2), new Vector2Int(22,2),
        // 22,3-22,6
        new Vector2Int(22,3), new Vector2Int(22,4), new Vector2Int(22,5), new Vector2Int(22,6),
        // 19,6-21,6
        new Vector2Int(19,6), new Vector2Int(20,6), new Vector2Int(21,6),
        // 19,7-19,9
        new Vector2Int(19,7), new Vector2Int(19,8), new Vector2Int(19,9),
        // 20,9-22,9
        new Vector2Int(20,9), new Vector2Int(21,9), new Vector2Int(22,9),
        // 22,10-22,12
        new Vector2Int(22,10), new Vector2Int(22,11), new Vector2Int(22,12),
        // 19,12-21,12
        new Vector2Int(19,12), new Vector2Int(20,12), new Vector2Int(21,12),
        // 19,13-19,16
        new Vector2Int(19,13), new Vector2Int(19,14), new Vector2Int(19,15), new Vector2Int(19,16),
        // 20,16-26,16
        new Vector2Int(20,16), new Vector2Int(21,16), new Vector2Int(22,16), new Vector2Int(23,16),
        new Vector2Int(24,16), new Vector2Int(25,16), new Vector2Int(26,16),
    };

    private List<Vector2Int> waterCoordinates = new List<Vector2Int>
    {
        // 0,19-26,19
        new Vector2Int(0,19), new Vector2Int(1,19), new Vector2Int(2,19), new Vector2Int(3,19),
        new Vector2Int(4,19), new Vector2Int(5,19), new Vector2Int(6,19), new Vector2Int(7,19),
        new Vector2Int(8,19), new Vector2Int(9,19), new Vector2Int(10,19), new Vector2Int(11,19),
        new Vector2Int(12,19), new Vector2Int(13,19), new Vector2Int(14,19), new Vector2Int(15,19),
        new Vector2Int(16,19), new Vector2Int(17,19), new Vector2Int(18,19), new Vector2Int(19,19),
        new Vector2Int(20,19), new Vector2Int(21,19), new Vector2Int(22,19), new Vector2Int(23,19),
        new Vector2Int(24,19), new Vector2Int(25,19), new Vector2Int(26,19),
        // 0,18-11,18
        new Vector2Int(0,18), new Vector2Int(1,18), new Vector2Int(2,18), new Vector2Int(3,18),
        new Vector2Int(4,18), new Vector2Int(5,18), new Vector2Int(6,18), new Vector2Int(7,18),
        new Vector2Int(8,18), new Vector2Int(9,18), new Vector2Int(10,18), new Vector2Int(11,18),
        // 0,17-8,17
        new Vector2Int(0,17), new Vector2Int(1,17), new Vector2Int(2,17), new Vector2Int(3,17),
        new Vector2Int(4,17), new Vector2Int(5,17), new Vector2Int(6,17), new Vector2Int(7,17),
        new Vector2Int(8,17),
        // 0,16-5,16
        new Vector2Int(0,16), new Vector2Int(1,16), new Vector2Int(2,16),
        new Vector2Int(3,16), new Vector2Int(4,16), new Vector2Int(5,16),
        // 0,15-2,15
        new Vector2Int(0,15), new Vector2Int(1,15), new Vector2Int(2,15),
        // isolated
        new Vector2Int(4,10), new Vector2Int(20,10),
        // 12,11-14,11
        new Vector2Int(12,11), new Vector2Int(13,11), new Vector2Int(14,11),
        // isolated
        new Vector2Int(12,10), new Vector2Int(14,10),
        // 12,9-14,9
        new Vector2Int(12,9), new Vector2Int(13,9), new Vector2Int(14,9),
        // 11,3-13,7 (rectangle)
        new Vector2Int(11,3), new Vector2Int(12,3), new Vector2Int(13,3),
        new Vector2Int(11,4), new Vector2Int(12,4), new Vector2Int(13,4),
        new Vector2Int(11,5), new Vector2Int(12,5), new Vector2Int(13,5),
        new Vector2Int(11,6), new Vector2Int(12,6), new Vector2Int(13,6),
        new Vector2Int(11,7), new Vector2Int(12,7), new Vector2Int(13,7),
    };

    private List<Vector2Int> dirtCoordinates = new List<Vector2Int>
    {
        new Vector2Int(3,11), new Vector2Int(5,9), new Vector2Int(5,7), new Vector2Int(8,9),
        new Vector2Int(10,6), new Vector2Int(14,6), new Vector2Int(12,8), new Vector2Int(13,10),
        new Vector2Int(10,13), new Vector2Int(16,13), new Vector2Int(18,3), new Vector2Int(21,3),
        new Vector2Int(20,8), new Vector2Int(21,11), new Vector2Int(20,15), new Vector2Int(21,18),
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
        if (x >= 0  && x <= 5  && y >= 0 && y <= 5)  return true; // 0,0-5,5
        if (x >= 6  && x <= 8  && y >= 0 && y <= 4)  return true; // 6,0-8,4
        if (x >= 9  && x <= 11 && y >= 0 && y <= 2)  return true; // 9,0-11,2
        if (x >= 12 && x <= 26 && y >= 0 && y <= 1)  return true; // 12,0-26,1

        return (x, y) switch
        {
            (7,14) => true,
            (8,11) => true,
            (2,8)  => true,
            (14,8) => true,
            (18,11) => true,
            (23,13) => true,
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
