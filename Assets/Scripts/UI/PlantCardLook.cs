using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Tommy's plant card (2026-11-25), shared by the loadout screen's squares (LoadoutSlotSquare) and the
// plant bar's buttons (PlantSlotButton): the greyscale wood tinted with the plant's element panel colour,
// the trim (vines, gold corners, the dark plant window and the cost slot) untinted over it, then the plant
// in the window and its cost in the slot. drawn at its own pixel size, 1 canvas unit an art px. the sprites
// are PlantInfoPanelStyle.plantCardBase and plantCardTrim
public static class PlantCardLook
{
    // canvas units
    public static readonly Vector2 Size = new Vector2(92f, 117f);
    // the plant window (art px 15 to 76 across, 15 to 80 down) and the cost slot (16 to 75 across, 89 to
    // 102 down), as offsets from the card's centre
    public static readonly Vector2 WindowCentre = new Vector2(0f, 10.5f);
    public static readonly Vector2 WindowSize = new Vector2(62f, 66f);
    public static readonly Vector2 CostCentre = new Vector2(0f, -37.5f);
    public static readonly Vector2 CostSize = new Vector2(60f, 14f);
    // the plant at its own 64 px; the cost a little under the font's native size so it fits the slot
    public const float IconSize = 64f;
    public const float CostFontSize = 16f;

    // an empty or open card's wood (proposal: the medium button's original wood)
    public static readonly Color NeutralWood = new Color32(0x7D, 0x44, 0x29, 0xFF);

    public static Color Wood(PlantData data) => data != null ? PlantData.ElementPanelColor(data.elementalType) : NeutralWood;

    // a rect centred on an offset from the card's centre
    public static void Place(RectTransform rect, Vector2 centre, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = centre;
        rect.sizeDelta = size;
    }

    public static void PlaceInWindow(RectTransform rect) => Place(rect, WindowCentre, new Vector2(IconSize, IconSize));

    public static void SetSprites(Image wood, Image trim)
    {
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        if (wood != null && style.plantCardBase != null) { wood.sprite = style.plantCardBase; wood.type = Image.Type.Simple; wood.preserveAspect = false; }
        if (trim != null && style.plantCardTrim != null) { trim.sprite = style.plantCardTrim; trim.type = Image.Type.Simple; trim.preserveAspect = false; }
    }

    // the cost text in the card's slot, made the first time and found again after
    public static TextMeshProUGUI Cost(Transform card)
    {
        Transform found = card.Find("Cost");
        if (found != null) return found.GetComponent<TextMeshProUGUI>();
        GameObject go = new GameObject("Cost", typeof(RectTransform));
        go.layer = card.gameObject.layer;
        go.transform.SetParent(card, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        StyleCost(text);
        return text;
    }

    public static void StyleCost(TMP_Text text)
    {
        if (InsectShowcaseStyle.Instance.font != null) text.font = InsectShowcaseStyle.Instance.font;
        text.fontSize = CostFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.margin = Vector4.zero;
        Place(text.rectTransform, CostCentre, CostSize);
        text.transform.SetAsLastSibling();
    }
}
