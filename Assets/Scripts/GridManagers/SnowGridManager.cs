using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class SnowGridManager : MonoBehaviour
{
    public int rows, columns;
    public GameObject snowTilePrefab;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap pathTilemap;
    [SerializeField] private Tilemap colliderTilemap;
    [SerializeField] private Tilemap nonColliderGroundTilemap;
    [SerializeField] private Tilemap nonColliderAirTilemap;
    [SerializeField] private Tilemap waterTilemap;
    [SerializeField] private Tilemap heatTilemap;
    [SerializeField] private Tilemap highgroundGroundTilemap;
    [SerializeField] private Tilemap highgroundWaterTilemap;
    [SerializeField] private Tilemap lowgroundGroundTilemap;
    [SerializeField] private Tilemap lowgroundWaterTilemap;
    [SerializeField] private Tilemap lightGroundTilemap;
    [SerializeField] private Tilemap lightAirTilemap;
    [SerializeField] private Tilemap lightColliderTilemap;

    [Header("Lights")]
    private float lightRadius = 3f;
    private float lightInnerRadius = 1.2f;
    private float lightIntensity   = 0.35f;
    private float lightFalloff     = 0.2f;

    void Start()
    {
        GenerateGrid();
        SpawnLightTilemapLights();
    }

    void GenerateGrid()
    {
        Dictionary<Vector2Int, Tile> tileMap = new Dictionary<Vector2Int, Tile>();

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 worldPos = GridToWorld(x, y);
                GameObject tileGO = Instantiate(snowTilePrefab, worldPos, Quaternion.identity, transform);
                Tile t = tileGO.GetComponent<Tile>();
                tileMap[new Vector2Int(x, y)] = t;

                Vector3Int cell = groundTilemap.WorldToCell(worldPos);

                bool hasCollider         = (colliderTilemap != null && colliderTilemap.HasTile(cell))
                                         || (lightColliderTilemap != null && lightColliderTilemap.HasTile(cell));
                bool hasPath             = pathTilemap             != null && pathTilemap.HasTile(cell);
                bool hasWater            = waterTilemap            != null && waterTilemap.HasTile(cell);
                bool hasHeat             = heatTilemap             != null && heatTilemap.HasTile(cell);
                bool hasHighgroundGround = highgroundGroundTilemap != null && highgroundGroundTilemap.HasTile(cell);
                bool hasHighgroundWater  = highgroundWaterTilemap  != null && highgroundWaterTilemap.HasTile(cell);
                bool hasLowgroundGround  = lowgroundGroundTilemap  != null && lowgroundGroundTilemap.HasTile(cell);
                bool hasLowgroundWater   = lowgroundWaterTilemap   != null && lowgroundWaterTilemap.HasTile(cell);

                // priority: collider > path > water > highground > lowground > ground (default snow).
                // heat is NOT part of this chain — it's a standalone flag (see isHeatSource below),
                // so a tile can be plain open ground that radiates heat, or a collider that also
                // radiates heat, independent of whatever it's painted as here
                if      (hasCollider)         t.tileType = TileType.Obstacle;
                else if (hasPath)             t.tileType = TileType.Path;
                else if (hasWater)            t.tileType = TileType.Water;
                else if (hasHighgroundWater)  t.tileType = TileType.Water;
                else if (hasLowgroundWater)   t.tileType = TileType.Water;
                else                          t.tileType = TileType.Snow;

                if (hasHighgroundGround || hasHighgroundWater) t.isHighground = true;
                if (hasLowgroundGround  || hasLowgroundWater)  t.isLowground  = true;
                if (hasHeat) t.isHeatSource = true;
            }
        }

        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int( 1,  1), new Vector2Int(-1,  1),
            new Vector2Int( 1, -1), new Vector2Int(-1, -1)
        };

        foreach (var kvp in tileMap)
        {
            foreach (var dir in directions)
            {
                if (!tileMap.TryGetValue(kvp.Key + dir, out Tile neighbor)) continue;
                if (neighbor.tileType == TileType.Water) kvp.Value.isWaterAdjacent = true;
            }
        }

        // heat reaches a fixed radius, independent of the 1-cell water adjacency above
        const float heatRadius = 3f;
        foreach (var kvp in tileMap)
        {
            if (kvp.Value.isHeatSource) continue; // a heat source doesn't need to warm itself
            foreach (var other in tileMap.Values)
            {
                if (!other.isHeatSource) continue;
                if (Vector3.Distance(kvp.Value.transform.position, other.transform.position) > heatRadius) continue;
                kvp.Value.isHeatAdjacent = true;
                break;
            }
        }
    }

    private void SpawnLightTilemapLights()
    {
        int[] sortingLayerIDs = GetAllSortingLayerIDs();
        SpawnLightsFrom(lightGroundTilemap, sortingLayerIDs);
        SpawnLightsFrom(lightAirTilemap, sortingLayerIDs);
        SpawnLightsFrom(lightColliderTilemap, sortingLayerIDs);
    }

    private void SpawnLightsFrom(Tilemap lightTilemap, int[] sortingLayerIDs)
    {
        if (lightTilemap == null) return;

        BoundsInt bounds = lightTilemap.cellBounds;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!lightTilemap.HasTile(cell)) continue;

            Vector3 worldCenter = lightTilemap.GetCellCenterWorld(cell);

            GameObject lightGO = new GameObject("SnowLight");
            lightGO.transform.SetParent(transform);
            lightGO.transform.position = worldCenter;

            Light2D light = lightGO.AddComponent<Light2D>();
            light.lightType            = Light2D.LightType.Point;
            light.pointLightOuterRadius = lightRadius;
            light.pointLightInnerRadius = Mathf.Min(lightInnerRadius, lightRadius);
            light.intensity            = lightIntensity;
            light.falloffIntensity     = lightFalloff;
            light.targetSortingLayers  = sortingLayerIDs;

            DarknessManager.RegisterLightSource(lightGO.transform, lightRadius);
        }
    }

    private int[] GetAllSortingLayerIDs()
    {
        var layers = SortingLayer.layers;
        int[] ids = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++)
            ids[i] = layers[i].id;
        return ids;
    }

    private Vector3 GridToWorld(int x, int y)
    {
        return new Vector3(x - (columns + 4) / 2f, y - (rows - 1) / 2f, 0) + transform.position;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
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
