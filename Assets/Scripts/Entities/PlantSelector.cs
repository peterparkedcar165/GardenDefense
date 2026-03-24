using UnityEngine;
using UnityEngine.InputSystem;

public class PlantSelector : MonoBehaviour
{
    public GameObject SelectedPlant
    {
        get;
        private set;
    }

    public void SelectPlant(GameObject plant)
    {
        SelectedPlant = plant;
        Debug.Log("Selected " + plant);
    }

    public void ClearSelection()
    {
        SelectedPlant = null;
        Debug.Log("Cleared Selection");
    }

    private void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame && SelectedPlant != null)
            ClearSelection();
    }

}
