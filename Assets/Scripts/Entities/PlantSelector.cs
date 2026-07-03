using UnityEngine;
using UnityEngine.InputSystem;

public class PlantSelector : MonoBehaviour
{
    public bool uprootMode;
    public bool flowerPotMode;
    public bool waterPotMode;
    public static PlantSelector instance;

    public GameObject flowerPotPrefab;
    public GameObject waterPotPrefab;

    public GameObject SelectedPlant { get; private set; }

    private void Awake()
    {
        if (instance != null) return;
        instance = this;
    }

    public void SetUprootMode(bool newMode)
    {
        if (newMode) SkillTargetingManager.instance?.CancelAll();
        ClearSelection();
        flowerPotMode = false;
        waterPotMode  = false;
        uprootMode = newMode;
        CursorManager.instance?.SetMode(newMode ? CursorState.Uproot : CursorState.Default);
    }

    public void SetFlowerPotMode(bool newMode)
    {
        if (newMode) SkillTargetingManager.instance?.CancelAll();
        ClearSelection();
        uprootMode   = false;
        waterPotMode = false;
        flowerPotMode = newMode;
    }

    public void SetWaterPotMode(bool newMode)
    {
        if (newMode) SkillTargetingManager.instance?.CancelAll();
        ClearSelection();
        uprootMode    = false;
        flowerPotMode = false;
        waterPotMode  = newMode;
    }

    public void SelectPlant(GameObject plant)
    {
        SkillTargetingManager.instance?.CancelAll();
        uprootMode    = false;
        flowerPotMode = false;
        waterPotMode  = false;
        SelectedPlant = plant;
        CursorIcon.instance?.OnSelectionChanged();
        GameManager.instance?.PlaySound(GameManager.instance.plantSelect);
    }

    public void ClearSelection()
    {
        SelectedPlant = null;
        CursorIcon.instance?.OnSelectionChanged();
    }

    public void ClearAll()
    {
        SelectedPlant = null;
        uprootMode    = false;
        flowerPotMode = false;
        waterPotMode  = false;
        CursorIcon.instance?.OnSelectionChanged();
    }

    private void Update()
    {
        bool uiOpen = (LoadoutSelectionUI.instance != null && LoadoutSelectionUI.instance.IsOpen)
                   || (FertilizerSelectionUI.instance != null && FertilizerSelectionUI.instance.IsOpen);
        if (uiOpen) return;

        if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClearSelection();
            uprootMode    = false;
            flowerPotMode = false;
            waterPotMode  = false;
        }

        if (Keyboard.current.fKey.wasPressedThisFrame && SaveManager.instance?.saveData.flowerPotLevel > 0)
            SetFlowerPotMode(!flowerPotMode);

        if (Keyboard.current.wKey.wasPressedThisFrame && SaveManager.instance?.saveData.waterPotLevel > 0)
            SetWaterPotMode(!waterPotMode);
    }
}
