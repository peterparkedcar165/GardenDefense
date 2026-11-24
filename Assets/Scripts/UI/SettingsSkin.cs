using UnityEngine;
using UnityEngine.UI;
using TMPro;

// the escape menu in Tommy's pixel kit (2026-11-24), applied once by SettingsManager to the menu's own
// objects, so their wiring (the mixer, PlayerPrefs, the indicator settings, Restart and Main Menu) stays:
// the volume sliders become the plant info panel's bars (bar_frame, bar_fill and its shine) with the
// info panel's button as the knob; the indicator toggles a kit box with a bloom pip for the tick; Restart
// and Main Menu the game controls' planks. 1 art pixel = 2 canvas units
public static class SettingsSkin
{
    private const float Px = 2f;
    // art pixels: the bar's frame and fill heights, the knob's size (the button sprite is 18 tall)
    private const float BarHeight = 11f;
    private const float FillHeight = 9f;
    private const float KnobWidth = 16f;
    private const float KnobHeight = 18f;
    private const float BoxSize = 19f;
    private const float TickSize = 15f;
    // canvas units: the toggles' words at the font's native size, and the space between box and words
    private const float LabelSize = 20f;
    private const float LabelGap = 12f;

    public static void Slider(Slider slider)
    {
        if (slider == null) return;
        InsectShowcaseStyle kit = InsectShowcaseStyle.Instance;
        PlantInfoPanelStyle look = PlantInfoPanelStyle.Instance;
        RectTransform root = (RectTransform)slider.transform;
        slider.transition = Selectable.Transition.None;

        // the frame, centred on the slider's height
        Transform background = root.Find("Background");
        if (background != null)
        {
            SetKit(background.GetComponent<Image>(), kit.barFrame, Color.white, true);
            RectTransform r = (RectTransform)background;
            r.anchorMin = new Vector2(0f, 0.5f);
            r.anchorMax = new Vector2(1f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(0f, BarHeight * Px);
        }

        // the fill, 1 pixel inside the frame; the slider moves its right edge
        RectTransform fill = slider.fillRect;
        if (fill != null)
        {
            RectTransform area = fill.parent as RectTransform;
            if (area != null)
            {
                area.anchorMin = new Vector2(0f, 0.5f);
                area.anchorMax = new Vector2(1f, 0.5f);
                area.pivot = new Vector2(0.5f, 0.5f);
                area.offsetMin = new Vector2(Px, -FillHeight * Px * 0.5f);
                area.offsetMax = new Vector2(-Px, FillHeight * Px * 0.5f);
            }
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            SetKit(fill.GetComponent<Image>(), kit.barFill, kit.volumeFill, false);
            if (fill.Find("Shine") == null)
            {
                RectTransform shine = NewImage("Shine", fill, kit.barFillShine, Color.white, false);
                shine.anchorMin = Vector2.zero;
                shine.anchorMax = Vector2.one;
                shine.offsetMin = shine.offsetMax = Vector2.zero;
            }
        }

        // the knob: the info panel's button, its centre able to reach both ends of the bar
        RectTransform knob = slider.handleRect;
        if (knob != null)
        {
            RectTransform area = knob.parent as RectTransform;
            if (area != null)
            {
                area.anchorMin = new Vector2(0f, 0.5f);
                area.anchorMax = new Vector2(1f, 0.5f);
                area.pivot = new Vector2(0.5f, 0.5f);
                area.offsetMin = new Vector2(KnobWidth * Px * 0.5f, -KnobHeight * Px * 0.5f);
                area.offsetMax = new Vector2(-KnobWidth * Px * 0.5f, KnobHeight * Px * 0.5f);
            }
            knob.sizeDelta = new Vector2(KnobWidth * Px, 0f);
            SetKit(knob.GetComponent<Image>(), look.button, look.normal.face, true);
        }
    }

    public static void Toggle(Toggle toggle)
    {
        if (toggle == null) return;
        InsectShowcaseStyle kit = InsectShowcaseStyle.Instance;
        toggle.transition = Selectable.Transition.None;

        RectTransform row = (RectTransform)toggle.transform;
        float box = BoxSize * Px;
        Image boxImage = toggle.targetGraphic as Image;
        if (boxImage != null)
        {
            SetKit(boxImage, kit.toggleBox, Color.white, true);
            RectTransform r = boxImage.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(box, box);
        }

        // the label: the old one is unity's default Arial (UnityEngine.UI.Text), so it hides and the same
        // words show in the game's font right of the box, with the kit's 1 pixel shadow
        Text oldLabel = toggle.GetComponentInChildren<Text>(true);
        TextMeshProUGUI label = null;
        if (oldLabel != null && toggle.transform.Find("KitLabel") == null)
        {
            oldLabel.enabled = false;
            GameObject go = new GameObject("KitLabel", typeof(RectTransform));
            go.layer = toggle.gameObject.layer;
            go.transform.SetParent(toggle.transform, false);
            label = go.AddComponent<TextMeshProUGUI>();
            if (kit.font != null) label.font = kit.font;
            label.fontSize = LabelSize;
            label.color = kit.text;
            label.alignment = TextAlignmentOptions.Left;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.margin = Vector4.zero;
            // clicking the words ticks the box too
            label.raycastTarget = true;
            label.text = oldLabel.text;
            RectTransform l = label.rectTransform;
            l.anchorMin = new Vector2(0f, 0f);
            l.anchorMax = new Vector2(1f, 1f);
            l.offsetMin = new Vector2(box + LabelGap, 0f);
            l.offsetMax = Vector2.zero;
            go.AddComponent<PixelTextEffect>();
        }

        // the row as tall as the box and as wide as box, gap and words, its left edge where it was
        float width = box + LabelGap + (label != null ? Mathf.Ceil(label.GetPreferredValues(label.text).x) + 4f : row.sizeDelta.x);
        float left = row.anchoredPosition.x - row.sizeDelta.x * row.pivot.x;
        row.sizeDelta = new Vector2(width, box);
        row.anchoredPosition = new Vector2(left + width * row.pivot.x, row.anchoredPosition.y);
        Image tick = toggle.graphic as Image;
        if (tick != null)
        {
            SetKit(tick, kit.toggleTick, kit.toggleTickColor, false);
            RectTransform t = tick.rectTransform;
            t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0.5f);
            t.anchoredPosition = Vector2.zero;
            t.sizeDelta = new Vector2(TickSize * Px, TickSize * Px);
        }
    }

    // the button's old look hides; a plank with the same word sits in its place and clicks the same
    // button, so its Restart or Main Menu wiring is untouched. call while the button is active (the
    // plank measures its word)
    public static void Plank(GameObject buttonObject, GameControlsStyle.ButtonColors colors)
    {
        if (buttonObject == null || buttonObject.GetComponentInChildren<PlankButton>(true) != null) return;
        Button button = buttonObject.GetComponent<Button>();
        if (button == null) return;
        Image face = buttonObject.GetComponent<Image>();
        TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
        string word = label != null && !string.IsNullOrEmpty(label.text) ? label.text : buttonObject.name;
        if (face != null) face.enabled = false;
        if (label != null) label.gameObject.SetActive(false);
        PlankButton plank = PlankButton.Create((RectTransform)buttonObject.transform, word, colors,
            InsectShowcaseStyle.Instance.font, () => button.onClick.Invoke());
        RectTransform r = plank.Rect;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
    }

    private static RectTransform NewImage(string name, Transform parent, Sprite sprite, Color color, bool sliced)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        SetKit(image, sprite, color, sliced);
        image.raycastTarget = false;
        return (RectTransform)go.transform;
    }

    private static void SetKit(Image image, Sprite sprite, Color color, bool sliced)
    {
        if (image == null || sprite == null) return;
        image.sprite = sprite;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = 1f;
        image.color = color;
    }
}
