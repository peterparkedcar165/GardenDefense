using UnityEngine;
using UnityEngine.EventSystems;

public class StatsAreaHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject statsPanels;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (statsPanels != null) statsPanels.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (statsPanels != null) statsPanels.SetActive(false);
    }
}
