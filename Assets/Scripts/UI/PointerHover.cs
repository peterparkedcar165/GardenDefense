using UnityEngine;
using UnityEngine.EventSystems;

// remembers whether the pointer is over this ui object, for owners that recolour things on hover
// (the plant info panel's locked skill overlay)
public class PointerHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool IsHovered { get; private set; }

    public void OnPointerEnter(PointerEventData eventData) => IsHovered = true;
    public void OnPointerExit(PointerEventData eventData) => IsHovered = false;
    private void OnDisable() => IsHovered = false;
}
