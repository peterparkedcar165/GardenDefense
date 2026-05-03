using UnityEngine;
using UnityEngine.InputSystem;

public class PlantSelector : MonoBehaviour
{
    public bool uprootMode;
    public static PlantSelector instance;
    public GameObject SelectedPlant
    {
        get;
        private set;
    }

    private void Awake()
    {
        instance = this;
    }
    public void SetUprootMode(bool newMode)
    {
        ClearSelection();
        uprootMode = newMode;
        Debug.Log("Set uproot mode to " + uprootMode);
    }

    public void SelectPlant(GameObject plant)
    {
        SelectedPlant = plant;
        GameManager.instance.PlaySound(GameManager.instance.plantSelect);
        Debug.Log("Selected " + plant);
    }

    public void ClearSelection()
    {
        SelectedPlant = null;
        Debug.Log("Cleared Selection");
    }

    private void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame) {
            ClearSelection();
            uprootMode = false;
        }
    }

}
