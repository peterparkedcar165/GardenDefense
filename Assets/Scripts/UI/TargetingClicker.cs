using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// the plant info panel's targeting button (Tommy, 2026-11-26): the arrows are gone and the mode's name
// sits on a button like the upgrade buttons, Tommy's small button (PlantInfoPanelStyle.upgradeButton and
// its trim) stretched at its plain middle to the slot's width, its wood in the upgrade buttons' colours
// (normal, hover, pressed). a left click steps to the next targeting mode, a right click back, both
// wrapping round, and the name drops 2 art pixels while either is held. while the pointer is over it the
// panel's tooltip lists every mode (PlantUpgradeUI.ShowTargetingTooltip), kept up to date as it's clicked
public class TargetingClicker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    IPointerDownHandler, IPointerUpHandler
{
    private const float Px = 2f;

    public PlantUpgradeUI ui;
    private Image face;
    private TMP_Text label;
    private PixelTextEffect labelShadow;
    private float labelTop;
    private bool hovered, pressed;

    // the pointer is over it (and it's shown): the info panel then doesn't close on a right click
    public bool Hovered => hovered && isActiveAndEnabled;

    // the button's wood and the mode's name; labelTopUnits is the name's rect top under the button's top
    public void Build(Image faceImage, TMP_Text modeText, float labelTopUnits)
    {
        face = faceImage;
        label = modeText;
        labelShadow = label != null ? label.GetComponent<PixelTextEffect>() : null;
        labelTop = labelTopUnits;
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        Apply();
        if (ui != null) ui.ShowTargetingTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        pressed = false;
        Apply();
        if (ui != null) ui.HideTooltip();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsStepButton(eventData)) return;
        pressed = true;
        Apply();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsStepButton(eventData)) return;
        pressed = false;
        Apply();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ui == null) return;
        if (eventData.button == PointerEventData.InputButton.Left) ui.OnTargetingNextClicked();
        else if (eventData.button == PointerEventData.InputButton.Right) ui.OnTargetingPrevClicked();
        else return;
        if (hovered) ui.ShowTargetingTooltip();
    }

    private static bool IsStepButton(PointerEventData e) =>
        e.button == PointerEventData.InputButton.Left || e.button == PointerEventData.InputButton.Right;

    // the panel tints its parts with the element's colours when a plant is picked (ApplyElement); the
    // button keeps its own, put back whenever something else changed them
    private void LateUpdate() => Apply();

    private void Apply()
    {
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        PlantInfoPanelStyle.ButtonLook look = pressed ? style.pressed : hovered ? style.hover : style.normal;
        if (face != null) face.color = look.face;
        if (label == null) return;
        label.color = look.text;
        if (labelShadow != null) labelShadow.Color = look.shadow;
        RectTransform r = label.rectTransform;
        float y = -labelTop - (pressed ? 2f * Px : 0f);
        if (r.anchoredPosition.y != y) r.anchoredPosition = new Vector2(r.anchoredPosition.x, y);
    }

    private void OnDisable()
    {
        if (hovered && ui != null) ui.HideTooltip();
        hovered = false;
        pressed = false;
        Apply();
    }
}
