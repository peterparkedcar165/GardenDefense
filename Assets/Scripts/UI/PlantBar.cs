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
            if (data.plantName == plantName) return data;
        return null;
    }
}
