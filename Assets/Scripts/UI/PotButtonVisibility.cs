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
        _flowerPotButton?.SetActive(SaveManager.instance.saveData.flowerPotLevel > 0);
        _waterPotButton?.SetActive(SaveManager.instance.saveData.waterPotLevel > 0);
    }
}
