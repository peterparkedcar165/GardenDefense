using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class UnderwaterGridManager : MonoBehaviour
{
    public int rows, columns;
    public GameObject underwaterTilePrefab;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap pathTilemap;
    [SerializeField] private Tilemap colliderTilemap;
    [SerializeField] private Tilemap nonColliderGroundTilemap;
    [SerializeField] private Tilemap nonColliderAirTilemap;
    [SerializeField] private Tilemap highgroundGroundTilemap;
    [SerializeField] private Tilemap lightColliderTilemap;
    [SerializeField] private Tilemap airBubbleTilemap;

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
                GameObject tileGO = Instantiate(underwaterTilePrefab, worldPos, Quaternion.identity, transform);
                Tile t = tileGO.GetComponent<Tile>();
                tileMap[new Vector2Int(x, y)] = t;

                Vector3Int cell = groundTilemap.WorldToCell(worldPos);

                // LightCollider counts as a collider too, same as every other biome's campfire-style tile
                bool hasCollider         = (colliderTilemap != null && colliderTilemap.HasTile(cell))
                                         || (lightColliderTilemap != null && lightColliderTilemap.HasTile(cell));
                bool hasPath             = pathTilemap             != null && pathTilemap.HasTile(cell);
                bool hasHighgroundGround = highgroundGroundTilemap != null && highgroundGroundTilemap.HasTile(cell);
                bool hasAirBubble        = airBubbleTilemap        != null && airBubbleTilemap.HasTile(cell);

                // priority: collider > path > ground (default seafloor). air bubble is a
                // standalone flag, independent of tileType — a bubble can sit on any tile
                if      (hasCollider) t.tileType = TileType.Obstacle;
                else if (hasPath)     t.tileType = TileType.Path;
                else                  t.tileType = TileType.Seafloor;

                if (hasHighgroundGround) t.isHighground = true;
                if (hasAirBubble) t.isAirBubble = true;
            }
        }
    }

    private void SpawnLightTilemapLights()
    {
        int[] sortingLayerIDs = GetAllSortingLayerIDs();
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

            GameObject lightGO = new GameObject("UnderwaterLight");
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
