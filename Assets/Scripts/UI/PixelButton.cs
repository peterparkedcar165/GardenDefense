using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// the look of one plant info panel button (Downloads\gd_plant_info_panel): the greyscale button
// sprite tinted by state, its contents (texts and glyphs, the parts) laid out side by side and
// centred, the text and glyph shadows, and the 2 pixel drop of the face while pressed. the Button
// next to it still does the clicking (its own colour transition is off). the owner sets the
// state with SetLook and which parts show, and this keeps the rest up to date
public class PixelButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public enum Look { Normal, Locked, Max }

    [SerializeField] private Image face;
    [Tooltip("holds the parts; moves down 2 art pixels while pressed")]
    [SerializeField] private RectTransform content;
    [Tooltip("left to right. inactive parts are skipped. each keeps its own height in the prefab, only x is set here")]
    [SerializeField] private RectTransform[] parts;
    [Tooltip("art pixels before each part (the first shown part ignores its gap)")]
    [SerializeField] private int[] gapsBefore;
    [SerializeField] private TMP_Text[] texts;
    [Tooltip("lock and arrow glyphs: the state's text colour")]
    [SerializeField] private Image[] glyphs;
    [Tooltip("the copy 1 pixel under each glyph: the state's shadow colour")]
    [SerializeField] private Image[] glyphShadows;
    [SerializeField] private Image[] suns;
    [Tooltip("canvas units per art pixel")]
    [SerializeField] private float pixel = 2f;
    [Tooltip("this button's own sprites instead of the style's button and pressed button (the targeting arrows)")]
    [SerializeField] private Sprite faceSprite;
    [SerializeField] private Sprite pressedFaceSprite;
    [Tooltip("texts and glyphs turn white while hovered (the targeting arrows)")]
    [SerializeField] private bool whiteOnHover;

    private Look look = Look.Normal;
    private bool hovered, pressed;
    private Button button;

    public Look CurrentLook => look;

    private void Awake()
    {
        button = GetComponent<Button>();
        Apply();
    }

    private void OnDisable()
    {
        hovered = false;
        pressed = false;
    }

    public void SetLook(Look value)
    {
        if (look == value) return;
        look = value;
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
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        bool down = look == Look.Normal && pressed && Clickable;
        PlantInfoPanelStyle.ButtonLook colors =
            look == Look.Max ? style.max
            : look == Look.Locked ? style.locked
            : down ? style.pressed
            : hovered && Clickable ? style.hover
            : style.normal;

        if (face != null)
        {
            Sprite sprite = down ? (pressedFaceSprite != null ? pressedFaceSprite : style.buttonPressed)
                : faceSprite != null ? faceSprite : style.button;
            if (sprite != null) face.sprite = sprite;
            face.color = colors.face;
        }
        if (content != null)
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, down ? -2f * pixel : 0f);

        bool shadow = colors.shadow.a > 0f;
        Color textColor = whiteOnHover && look == Look.Normal && hovered && !down && Clickable ? Color.white : colors.text;
        if (texts != null)
            foreach (TMP_Text text in texts)
            {
                if (text == null) continue;
                text.color = textColor;
                PixelTextEffect effect = text.GetComponent<PixelTextEffect>();
                if (effect != null) effect.Color = colors.shadow;
            }
        if (glyphs != null)
            foreach (Image glyph in glyphs)
                if (glyph != null) glyph.color = textColor;
        if (glyphShadows != null)
            foreach (Image glyph in glyphShadows)
                if (glyph != null) { glyph.color = colors.shadow; glyph.enabled = shadow; }
        if (suns != null)
            foreach (Image sun in suns)
                if (sun != null) { sun.color = colors.sun; sun.enabled = colors.showSun; }
    }

    private void LateUpdate()
    {
        if (button != null && !button.interactable && pressed) { pressed = false; Apply(); }
        Relayout();
    }

    // each part's text (null for a glyph), looked up once, and what each part showed at the last
    // layout, compared every frame without building a string
    private TMP_Text[] partTexts;
    private bool[] laidOutActive;
    private string[] laidOutText;
    private float[] partWidths;
    private bool laidOut;

    private void CachePartTexts()
    {
        if (partTexts != null && partTexts.Length == parts.Length) return;
        partTexts = new TMP_Text[parts.Length];
        laidOutActive = new bool[parts.Length];
        laidOutText = new string[parts.Length];
        partWidths = new float[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            partTexts[i] = parts[i] != null ? parts[i].GetComponent<TMP_Text>() : null;
        laidOut = false;
    }

    // true when a part was shown or hidden, or a shown text changed, since the last layout
    private bool PartsChanged()
    {
        bool changed = !laidOut;
        for (int i = 0; i < parts.Length; i++)
        {
            RectTransform part = parts[i];
            bool active = part != null && part.gameObject.activeSelf;
            string text = active && partTexts[i] != null ? partTexts[i].text : null;
            if (active != laidOutActive[i] || text != laidOutText[i])
            {
                laidOutActive[i] = active;
                laidOutText[i] = text;
                changed = true;
            }
        }
        laidOut = true;
        return changed;
    }

    // centres the shown parts side by side, every x on a whole art pixel. texts take the width of
    // what they say. only redone when what's shown changes
    public void Relayout()
    {
        if (parts == null || parts.Length == 0 || content == null) return;
        CachePartTexts();
        if (!PartsChanged()) return;

        float total = 0f;
        bool first = true;
        float[] widths = partWidths;
        for (int i = 0; i < parts.Length; i++)
        {
            widths[i] = 0f;
            RectTransform part = parts[i];
            if (part == null || !part.gameObject.activeSelf) continue;
            TMP_Text text = partTexts[i];
            float width = part.sizeDelta.x;
            if (text != null)
            {
                width = Mathf.Ceil(text.GetPreferredValues(text.text).x / pixel) * pixel;
                part.sizeDelta = new Vector2(width, part.sizeDelta.y);
            }
            widths[i] = width;
            if (!first) total += Gap(i);
            total += width;
            first = false;
        }

        float buttonWidth = ((RectTransform)transform).rect.width;
        float x = Mathf.Round((buttonWidth - total) / 2f / pixel) * pixel;
        first = true;
        for (int i = 0; i < parts.Length; i++)
        {
            RectTransform part = parts[i];
            if (part == null || !part.gameObject.activeSelf) continue;
            if (!first) x += Gap(i);
            part.anchoredPosition = new Vector2(x, part.anchoredPosition.y);
            x += widths[i];
            first = false;
        }
    }

    private float Gap(int index) =>
        gapsBefore != null && index < gapsBefore.Length ? gapsBefore[index] * pixel : 0f;
}
