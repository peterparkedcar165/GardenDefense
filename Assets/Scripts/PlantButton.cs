using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlantButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    
    [SerializeField] private GameObject plantPrefab;
    [SerializeField] private Image buttonImage;
    [SerializeField] private TMP_Text costText;
    GameManager gameManager;
    Plant plant;

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();

        plant = plantPrefab.GetComponent<Plant>();
        buttonImage.sprite = plantPrefab.GetComponentInChildren<SpriteRenderer>().sprite;
    }

    // Update is called once per frame
    void Update()
    {
        if (Plant.AtPlacementLimit(plant.data))
        {
            costText.text = "MAX";
            costText.fontStyle = FontStyles.Bold;
            costText.color = Color.red;
            return;
        }
        costText.text = plant.sunCost.ToString();
        if (gameManager.SunCount >= plant.sunCost)
        {
            costText.fontStyle = FontStyles.Bold;
            costText.color = Color.green;
        }
        else
        {
            costText.fontStyle = FontStyles.Normal;
            costText.color = Color.red;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        string text = $"{plant.GetName()}\n\n{plant.GetDescription()}\n\n<b>Affinity</b>:\n\n{plant.GetElement()}\n{plant.GetElementDescription()}\n\n<b>Family</b>:\n\n{plant.GetFamily()}\n{plant.GetFamilyDescription()}\n\n{plant.GetFamilyPassiveDescription()}";
        PlantUpgradeUI.instance.ShowTooltip(text);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlantUpgradeUI.instance.HideTooltip();
    }
}
