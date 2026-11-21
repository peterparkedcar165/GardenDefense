using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// the look of one game control button (Downloads\gd_game_controls, Bigger Planks): the long start,
// pause and resume button, or the speed button. the greyscale button and its white shine swap
// sprites for idle, hover and pressed, the face takes that state's colour, and the icon, a gap and
// the word (Start, 1x) sit centred on the button as one group by their drawn pixels, the icon with
// a copy 1 pixel lower in the shadow colour. heights and the gap are on GameControlsStyle.
// everything inside drops 2 pixels while pressed. the Button next to it still does the clicking
// (its own transition is off). GameHUD sets the colours, icon and word with Set; until then the
// prefab's own look stays
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
    [Tooltip("the word beside the icon; hidden when the button has none")]
    [SerializeField] private TMP_Text label;
    [Tooltip("canvas units per art pixel")]
    [SerializeField] private float pixel = 2f;

    private GameControlsStyle.ButtonColors colors;
    private bool hasColors, hovered, pressed;
    // held down by a key (space for the start / pause / resume button): looks pressed like a click
    private bool keyHeld;
    private Button button;
    private int iconTop;
    private bool hasPlacement;
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

    // glyph: the icon, its top iconTop art pixels under the button's top. text: the word beside it;
    // empty hides it
    public void Set(GameControlsStyle.ButtonColors value, Sprite glyph, int iconTop, string text)
    {
        colors = value;
        hasColors = true;
        this.iconTop = iconTop;
        hasPlacement = true;
        if (glyph != null)
        {
            if (icon != null) icon.sprite = glyph;
            if (iconShadow != null) iconShadow.sprite = glyph;
        }
        if (label != null)
        {
            bool show = !string.IsNullOrEmpty(text);
            if (label.gameObject.activeSelf != show) label.gameObject.SetActive(show);
            if (show) label.text = text;
        }
        Apply();
        Relayout();
    }

    public void SetKeyHeld(bool held)
    {
        if (keyHeld == held) return;
        keyHeld = held;
        Apply();
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
        bool down = (pressed || keyHeld) && Clickable;
        bool over = hovered && Clickable;
        Sprite faceSprite = down ? style.buttonPressed : over ? style.buttonHover : style.buttonIdle;
        Sprite shineSprite = down ? style.buttonPressedShine : over ? style.buttonHoverShine : style.buttonIdleShine;
        if (face != null && faceSprite != null) face.sprite = faceSprite;
        if (shine != null && shineSprite != null) shine.sprite = shineSprite;
        if (content != null)
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, down ? -2f * pixel : 0f);

        if (!hasColors) return;
        if (face != null) face.color = down ? colors.facePressed : over ? colors.faceHover : colors.face;
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

    // the icon, the gap and the word's drawn pixels centred on the button as one group, left edge
    // rounded down, the icon sized to its sprite. only redone when the icon or the word changes
    private void Relayout()
    {
        if (!hasPlacement || iconBox == null) return;
        GameControlsStyle style = GameControlsStyle.Instance;
        Sprite glyph = icon != null ? icon.sprite : null;
        bool showLabel = label != null && label.gameObject.activeSelf;
        string key = (glyph != null ? glyph.name : "") + "|" + (showLabel ? label.text : "") + "|" + iconTop + "|"
            + style.iconGap + "|" + style.labelTop;
        if (key == layoutKey) return;
        layoutKey = key;

        float iconWidth = glyph != null ? glyph.rect.width : 0f;
        float inkLeft = 0f, inkWidth = 0f;
        bool hasInk = showLabel && PixelLayout.Ink(label, pixel, out inkLeft, out inkWidth);
        float gap = iconWidth > 0f && hasInk ? style.iconGap : 0f;
        float buttonWidth = Mathf.Round(((RectTransform)transform).rect.width / pixel);
        float left = Mathf.Floor((buttonWidth - (iconWidth + gap + inkWidth)) / 2f);

        iconBox.sizeDelta = glyph != null ? glyph.rect.size * pixel : Vector2.zero;
        iconBox.anchoredPosition = new Vector2(left * pixel, -iconTop * pixel);
        if (!hasInk) return;
        // the font's capitals start 3 pixels under the top of its rect at its native size
        RectTransform labelRect = label.rectTransform;
        labelRect.anchoredPosition = new Vector2((left + iconWidth + gap - inkLeft) * pixel, -(style.labelTop - 3) * pixel);
    }
}
