using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// the "Sell" popup a right click opens on a bag mulch (Tommy, 2026-11-20): one button over the
// slot, in the plant info panel's button look (PlantInfoPanelStyle: tinted button sprite, lighter
// on hover, the pressed sprite with the label 2 pixels lower). it reads "Sell 50" and the sun glyph
// (Tommy, 2026-11-21: show the price), laid out like an upgrade button's price: the sun 2 pixels
// after the number, 4 under the button's top, in the look's sun colour. a left click on it sells the
// mulch, a right click closes it. MulchInventoryUI builds it on its overlay and also closes it on a
// click anywhere else, escape, a drag, or when the slot's mulch changes
public class MulchSellPopup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
    IPointerUpHandler, IPointerClickHandler
{
    // canvas units per art pixel, like the plant info panel
    private const float Px = 2f;
    // art pixels left and right of the label
    private const float PadX = 6f;
    // art pixels: the button sprite's height
    private const float Height = 18f;
    // art pixels: the sun glyph's size, its gap after the price and its drop from the button's top
    private const float SunSize = 7f;
    private const float SunGap = 2f;
    private const float SunTop = 4f;

    private Image face;
    private RectTransform content;
    private TextMeshProUGUI label;
    private Image sun;
    private PixelTextEffect shadow;
    private bool hovered, pressed;
    private System.Action onSell, onClose;

    public RectTransform Rect => (RectTransform)transform;
    public bool IsOpen => gameObject.activeSelf;

    public static MulchSellPopup Create(Transform parent, TMP_FontAsset font)
    {
        GameObject go = new GameObject("MulchSellPopup", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        MulchSellPopup popup = go.AddComponent<MulchSellPopup>();
        popup.Build(font);
        go.SetActive(false);
        return popup;
    }

    private void Build(TMP_FontAsset font)
    {
        RectTransform rect = Rect;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);

        face = gameObject.AddComponent<Image>();
        face.type = Image.Type.Sliced;
        face.raycastTarget = true;

        GameObject contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(transform, false);
        content = (RectTransform)contentGO.transform;
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(content, false);
        label = labelGO.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        // the font's native 10 pixels; top aligned, its capitals start 3 art pixels under the rect's top
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        // capitals 4 art pixels under the button's top, like the info panel's buttons
        labelRect.anchoredPosition = new Vector2(PadX * Px, -1f * Px);
        shadow = labelGO.AddComponent<PixelTextEffect>();

        GameObject sunGO = new GameObject("Sun", typeof(RectTransform));
        sunGO.transform.SetParent(content, false);
        sun = sunGO.AddComponent<Image>();
        sun.raycastTarget = false;
        sun.sprite = PlantInfoPanelStyle.Instance.sunGlyph;
        RectTransform sunRect = sun.rectTransform;
        sunRect.anchorMin = sunRect.anchorMax = new Vector2(0f, 1f);
        sunRect.pivot = new Vector2(0f, 1f);
        sunRect.sizeDelta = new Vector2(SunSize * Px, SunSize * Px);

        Layout(0);
        Apply();
    }

    // "Sell <price>" and the sun after it; the button is as wide as both plus its padding
    private void Layout(int price)
    {
        label.text = $"Sell {price}";
        float width = Mathf.Ceil(label.GetPreferredValues(label.text).x / Px) * Px;
        label.rectTransform.sizeDelta = new Vector2(width, 12f * Px);
        float right = PadX * Px + width;
        bool showSun = sun.sprite != null;
        sun.gameObject.SetActive(showSun);
        if (showSun)
        {
            sun.rectTransform.anchoredPosition = new Vector2(right + SunGap * Px, -SunTop * Px);
            right += (SunGap + SunSize) * Px;
        }
        Rect.sizeDelta = new Vector2(right + PadX * Px, Height * Px);
    }

    // opens centred on a point of the parent (the slot's middle), on whole art pixels, showing the
    // mulch's sell price
    public void Open(Vector2 center, int price, System.Action sell, System.Action close)
    {
        onSell = sell;
        onClose = close;
        hovered = false;
        pressed = false;
        Layout(price);
        Vector2 size = Rect.sizeDelta;
        Rect.anchoredPosition = new Vector2(Mathf.Round((center.x - size.x / 2f) / Px) * Px, Mathf.Round(center.y / Px) * Px);
        gameObject.SetActive(true);
        Apply();
    }

    public void Close()
    {
        onSell = null;
        onClose = null;
        gameObject.SetActive(false);
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) onSell?.Invoke();
        else if (eventData.button == PointerEventData.InputButton.Right) onClose?.Invoke();
    }

    private void Apply()
    {
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        PlantInfoPanelStyle.ButtonLook look = pressed ? style.pressed : hovered ? style.hover : style.normal;
        Sprite sprite = pressed ? style.buttonPressed : style.button;
        if (sprite != null) face.sprite = sprite;
        face.color = look.face;
        label.color = look.text;
        shadow.Color = look.shadow;
        sun.color = look.sun;
        content.anchoredPosition = new Vector2(0f, pressed ? -2f * Px : 0f);
    }
}
