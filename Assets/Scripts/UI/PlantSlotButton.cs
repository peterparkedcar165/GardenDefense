using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlantSlotButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // the bevel portrait (gd_portrait_bevel_grey, drawn 72 by 72, shown at the button's 100 by 100,
    // Tommy): background is the greyscale backdrop filling the frame's window (64 of its 72), tinted
    // with the element colour mixed 60% toward white; icon is the plant, 10% smaller than the frame;
    // frame is the greyscale bevel on top, tinted with the element colour. both colours come from
    // PlantData (ElementColor, ElementBackdropColor)
    [SerializeField] private Image icon;
    [SerializeField] private Image background;
    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text sunCostText;

    private PlantData data;
    private Plant plant;

    public PlantData Data => data;

    public void Setup(PlantData plantData)
    {
        data = plantData;
        plant = plantData.plantPrefab;
        if (icon != null) icon.sprite = plantData.icon;
        if (background != null) background.color = PlantData.ElementBackdropColor(plantData.elementalType);
        if (frame != null) frame.color = PlantData.ElementColor(plantData.elementalType);
        if (sunCostText != null) sunCostText.text = FertilizerManager.GetPlacementCost(data).ToString();
    }

    void Update()
    {
        if (data == null || sunCostText == null || GameManager.instance == null) return;
        if (Plant.AtPlacementLimit(data))
        {
            sunCostText.text = "MAX";
            sunCostText.fontStyle = FontStyles.Bold;
            sunCostText.color = Color.red;
            return;
        }
        int effectiveCost = FertilizerManager.GetPlacementCost(data);
        sunCostText.text = effectiveCost.ToString();
        if (GameManager.instance.SunCount >= effectiveCost)
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
        if (GameManager.instance == null || GameManager.instance.SunCount < FertilizerManager.GetPlacementCost(data)) return;
        if (Plant.AtPlacementLimit(data)) return;
        if (FertilizerSelectionUI.instance != null && FertilizerSelectionUI.instance.IsOpen) return;
        if (MulchDebugMenu.instance != null && MulchDebugMenu.instance.IsOpen) return;
        if (PlantSelector.instance == null) return;
        PlantSelector.instance.SelectPlant(data.plantPrefab.gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        string text = $"{plant.GetName()}\n\n{plant.GetDescription()}\n\n<b>Element</b>:\n\n{plant.GetElement()}\n{plant.GetElementDescription()}";
        PlantUpgradeUI.instance.ShowTooltip(text);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlantUpgradeUI.instance.HideTooltip();
    }
}
