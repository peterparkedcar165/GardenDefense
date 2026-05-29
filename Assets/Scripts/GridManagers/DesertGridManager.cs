using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class DesertGridManager : MonoBehaviour
{
    public int rows, columns;
    public GameObject sandTilePrefab;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap pathTilemap;
    [SerializeField] private Tilemap colliderTilemap;
    [SerializeField] private Tilemap nonColliderGroundTilemap;
    [SerializeField] private Tilemap nonColliderAirTilemap;
    [SerializeField] private Tilemap waterTilemap;
    [SerializeField] private Tilemap highgroundGroundTilemap;
    [SerializeField] private Tilemap highgroundWaterTilemap;

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
                Vector3 worldPos = GridToWorld(x, y);
                GameObject tileGO = Instantiate(sandTilePrefab, worldPos, Quaternion.identity, transform);
                Tile t = tileGO.GetComponent<Tile>();
                tileMap[new Vector2Int(x, y)] = t;

                // all tilemaps share the same Grid, any one works for cell conversion
                Vector3Int cell = groundTilemap.WorldToCell(worldPos);

                bool hasCollider         = colliderTilemap         != null && colliderTilemap.HasTile(cell);
                bool hasPath             = pathTilemap             != null && pathTilemap.HasTile(cell);
                bool hasWater            = waterTilemap            != null && waterTilemap.HasTile(cell);
                bool hasHighgroundGround = highgroundGroundTilemap != null && highgroundGroundTilemap.HasTile(cell);
                bool hasHighgroundWater  = highgroundWaterTilemap  != null && highgroundWaterTilemap.HasTile(cell);

                // priority: collider > path > water > highground > ground (default sand)
                if      (hasCollider)         t.tileType = TileType.Obstacle;
                else if (hasPath)             t.tileType = TileType.Path;
                else if (hasWater)            t.tileType = TileType.Water;
                else if (hasHighgroundWater)  t.tileType = TileType.Water;
                else                          t.tileType = TileType.Sand;

                if (hasHighgroundGround || hasHighgroundWater) t.isHighground = true;

            }
        }

        // mark tiles adjacent to water
        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int( 1,  1), new Vector2Int(-1,  1),
            new Vector2Int( 1, -1), new Vector2Int(-1, -1)
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

    private Vector3 GridToWorld(int x, int y)
    {
        return new Vector3(x - (columns + 4) / 2f, y - (rows - 1) / 2f, 0) + transform.position;
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
                Vector3 pos = GridToWorld(x, y);
                Gizmos.DrawWireCube(pos, Vector3.one);
                UnityEditor.Handles.Label(pos + new Vector3(-0.4f, -0.2f, 0), $"({x},{y})", labelStyle);
            }
        }
    }
#endif
}
