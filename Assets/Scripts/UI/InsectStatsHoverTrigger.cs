using UnityEngine;
using UnityEngine.EventSystems;

public class InsectStatsHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool isHovering;

    void Update()
    {
        if (!isHovering) return;

        Insect insect = InsectInfoUI.instance?.GetSelectedInsect();
        if (insect == null) return;

        PlantUpgradeUI.instance.ShowTooltip(BuildText(insect));
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovering = true;

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        PlantUpgradeUI.instance?.HideTooltip();
    }

    private string BuildText(Insect insect)
    {
        return
            $"{insect.GetPassiveDescription()}\n\n" +
            $"<b>Resistances:</b>\n" +
            $"Physical: <b>{insect.physicalResistance * 100:F0}%</b>  Magic: <b>{insect.magicResistance * 100:F0}%</b>\n" +
            $"Fire: <b>{insect.fireResistance * 100:F0}%</b>  Water: <b>{insect.waterResistance * 100:F0}%</b>  " +
            $"Nature: <b>{insect.natureResistance * 100:F0}%</b>\n" +
            $"Ice: <b>{insect.iceResistance * 100:F0}%</b>  Poison: <b>{insect.poisonResistance * 100:F0}%</b>  " +
            $"Wind: <b>{insect.windResistance * 100:F0}%</b>\n\n" +
            insect.GetActiveEffectsString();
    }
}
