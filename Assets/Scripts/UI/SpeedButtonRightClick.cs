using UnityEngine;
using UnityEngine.EventSystems;

// sits alongside the speed button's own Button component (which still handles left click via
// GameButtons.ToggleSpeed through its normal OnClick wiring). right click steps the game speed
// back down to the previous option instead of cycling forward
public class SpeedButtonRightClick : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        GameManager.instance?.DecreaseSpeed();
    }
}
