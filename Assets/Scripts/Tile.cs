using UnityEngine;
using System.Collections.Generic;

public enum TileType
{
    Grass, Dirt, Water, Path, Potted, WaterPotted, Cave, Obstacle, Sand, Snow, Heat, Seafloor
    /* Grass-element plants will be able to be placed on Grass, others won't, except for Flower Pot
    Every plant that is non-aquatic will be placeable on Dirt and Potted
    Aquatic plants can be placed in Water, Pondplanters allow terrian plants to be placed on water
    Water Pot will allow aquatic plants to be placed on ground
    Cave ground requires Flower Pot and Water Pot */
}

public class Tile : MonoBehaviour
{
    // Global position → tile lookup so WaterZone can find neighbours at runtime
    public static readonly Dictionary<Vector2Int, Tile> allTiles = new Dictionary<Vector2Int, Tile>();
    public static Vector2Int TileKey(Vector3 pos) =>
        new Vector2Int(Mathf.RoundToInt(pos.x * 2), Mathf.RoundToInt(pos.y * 2));

    private void Awake()  => allTiles[TileKey(transform.position)] = this;
    private void OnDestroy() => allTiles.Remove(TileKey(transform.position));

    // world-space bounding box of every currently-registered tile, cached lazily and rebuilt
    // once per scene (tiles are level-authored and static once loaded, so the set of keys in
    // allTiles doesn't otherwise change mid-level). lets anything (e.g. projectiles) cheaply
    // check "am I still over the playable grid" without re-scanning allTiles every frame
    private static bool _boundsValid;
    private static float _minX, _maxX, _minY, _maxY;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetBoundsCacheOnLoad()
    {
        _boundsValid = false;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) => _boundsValid = false;
    }

    private static void EnsureBounds()
    {
        if (_boundsValid || allTiles.Count == 0) return;

        _minX = _minY = float.MaxValue;
        _maxX = _maxY = float.MinValue;
        foreach (Vector2Int key in allTiles.Keys)
        {
            float x = key.x * 0.5f;
            float y = key.y * 0.5f;
            if (x < _minX) _minX = x;
            if (x > _maxX) _maxX = x;
            if (y < _minY) _minY = y;
            if (y > _maxY) _maxY = y;
        }
        _boundsValid = true;
    }

    // true if worldPos falls within the playable tile grid, plus a 2-tile grace margin beyond
    // its outer edge (so things don't despawn the instant they clip past the last row of
    // tiles). if no tiles are registered yet (e.g. called before the level finishes loading),
    // defaults to true so nothing gets destroyed prematurely
    private const float TileSize = 0.5f;
    private const float GraceTiles = 2f;

    public static bool IsInsideGrid(Vector3 worldPos)
    {
        EnsureBounds();
        if (!_boundsValid) return true;

        const float margin = TileSize * 0.5f + GraceTiles * TileSize;
        return worldPos.x >= _minX - margin && worldPos.x <= _maxX + margin
            && worldPos.y >= _minY - margin && worldPos.y <= _maxY + margin;
    }

    public bool isOccupied = false, isHighground = false, isLowground = false;
    public DeadPlantRecord deadPlant;
    public bool isWaterAdjacent = false;
    public bool isHeatAdjacent  = false;
    // true if this exact tile radiates heat, independent of its resolved tileType — lets a
    // heat tile also be painted as a collider/path/water tile without losing its warmth
    public bool isHeatSource    = false;
    // true for Underwater levels' air bubble tiles: a plant standing here doesn't lose Air
    public bool isAirBubble     = false;
    public FlowerPot flowerPot;
    public WaterPot  waterPot;

    private TileType _tileType;
    public TileType tileType
    {
        get => _tileType;
        set
        {
            _tileType = value;
            if (value == TileType.Water || value == TileType.WaterPotted)
                SpawnWaterZone();
            if (value == TileType.Obstacle)
                SpawnObstacleZone();
        }
    }

    private void SpawnWaterZone()
    {
        if (GetComponentInChildren<WaterZone>() != null) return;

        GameObject zone = new GameObject("WaterZone");
        zone.transform.SetParent(transform);
        zone.transform.localPosition = Vector3.zero;
        zone.layer = LayerMask.NameToLayer("Ignore Raycast");

        BoxCollider2D col = zone.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.7f, 0.7f);

        zone.AddComponent<WaterZone>();
    }

    private void SpawnObstacleZone()
    {
        if (GetComponentInChildren<ObstacleZone>() != null) return;

        GameObject zone = new GameObject("ObstacleZone");
        zone.transform.SetParent(transform);
        zone.transform.localPosition = Vector3.zero;
        zone.layer = LayerMask.NameToLayer("Obstacle");

        BoxCollider2D col = zone.AddComponent<BoxCollider2D>();
        col.isTrigger = false;
        col.size = new Vector2(0.9f, 0.9f);

        zone.AddComponent<ObstacleZone>();
    }

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        Debug.Log("Tile clicked");
        if (isOccupied){
            Debug.Log("Tile is occupied, cannot place");
            return;
        }

        PlantSelector selector = FindAnyObjectByType<PlantSelector>();
        if (selector == null) return;

        if (selector.uprootMode && tileType == TileType.Potted && flowerPot != null)
        {
            float refundRate = GameManager.instance.currentWave == 0 ? 1f : 0.5f;
            GameManager.instance.SunCount += Mathf.CeilToInt(FlowerPot.SunCost * refundRate);
            GameManager.instance.UpdateSun();
            tileType = flowerPot.originalTileType;
            Destroy(flowerPot.gameObject);
            flowerPot = null;
            return;
        }

        if (selector.uprootMode && tileType == TileType.WaterPotted && waterPot != null)
        {
            float refundRate = GameManager.instance.currentWave == 0 ? 1f : 0.5f;
            GameManager.instance.SunCount += Mathf.CeilToInt(WaterPot.SunCost * refundRate);
            GameManager.instance.UpdateSun();
            WaterZone wz = GetComponentInChildren<WaterZone>();
            if (wz != null) Destroy(wz.gameObject);
            tileType = waterPot.originalTileType;
            Destroy(waterPot.gameObject);
            waterPot = null;
            return;
        }

        if (selector.flowerPotMode)
        {
            if (tileType == TileType.Water || tileType == TileType.Path || tileType == TileType.Potted || tileType == TileType.WaterPotted || tileType == TileType.Obstacle || tileType == TileType.Heat) return;
            if (selector.flowerPotPrefab == null || GameManager.instance == null) return;
            if (GameManager.instance.SpendSun(FlowerPot.SunCost))
            {
                GameObject potObj = Instantiate(selector.flowerPotPrefab, transform.position, Quaternion.identity);
                flowerPot = potObj.GetComponent<FlowerPot>();
                flowerPot.originalTileType = tileType;
                tileType = TileType.Potted;
                selector.flowerPotMode = false;
            }
            return;
        }

        if (selector.waterPotMode)
        {
            if (tileType == TileType.Water || tileType == TileType.Path || tileType == TileType.Potted || tileType == TileType.WaterPotted || tileType == TileType.Obstacle || tileType == TileType.Heat) return;
            if (selector.waterPotPrefab == null || GameManager.instance == null) return;
            if (GameManager.instance.SpendSun(WaterPot.SunCost))
            {
                GameObject potObj = Instantiate(selector.waterPotPrefab, transform.position, Quaternion.identity);
                waterPot = potObj.GetComponent<WaterPot>();
                waterPot.originalTileType = tileType;
                tileType = TileType.WaterPotted;
                selector.waterPotMode = false;
            }
            return;
        }

        if (selector.SelectedPlant == null)
        return;

        GameManager gm = FindAnyObjectByType<GameManager>();
        Debug.Log("Found game manager");
        if (gm == null)
        {
            Debug.Log("Gm is null");
            return;
        }


        Plant selectedPlant = selector.SelectedPlant.GetComponent<Plant>();
        if (System.Array.IndexOf(selectedPlant.allowedTiles, tileType) == -1)
        {
            Debug.Log("Cannot be planted here!");
            return;
        }

        int cost = selector.SelectedPlant.GetComponent<Plant>().data.sunCost;
        Debug.Log("Cost of selected plant is: " + cost);

        if (gm.SpendSun(cost))
        {
            deadPlant = null;
            GameObject placedPlant = Instantiate(selector.SelectedPlant, transform.position, Quaternion.identity);
            GameManager.instance.PlaySound(GameManager.instance.plantPlace);
            Plant plant = placedPlant.GetComponent<Plant>();
            plant.selfPrefab = selector.SelectedPlant;
            plant.totalSunSpent += cost;
            plant.occupiedTile = this;
            if (isHighground)
            {
                plant.attackRangeMultiplier += 0.5f;
                if (plant is Cattail || plant is LeafRanger)
                    plant.criticalChanceAdder += 0.15f;
            }
            isOccupied = true;
            GetComponent<Collider2D>().enabled = false;
            selector.ClearSelection();
            Debug.Log("Spent " + cost);
        }
    }
}
