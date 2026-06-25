using UnityEngine;
using UnityEngine.EventSystems;

public class ToolButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum ToolType { Shovel, FlowerPot, WaterPot }

    [SerializeField] private ToolType toolType;

    private string _text;

    void Awake()
    {
        _text = toolType switch
        {
            ToolType.Shovel    => "<b>Shovel</b>\n\nRemoves a plant from the garden, and refunds <color=green><b>50%</b></color> of the total amount of <color=green><b>Sun</b></color> spent.\nIf in setup mode, refunds <color=green><b>100%</b></color>.",
            ToolType.FlowerPot => "<b>Flower Pot</b>\n\nAllows a plant to be placed on top.",
            ToolType.WaterPot  => "<b>Water Pot</b>\n\nAllows an aquatic plant to be placed on top.",
            _                  => ""
        };
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlantUpgradeUI.instance?.ShowTooltip(_text);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlantUpgradeUI.instance?.HideTooltip();
    }
}
