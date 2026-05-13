using UnityEngine;
using UnityEngine.InputSystem;

public class PlantSelector : MonoBehaviour
{
    public bool uprootMode;
    public static PlantSelector instance;

    public GameObject SelectedPlant { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    public void SetUprootMode(bool newMode)
    {
        ClearSelection();
        uprootMode = newMode;
    }

    public void SelectPlant(GameObject plant)
    {
        SelectedPlant = plant;
        CursorIcon.instance.OnSelectionChanged();
        GameManager.instance.PlaySound(GameManager.instance.plantSelect);
    }

    public void ClearSelection()
    {
        SelectedPlant = null;
        CursorIcon.instance.OnSelectionChanged();
    }

    private void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            ClearSelection();
            uprootMode = false;
        }
    }
}
