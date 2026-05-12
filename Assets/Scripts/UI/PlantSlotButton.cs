using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlantSlotButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text sunCostText;

    private PlantData data;
    private Plant plant;
    private GameManager gameManager;

    public void Setup(PlantData plantData)
    {
        data = plantData;
        plant = plantData.plantPrefab;
        if (icon != null) icon.sprite = plantData.icon;
        if (sunCostText != null) sunCostText.text = plant.sunCost.ToString();
        gameManager = FindAnyObjectByType<GameManager>();
    }

    void Update()
    {
        if (plant == null || sunCostText == null || gameManager == null) return;
        sunCostText.text = plant.sunCost.ToString();
        if (gameManager.SunCount >= plant.sunCost)
        {
            sunCostText.fontStyle = FontStyles.Bold;
            sunCostText.color = Color.green;
        }
        else
        {
            sunCostText.fontStyle = FontStyles.Normal;
            sunCostText.color = Color.red;
        }
    }

    public void OnClicked()
    {
        if (gameManager.SunCount < plant.sunCost) return;
        if (Time.timeScale == 0) return;
        PlantSelector.instance.SelectPlant(data.plantPrefab.gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        string text = $"{plant.GetName()}\n\n{plant.GetDescription()}\n\n<b>Affinity</b>:\n\n{plant.GetElement()}\n{plant.GetElementDescription()}";
        PlantUpgradeUI.instance.ShowTooltip(text);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlantUpgradeUI.instance.HideTooltip();
    }
}
