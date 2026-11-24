using UnityEngine;

public class PotButtonVisibility : MonoBehaviour
{
    private GameObject _flowerPotButton;
    private GameObject _waterPotButton;

    private void Start()
    {
        FlowerPotButton fb = FindAnyObjectByType<FlowerPotButton>(FindObjectsInactive.Include);
        WaterPotButton  wb = FindAnyObjectByType<WaterPotButton>(FindObjectsInactive.Include);
        if (fb != null) _flowerPotButton = fb.gameObject;
        if (wb != null) _waterPotButton  = wb.gameObject;
    }

    private void Update()
    {
        if (SaveManager.instance == null) return;
        SaveData data = SaveManager.instance.saveData;
        // Flower Pot's unlock is tied directly to Level 11 (SaveManager.RepairFlowerPotFromLevel
        // keeps flowerPotLevel in sync with that already) - checked again here too as a defensive
        // backstop against any stale/older save state
        // unity null checks (a destroyed button is skipped, not thrown on), and only switched when
        // the wanted state differs
        SetShown(_flowerPotButton, data.flowerPotLevel > 0 && data.highestLevelUnlocked >= SaveData.FlowerPotRequiredHighestLevelUnlocked);
        SetShown(_waterPotButton, data.waterPotLevel > 0);
    }

    private static void SetShown(GameObject button, bool show)
    {
        if (button != null && button.activeSelf != show) button.SetActive(show);
    }
}
