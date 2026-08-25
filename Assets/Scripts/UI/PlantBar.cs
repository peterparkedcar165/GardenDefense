using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlantBar : MonoBehaviour
{
    public static PlantBar instance;

    [SerializeField] private PlantSlotButton slotPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private PlantRegistry plantRegistry;

    private readonly List<PlantSlotButton> slots = new List<PlantSlotButton>();

    private static readonly Key[] digitKeys = new Key[]
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8,
    };

    // F1-F8 mirror the toolbar slots, but cycle through already-placed instances of that
    // slot's plant type instead of picking a seed to place
    private static readonly Key[] fKeys = new Key[]
    {
        Key.F1, Key.F2, Key.F3, Key.F4,
        Key.F5, Key.F6, Key.F7, Key.F8,
    };

    void Awake()
    {
        if (instance != null) return;
        instance = this;
    }

    void Start()
    {
        // Clear any stale slots from a previous run.
        // Build() is called by GameManager.OnLoadoutConfirmed() once the player confirms.
        foreach (Transform child in container)
            Destroy(child.gameObject);
        slots.Clear();
    }

    void Update()
    {
        if (LoadoutSelectionUI.instance != null && LoadoutSelectionUI.instance.IsOpen) return;
        if (FertilizerSelectionUI.instance != null && FertilizerSelectionUI.instance.IsOpen) return;

        for (int i = 0; i < slots.Count && i < digitKeys.Length; i++)
        {
            if (Keyboard.current[digitKeys[i]].wasPressedThisFrame)
                slots[i].OnClicked();
        }

        for (int i = 0; i < slots.Count && i < fKeys.Length; i++)
        {
            if (Keyboard.current[fKeys[i]].wasPressedThisFrame)
                CyclePlacedPlant(slots[i].Data);
        }
    }

    // selects the oldest-placed live instance of this plant type, or the next one placed after
    // the currently-selected plant if it's already one of this type (wrapping to the oldest
    // again after the most recently placed one)
    private void CyclePlacedPlant(PlantData slotData)
    {
        if (slotData == null || PlantUpgradeUI.instance == null) return;

        List<Plant> matching = new List<Plant>();
        foreach (Plant plant in Plant.allPlants)
            if (plant != null && plant.IsAlive && plant.data != null && plant.data.plantName == slotData.plantName)
                matching.Add(plant);

        if (matching.Count == 0) return;

        Plant current = PlantUpgradeUI.instance.GetSelectedPlant();
        int currentIndex = matching.IndexOf(current);
        Plant next = currentIndex < 0 ? matching[0] : matching[(currentIndex + 1) % matching.Count];

        PlantUpgradeUI.instance.ShowPanel(next);
        CameraFit.instance?.CenterOn(next.transform.position);
    }

    public void Clear()
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);
        slots.Clear();
    }

    public void Build()
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);
        slots.Clear();

        foreach (string plantName in SaveManager.instance.selectedLoadout)
        {
            PlantData data = GetPlantData(plantName);
            if (data == null) continue;
            PlantSlotButton slot = Instantiate(slotPrefab, container);
            slot.Setup(data);
            slots.Add(slot);
        }
    }

    private PlantData GetPlantData(string plantName)
    {
        foreach (var data in plantRegistry.plants)
            if (data != null && data.plantName == plantName) return data;
        return null;
    }
}
