using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// a word on one of the game controls' plank buttons, built in code (the loadout screen's Show Map):
// GameControlsStyle's idle, hover and pressed sprites with their shine, tinted with one of its colour
// sets, the word's capitals labelTop pixels under the top, everything 2 pixels lower while pressed,
// as GameControlButton draws Start and 1x. the Button on it does the clicking. Adopt turns a button
// already in a scene into one in place, in edit mode too, so the scene can be saved with it (the main
// menu's, Tommy 2026-11-25); its parts are then serialized and picked up again on Awake
public class PlankButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private const float Px = 2f;
    // art pixels, from GameControlsStyle: the button's height, the narrowest it's drawn, the room kept clear
    // at each end of the word (the medium button's ornaments, Tommy 2026-11-25) and the word's cap top. a
    // large one (the main menu's leafy button) has its own
    private float Height => large ? GameControlsStyle.Instance.largeButtonHeight : GameControlsStyle.Instance.buttonHeight;
    private float MinWidth => large ? GameControlsStyle.Instance.largeButtonWidth : 0f;
    private float SidePad => large ? GameControlsStyle.Instance.largeButtonSidePad : GameControlsStyle.Instance.buttonSidePad;
    private int LabelTop => large ? GameControlsStyle.Instance.largeLabelTop : GameControlsStyle.Instance.labelTop;

    [SerializeField] private Image face;
    [SerializeField] private Image shine;
    [SerializeField] private RectTransform content;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private GameControlsStyle.ButtonColors colors;
    [Tooltip("the large leafy button (the main menu's) instead of the medium one")]
    [SerializeField] private bool large;
    private PixelTextEffect shadow;
    private bool hovered, pressed;
    private Button button;
    private bool pressable = true;

    public RectTransform Rect => (RectTransform)transform;

    // a plank saved in a scene (Adopt): its parts are already there. one made by Create has none yet
    // here (Build fills them right after)
    private void Awake()
    {
        if (face == null) return;
        button = GetComponent<Button>();
        Apply();
    }

    // turns an existing UI Button into a plank in place, in edit mode or play mode: the button's own
    // Image becomes the face, its text the label (moved into a Content child so it can drop when
    // pressed), and a Shine child is added. the Button keeps its own onClick wiring. running it again on
    // the same button just restyles it
    public static PlankButton Adopt(Button target, GameControlsStyle.ButtonColors colors, TMP_FontAsset font, bool large = false)
    {
        GameObject go = target.gameObject;
        PlankButton plank = go.GetComponent<PlankButton>();
        if (plank == null) plank = go.AddComponent<PlankButton>();
        plank.button = target;
        plank.colors = colors;
        plank.large = large;

        plank.face = go.GetComponent<Image>();
        if (plank.face == null) plank.face = go.AddComponent<Image>();
        plank.face.enabled = true;
        plank.face.type = plank.SpriteType;
        plank.face.pixelsPerUnitMultiplier = 1f;
        plank.face.raycastTarget = true;

        if (plank.shine == null)
        {
            Transform found = go.transform.Find("Shine");
            plank.shine = found != null ? found.GetComponent<Image>() : null;
            if (plank.shine == null) plank.shine = plank.NewChild("Shine").gameObject.AddComponent<Image>();
        }
        plank.shine.type = plank.SpriteType;
        plank.shine.pixelsPerUnitMultiplier = 1f;
        plank.shine.raycastTarget = false;
        Stretch(plank.shine.rectTransform);
        plank.shine.transform.SetSiblingIndex(0);

        if (plank.content == null)
        {
            Transform found = go.transform.Find("Content");
            plank.content = found != null ? (RectTransform)found : plank.NewChild("Content");
        }
        Stretch(plank.content);
        plank.content.SetSiblingIndex(1);

        if (plank.label == null)
        {
            plank.label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (plank.label == null) plank.label = plank.NewChild("Label", plank.content).gameObject.AddComponent<TextMeshProUGUI>();
        }
        if (plank.label.transform.parent != plank.content) plank.label.transform.SetParent(plank.content, false);
        plank.label.gameObject.SetActive(true);
        StyleLabel(plank.label, font, plank.LabelTop);
        plank.shadow = plank.label.GetComponent<PixelTextEffect>();
        if (plank.shadow == null) plank.shadow = plank.label.gameObject.AddComponent<PixelTextEffect>();

        target.transition = Selectable.Transition.None;
        target.targetGraphic = plank.face;
        plank.Resize();
        plank.Apply();
        return plank;
    }

    // the medium button's middle stretches (it's plain); the large one's repeats, its wood has grain
    private Image.Type SpriteType => large ? Image.Type.Tiled : Image.Type.Sliced;

    // the word: native size, the capitals labelTop art px under the plank's top
    private static void StyleLabel(TextMeshProUGUI text, TMP_FontAsset font, int labelTop)
    {
        if (font != null) text.font = font;
        text.fontSize = 20f;
        text.fontStyle = FontStyles.Normal;
        text.alignment = TextAlignmentOptions.Top;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.margin = Vector4.zero;
        RectTransform r = text.rectTransform;
        // the font's capitals start 3 pixels under the top of its rect at its native size
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -(labelTop - 3) * Px);
        r.sizeDelta = new Vector2(0f, 14f * Px);
    }

    // false: a plank that only reacts to the pointer over it (the loadout screen's Show Map, Tommy
    // 2026-11-25): it still lights up while hovered, but never shows pressed and can't be clicked
    public bool Pressable
    {
        get => pressable;
        set
        {
            pressable = value;
            if (button != null) button.interactable = value;
            if (!value) pressed = false;
            if (face != null) Apply();
        }
    }

    public static PlankButton Create(RectTransform parent, string text, GameControlsStyle.ButtonColors colors, TMP_FontAsset font, Action onClick)
    {
        GameObject go = new GameObject("PlankButton " + text, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        PlankButton plank = go.AddComponent<PlankButton>();
        plank.Build(text, colors, font, onClick);
        return plank;
    }

    private void Build(string text, GameControlsStyle.ButtonColors buttonColors, TMP_FontAsset font, Action onClick)
    {
        colors = buttonColors;
        face = gameObject.AddComponent<Image>();
        face.type = Image.Type.Sliced;
        face.pixelsPerUnitMultiplier = 1f;
        face.raycastTarget = true;

        shine = NewChild("Shine").gameObject.AddComponent<Image>();
        shine.type = Image.Type.Sliced;
        shine.pixelsPerUnitMultiplier = 1f;
        shine.raycastTarget = false;
        Stretch(shine.rectTransform);

        content = NewChild("Content");
        Stretch(content);

        RectTransform labelRect = NewChild("Label", content);
        label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        StyleLabel(label, font, LabelTop);
        shadow = labelRect.gameObject.AddComponent<PixelTextEffect>();

        button = gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = face;
        button.onClick.AddListener(() => onClick?.Invoke());

        Resize();
        Apply();
    }

    public void SetLabel(string text)
    {
        if (label.text == text) return;
        label.text = text;
        Resize();
    }

    private void Resize() => Rect.sizeDelta = new Vector2(NaturalWidth, Height * Px);

    // canvas units: as wide as its word and the clear ends need, at least MinWidth (before any scale)
    public float NaturalWidth
    {
        get
        {
            float word = Mathf.Ceil(label.GetPreferredValues(label.text).x / Px);
            return Mathf.Max(word + 2f * SidePad, MinWidth) * Px;
        }
    }

    private void Apply()
    {
        if (face == null || shine == null || content == null || label == null) return;
        GameControlsStyle style = GameControlsStyle.Instance;
        if (large && style.largeButton != null)
        {
            // one look: hover and pressed are its colours (and the word's drop) only
            face.sprite = style.largeButton;
            shine.sprite = style.largeButtonTrim;
        }
        else
        {
            face.sprite = pressed ? style.buttonPressed : hovered ? style.buttonHover : style.buttonIdle;
            shine.sprite = pressed ? style.buttonPressedShine : hovered ? style.buttonHoverShine : style.buttonIdleShine;
        }
        shine.enabled = shine.sprite != null;
        // one that can't be clicked is the grey locked plank (GameControlsStyle.locked), never lit or pressed
        shownUsable = Usable;
        GameControlsStyle.ButtonColors look = shownUsable ? colors : style.locked;
        face.color = pressed ? look.facePressed : hovered ? look.faceHover : look.face;
        shine.color = Color.white;
        content.anchoredPosition = new Vector2(0f, pressed ? -2f * Px : 0f);
        label.color = look.text;
        // a saved plank finds its label's shadow when it first needs it
        if (shadow == null) shadow = label.GetComponent<PixelTextEffect>();
        if (shadow != null) shadow.Color = look.shadow;
    }

    // a plank whose Button can't be clicked (a locked level, Next with no plant picked) is the grey locked
    // plank and doesn't light up or press; a hover only plank (Pressable false) never counts as that
    private bool Usable => !pressable || button == null || button.interactable;
    private bool shownUsable = true;

    private void Update()
    {
        bool usable = Usable;
        if (usable == shownUsable) return;
        if (!usable) { hovered = false; pressed = false; }
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData) { if (!Usable) return; hovered = true; Apply(); }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; Apply(); }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !pressable || !Usable) return;
        pressed = true;
        Apply();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        pressed = false;
        Apply();
    }

    private void OnDisable()
    {
        hovered = false;
        pressed = false;
        if (face != null) Apply();
    }

    private RectTransform NewChild(string name, Transform parent = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent != null ? parent : transform, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
