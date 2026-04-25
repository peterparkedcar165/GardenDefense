using UnityEngine;

public class Tile : MonoBehaviour
{
    public bool isOccupied = false;
    
    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        Debug.Log("Tile clicked");
        if (isOccupied){
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

        int cost = selector.SelectedPlant.GetComponent<Plant>().sunCost;
        Debug.Log("Cost of selected plant is: " + cost);

        if (gm.SpendSun(cost))
        {
            Instantiate(selector.SelectedPlant, transform.position, Quaternion.identity);
            isOccupied = true;
            selector.ClearSelection();
            Debug.Log("Spent " + cost);
        }
    }
}
