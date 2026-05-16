using UnityEngine;
using System.Collections.Generic;

public class PlantBar : MonoBehaviour
{
    public static PlantBar instance;

    [SerializeField] private PlantSlotButton slotPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private PlantData[] allPlantData;

    void Awake()
    {
        if (instance != null) return;
        instance = this;
    }

    void Start()
    {
        Build();
    }

    public void Build()
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);

        foreach (string plantName in SaveManager.instance.selectedLoadout)
        {
            PlantData data = GetPlantData(plantName);
            if (data == null) continue;
            PlantSlotButton slot = Instantiate(slotPrefab, container);
            slot.Setup(data);
        }
    }

    private PlantData GetPlantData(string plantName)
    {
        foreach (var data in allPlantData)
            if (data.plantName == plantName) return data;
        return null;
    }
}
