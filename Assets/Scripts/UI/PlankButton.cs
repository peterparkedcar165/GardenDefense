using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// a word on one of the game controls' plank buttons, built in code (the loadout screen's Show Map):
// GameControlsStyle's idle, hover and pressed sprites with their shine, tinted with one of its colour
// sets, the word's capitals labelTop pixels under the top, everything 2 pixels lower while pressed,
// as GameControlButton draws Start and 1x. the Button on it does the clicking
public class PlankButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private const float Px = 2f;
    // art pixels: the plank sprite's height, and the space left and right of the word
    private const float Height = 34f;
    private const float SidePad = 12f;

    private Image face;
    private Image shine;
    private RectTransform content;
    private TextMeshProUGUI label;
    private PixelTextEffect shadow;
    private GameControlsStyle.ButtonColors colors;
    private bool hovered, pressed;

    public RectTransform Rect => (RectTransform)transform;

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
        if (font != null) label.font = font;
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.Top;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.margin = Vector4.zero;
        label.text = text;
        // the font's capitals start 3 pixels under the top of its rect at its native size
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -(GameControlsStyle.Instance.labelTop - 3) * Px);
        labelRect.sizeDelta = new Vector2(0f, 14f * Px);
        shadow = labelRect.gameObject.AddComponent<PixelTextEffect>();

        Button button = gameObject.AddComponent<Button>();
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

    private void Resize()
    {
        float word = Mathf.Ceil(label.GetPreferredValues(label.text).x / Px);
        Rect.sizeDelta = new Vector2((word + 2f * SidePad) * Px, Height * Px);
    }

    private void Apply()
    {
        GameControlsStyle style = GameControlsStyle.Instance;
        face.sprite = pressed ? style.buttonPressed : hovered ? style.buttonHover : style.buttonIdle;
        shine.sprite = pressed ? style.buttonPressedShine : hovered ? style.buttonHoverShine : style.buttonIdleShine;
        shine.enabled = shine.sprite != null;
        face.color = pressed ? colors.facePressed : hovered ? colors.faceHover : colors.face;
        content.anchoredPosition = new Vector2(0f, pressed ? -2f * Px : 0f);
        label.color = colors.text;
        shadow.Color = colors.shadow;
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
