using System;
using UnityEngine;
using UnityEngine.EventSystems;

// passes a UI element's pointer enter and exit to code that built it (the insect panel's cards and chips).
// drags and scrolls are left to the parents, so a card inside a scroll view still scrolls
public class UIHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Action onEnter;
    public Action onExit;
    private bool inside;

    public void OnPointerEnter(PointerEventData eventData)
    {
        inside = true;
        onEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inside = false;
        onExit?.Invoke();
    }

    // hidden or destroyed under the pointer: no exit event comes, so send one
    private void OnDisable()
    {
        if (!inside) return;
        inside = false;
        onExit?.Invoke();
    }
}
