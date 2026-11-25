using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Tommy's plant card (2026-11-25), shared by the loadout screen's cards (LoadoutSlotSquare) and the plant
// bar's buttons (PlantSlotButton): the greyscale wood tinted with the plant's element panel colour, the trim
// (vines, gold corners, the dark plant window and the cost slot) untinted over it, then the plant in the
// window and its cost in the slot. every part is placed as a share of the card (its anchors), so a card
// keeps the art's proportions at whatever size its grid cell gives it (Tommy: the loadout's cards big and
// touching, the bar's as big as fit). the sprites are PlantInfoPanelStyle.plantCardBase and plantCardTrim
public static class PlantCardLook
{
    // the art, in pixels
    public const float ArtWidth = 92f;
    public const float ArtHeight = 117f;
    public static readonly Vector2 ArtSize = new Vector2(ArtWidth, ArtHeight);
    // as art px from the card's top left: the plant's 64 px box centred on the window (the window is art
    // px 15 to 76 across, 15 to 80 down, its centre 46 and 48), the cost slot (16 to 75 across, 89 to 102
    // down) and the padlock (padlock_x3, 34 by 48 at the card's own size) centred on the window
    private static readonly Rect IconBox = new Rect(14f, 16f, 64f, 64f);
    private static readonly Rect CostBox = new Rect(16f, 89f, 60f, 14f);
    private static readonly Rect PadlockBox = new Rect(46f - PlantSlotLook.PadlockWidth * 0.5f, 48f - PlantSlotLook.PadlockHeight * 0.5f,
        PlantSlotLook.PadlockWidth, PlantSlotLook.PadlockHeight);
    // the cost at the card's own pixel size, a little under the font's native size so it fits the slot
    public const float CostFontSize = 16f;

    // an empty or open card's wood (proposal: the medium button's original wood)
    public static readonly Color NeutralWood = new Color32(0x7D, 0x44, 0x29, 0xFF);

    public static Color Wood(PlantData data) => data != null ? PlantData.ElementPanelColor(data.elementalType) : NeutralWood;

    // the card's size at a scale, canvas units (1 is 1 unit an art px)
    public static Vector2 SizeAt(float scale) => ArtSize * scale;

    // how big a card is drawn, from its width
    public static float ScaleOf(float cardWidth) => cardWidth / ArtWidth;

    public static void Fill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public static void InWindow(RectTransform rect) => Span(rect, IconBox);
    public static void InCostSlot(RectTransform rect) => Span(rect, CostBox);

    public static void PlacePadlock(Image padlock)
    {
        if (padlock != null) Span(padlock.rectTransform, PadlockBox);
    }

    // the cost's font size on a card scale times the art's size
    public static float CostFontSizeAt(float scale) => Mathf.Round(CostFontSize * scale);

    // a box given in art px from the card's top left, as anchors, so it scales with the card
    private static void Span(RectTransform rect, Rect box)
    {
        rect.anchorMin = new Vector2(box.xMin / ArtWidth, 1f - box.yMax / ArtHeight);
        rect.anchorMax = new Vector2(box.xMax / ArtWidth, 1f - box.yMin / ArtHeight);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public static void SetSprites(Image wood, Image trim)
    {
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        if (wood != null && style.plantCardBase != null) { wood.sprite = style.plantCardBase; wood.type = Image.Type.Simple; wood.preserveAspect = false; }
        if (trim != null && style.plantCardTrim != null) { trim.sprite = style.plantCardTrim; trim.type = Image.Type.Simple; trim.preserveAspect = false; }
    }

    // the cost text in the card's slot, made the first time and found again after
    public static TextMeshProUGUI Cost(Transform card, float scale)
    {
        Transform found = card.Find("Cost");
        if (found != null) return found.GetComponent<TextMeshProUGUI>();
        GameObject go = new GameObject("Cost", typeof(RectTransform));
        go.layer = card.gameObject.layer;
        go.transform.SetParent(card, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        StyleCost(text, scale);
        return text;
    }

    public static void StyleCost(TMP_Text text, float scale)
    {
        if (InsectShowcaseStyle.Instance.font != null) text.font = InsectShowcaseStyle.Instance.font;
        text.fontSize = CostFontSizeAt(scale);
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.margin = Vector4.zero;
        InCostSlot(text.rectTransform);
        text.transform.SetAsLastSibling();
    }
}
