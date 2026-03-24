using UnityEngine;

public class Tile : MonoBehaviour
{
    public bool isOccupied = false;
    
    private void OnMouseDown()
    {
        if (isOccupied){
            return;
        }

        PlantSelector selector = FindAnyObjectByType<PlantSelector>();
        if (selector == null || selector.SelectedPlant == null)
        return;

        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm == null)
        return;

        int cost = selector.SelectedPlant.GetComponent<Plant>().sunCost;

        if (gm.SpendSun(cost))
        {
            Instantiate(selector.SelectedPlant, transform.position, Quaternion.identity);
            isOccupied = true;
            selector.ClearSelection();
        }
    }
}
