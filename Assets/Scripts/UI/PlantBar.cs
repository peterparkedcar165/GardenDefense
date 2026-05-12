using UnityEngine;
using System.Collections.Generic;

public class PlantBar : MonoBehaviour
{
    [SerializeField] private PlantSlotButton slotPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private PlantData[] allPlantData;

    void Start()
    {
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
