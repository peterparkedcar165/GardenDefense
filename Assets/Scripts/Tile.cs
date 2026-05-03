using UnityEngine;

public enum TileType
{
    Grass, Dirt, Water, Path, Potted, Cave
    /* Nature-element plants will be able to be placed on Grass, others won't, except for Flower Pot
    Every plant that is non-aquatic will be placeable on Dirt and Potted
    Aquatic plants can be placed in Water, Pondplanters allow terrian plants to be placed on water
    Water Pot will allow aquatic plants to be placed on ground
    Cave ground requires Flower Pot and Water Pot */
}

public class Tile : MonoBehaviour
{

    public bool isOccupied = false, isHighground = false;
    public TileType tileType;
    
    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        Debug.Log("Tile clicked");
        if (isOccupied){
            Debug.Log("Tile is occupied, cannot place");
            return;
        }

        PlantSelector selector = FindAnyObjectByType<PlantSelector>();
        Debug.Log("Found Selector");
        if (selector == null || selector.SelectedPlant == null)
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

        int cost = selector.SelectedPlant.GetComponent<Plant>().sunCost;
        Debug.Log("Cost of selected plant is: " + cost);

        if (gm.SpendSun(cost))
        {
            GameObject placedPlant = Instantiate(selector.SelectedPlant, transform.position, Quaternion.identity);
            Plant plant = placedPlant.GetComponent<Plant>();
            plant.totalSunSpent += cost;
            plant.occupiedTile = this;
            isOccupied = true;
            selector.ClearSelection();
            Debug.Log("Spent " + cost);
        }
    }
}
