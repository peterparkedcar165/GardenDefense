using UnityEngine;
using System.Collections.Generic;

public enum TileType
{
    Grass, Dirt, Water, Path, Potted, Cave, Obstacle, Sand
    /* Nature-element plants will be able to be placed on Grass, others won't, except for Flower Pot
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

    public bool isOccupied = false, isHighground = false;
    public bool isWaterAdjacent = false;
    public FlowerPot flowerPot;

    private TileType _tileType;
    public TileType tileType
    {
        get => _tileType;
        set
        {
            _tileType = value;
            if (value == TileType.Water)
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
            tileType = flowerPot.originalTileType;
            Destroy(flowerPot.gameObject);
            flowerPot = null;
            return;
        }

        if (selector.flowerPotMode)
        {
            if (tileType == TileType.Water || tileType == TileType.Path || tileType == TileType.Potted || tileType == TileType.Obstacle) return;
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
            GameObject placedPlant = Instantiate(selector.SelectedPlant, transform.position, Quaternion.identity);
            GameManager.instance.PlaySound(GameManager.instance.plantPlace);
            Plant plant = placedPlant.GetComponent<Plant>();
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
