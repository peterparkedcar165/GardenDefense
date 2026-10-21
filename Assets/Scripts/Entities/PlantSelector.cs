using UnityEngine;
using UnityEngine.InputSystem;

public class PlantSelector : MonoBehaviour
{
    public bool uprootMode;
    public bool flowerPotMode;
    public bool waterPotMode;
    public bool transplantMode;
    public Plant transplantingPlant;
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
        transplantMode = false;
        transplantingPlant = null;
        uprootMode = newMode;
        CursorManager.instance?.SetMode(newMode ? CursorState.Uproot : CursorState.Default);
    }

    public void SetFlowerPotMode(bool newMode)
    {
        if (newMode) SkillTargetingManager.instance?.CancelAll();
        ClearSelection();
        uprootMode   = false;
        waterPotMode = false;
        transplantMode = false;
        transplantingPlant = null;
        flowerPotMode = newMode;
    }

    public void SetWaterPotMode(bool newMode)
    {
        if (newMode) SkillTargetingManager.instance?.CancelAll();
        ClearSelection();
        uprootMode    = false;
        flowerPotMode = false;
        transplantMode = false;
        transplantingPlant = null;
        waterPotMode  = newMode;
    }

    // Transplant mulch: click the button on the selected plant's info panel, then click a valid
    // destination tile - same shape as flower/water pot modes, but the "placement" step reads
    // transplantingPlant.allowedTiles instead of a prefab (see Tile.OnMouseDown)
    public void SetTransplantMode(Plant plant)
    {
        if (plant == null || !plant.TransplantReady) return;
        SkillTargetingManager.instance?.CancelAll();
        ClearSelection();
        uprootMode    = false;
        flowerPotMode = false;
        waterPotMode  = false;
        transplantMode = true;
        transplantingPlant = plant;
        CursorManager.instance?.SetMode(CursorState.Uproot);
    }

    public void CancelTransplantMode()
    {
        transplantMode = false;
        transplantingPlant = null;
        CursorManager.instance?.SetMode(CursorState.Default);
    }

    public void SelectPlant(GameObject plant)
    {
        Plant plantComponent = plant.GetComponent<Plant>();
        if (plantComponent != null && Plant.AtPlacementLimit(plantComponent.data)) return; // no room left - selection rejected

        SkillTargetingManager.instance?.CancelAll();
        uprootMode    = false;
        flowerPotMode = false;
        waterPotMode  = false;
        transplantMode = false;
        transplantingPlant = null;
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
        transplantMode = false;
        transplantingPlant = null;
        CursorIcon.instance?.OnSelectionChanged();
    }

    private void Update()
    {
        bool uiOpen = (LoadoutSelectionUI.instance != null && LoadoutSelectionUI.instance.IsOpen)
                   || (FertilizerSelectionUI.instance != null && FertilizerSelectionUI.instance.IsOpen)
                   || (MulchSelectionUI.instance != null && MulchSelectionUI.instance.IsOpen);
        if (uiOpen) return;

        if (Mouse.current.rightButton.wasPressedThisFrame || (Keyboard.current.escapeKey.wasPressedThisFrame && !TutorialManager.BlocksEscape))
        {
            ClearSelection();
            uprootMode    = false;
            flowerPotMode = false;
            waterPotMode  = false;
            transplantMode = false;
            transplantingPlant = null;
            CursorManager.instance?.SetMode(CursorState.Default);
        }

        if (Keyboard.current.fKey.wasPressedThisFrame && SaveManager.instance?.saveData.flowerPotLevel > 0
            && SaveManager.instance.saveData.highestLevelUnlocked >= SaveData.FlowerPotRequiredHighestLevelUnlocked)
            SetFlowerPotMode(!flowerPotMode);

        if (Keyboard.current.wKey.wasPressedThisFrame && SaveManager.instance?.saveData.waterPotLevel > 0)
            SetWaterPotMode(!waterPotMode);
    }
}
