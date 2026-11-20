using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// the look of one game control button (Downloads\gd_game_controls): start, pause and resume, or the
// speed button. the greyscale button and its white shine swap sprites for idle, hover and pressed,
// the face takes the button's colour, and the icon, a 4 pixel gap and the label sit centred as one
// group, 2 pixels lower while pressed. the Button next to it still does the clicking (its own
// transition is off). GameHUD sets the colours, icon and label with Set; until then the prefab's
// own colours stay
public class GameControlButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image face;
    [SerializeField] private Image shine;
    [Tooltip("holds the icon and the label; moves down 2 art pixels while pressed")]
    [SerializeField] private RectTransform content;
    [Tooltip("holds the icon and its shadow, sized to the icon sprite")]
    [SerializeField] private RectTransform iconBox;
    [SerializeField] private Image icon;
    [Tooltip("the copy 1 pixel under the icon, in the shadow colour")]
    [SerializeField] private Image iconShadow;
    [SerializeField] private TMP_Text label;
    [Tooltip("art pixels between the icon and the label")]
    [SerializeField] private int iconGap = 4;
    [Tooltip("canvas units per art pixel")]
    [SerializeField] private float pixel = 2f;

    private GameControlsStyle.ButtonColors colors;
    private bool hasColors, hovered, pressed;
    private Button button;
    private string layoutKey;

    private void Awake()
    {
        button = GetComponent<Button>();
        Apply();
    }

    private void OnDisable()
    {
        hovered = false;
        pressed = false;
        Apply();
    }

    public void Set(GameControlsStyle.ButtonColors value, Sprite glyph, string text)
    {
        colors = value;
        hasColors = true;
        if (glyph != null)
        {
            if (icon != null) icon.sprite = glyph;
            if (iconShadow != null) iconShadow.sprite = glyph;
        }
        if (label != null) label.text = text;
        Apply();
        Relayout();
    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; Apply(); }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; Apply(); }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        pressed = true;
        Apply();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        pressed = false;
        Apply();
    }

    private bool Clickable => button == null || button.interactable;

    private void Apply()
    {
        GameControlsStyle style = GameControlsStyle.Instance;
        bool down = pressed && Clickable;
        bool over = hovered && Clickable;
        Sprite faceSprite = down ? style.buttonPressed : over ? style.buttonHover : style.buttonIdle;
        Sprite shineSprite = down ? style.buttonPressedShine : over ? style.buttonHoverShine : style.buttonIdleShine;
        if (face != null && faceSprite != null) face.sprite = faceSprite;
        if (shine != null && shineSprite != null) shine.sprite = shineSprite;
        if (content != null)
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, down ? -2f * pixel : 0f);

        if (!hasColors) return;
        if (face != null) face.color = colors.face;
        if (icon != null) icon.color = colors.text;
        if (iconShadow != null) iconShadow.color = colors.shadow;
        if (label != null)
        {
            label.color = colors.text;
            PixelTextEffect effect = label.GetComponent<PixelTextEffect>();
            if (effect != null) effect.Color = colors.shadow;
        }
    }

    private void LateUpdate()
    {
        if (!Clickable && pressed) { pressed = false; Apply(); }
        Relayout();
    }

    // the icon, the gap and the label centred as one group, every x on a whole art pixel. the icon
    // box takes the icon sprite's size and the label the width of what it says. only redone when the
    // icon or the label changes
    private void Relayout()
    {
        if (iconBox == null || label == null) return;
        Sprite glyph = icon != null ? icon.sprite : null;
        string key = (glyph != null ? glyph.name : "") + "|" + label.text;
        if (key == layoutKey) return;
        layoutKey = key;

        Vector2 iconSize = glyph != null ? glyph.rect.size * pixel : Vector2.zero;
        iconBox.sizeDelta = iconSize;
        RectTransform labelRect = label.rectTransform;
        float labelWidth = string.IsNullOrEmpty(label.text) ? 0f
            : Mathf.Ceil(label.GetPreferredValues(label.text).x / pixel) * pixel;
        labelRect.sizeDelta = new Vector2(labelWidth, labelRect.sizeDelta.y);

        float gap = iconSize.x > 0f && labelWidth > 0f ? iconGap * pixel : 0f;
        float total = iconSize.x + gap + labelWidth;
        float x = Mathf.Round((((RectTransform)transform).rect.width - total) / 2f / pixel) * pixel;
        iconBox.anchoredPosition = new Vector2(x, iconBox.anchoredPosition.y);
        labelRect.anchoredPosition = new Vector2(x + iconSize.x + gap, labelRect.anchoredPosition.y);
    }
}
