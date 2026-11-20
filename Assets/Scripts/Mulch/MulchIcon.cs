using UnityEngine;
using UnityEngine.UI;

// tommy's paper bag mulch icon (Downloads\gd_mulch_icons): three greyscale 32 by 32 layers in one
// rect, each tinted: the bag brown (MulchRegistry.iconBagColor), the label the rarity colour (the
// game's own, MulchTierColors, as the readme asks), and the mulch's symbol (MulchData.symbol, only
// some have one yet) a pale version of the rarity colour. Show draws it on a ui Image: the image is
// the bag and two child images it makes the first time are the label and the symbol. MulchDrop
// draws the same layers with sprite renderers. without a bag in the registry the old single icon
// shows instead (MulchData.icon or the placeholder, tinted by rarity)
public static class MulchIcon
{
    // how much of the rarity colour the symbol keeps: per channel 255 - (255 - rarity) * 0.18
    public const float SymbolPaleness = 0.18f;
    private const string LabelName = "MulchLabel";
    private const string SymbolName = "MulchSymbol";

    private static MulchRegistry loaded;
    public static MulchRegistry Registry
    {
        get
        {
            if (MulchManager.instance != null && MulchManager.instance.Registry != null) return MulchManager.instance.Registry;
            if (loaded == null) loaded = Resources.Load<MulchRegistry>("MulchRegistry");
            return loaded;
        }
    }

    public static bool HasBag => Registry != null && Registry.iconBag != null;
    public static Color BagColor => Registry != null ? Registry.iconBagColor : new Color32(0xC4, 0x74, 0x14, 0xFF);
    public static Color LabelColor(MulchTier rarity) => MulchTierColors.Get(rarity);

    public static Color SymbolColor(MulchTier rarity)
    {
        Color c = LabelColor(rarity);
        return new Color(1f - (1f - c.r) * SymbolPaleness, 1f - (1f - c.g) * SymbolPaleness, 1f - (1f - c.b) * SymbolPaleness, 1f);
    }

    // the old look: one sprite, tinted by rarity
    public static Sprite OldIcon(MulchItem item) =>
        item.data.icon != null ? item.data.icon : Registry != null ? Registry.placeholderIcon : null;

    public static void Show(Image image, MulchItem item, float alpha = 1f)
    {
        if (image == null) return;
        if (item == null || item.data == null) { Hide(image); return; }
        image.enabled = true;
        if (!HasBag)
        {
            image.sprite = OldIcon(item);
            image.color = WithAlpha(MulchTierColors.Get(item.rarity), alpha);
            SetLayer(image, LabelName, null, Color.clear);
            SetLayer(image, SymbolName, null, Color.clear);
            return;
        }
        image.sprite = Registry.iconBag;
        image.color = WithAlpha(BagColor, alpha);
        SetLayer(image, LabelName, Registry.iconLabel, WithAlpha(LabelColor(item.rarity), alpha));
        SetLayer(image, SymbolName, item.data.symbol, WithAlpha(SymbolColor(item.rarity), alpha));
    }

    public static void Hide(Image image)
    {
        if (image == null) return;
        image.enabled = false;
        SetLayer(image, LabelName, null, Color.clear);
        SetLayer(image, SymbolName, null, Color.clear);
    }

    // a layer over the bag, the bag's whole rect, made the first time it's needed. no sprite: hidden
    private static void SetLayer(Image bag, string name, Sprite sprite, Color color)
    {
        Transform existing = bag.transform.Find(name);
        Image layer = existing != null ? existing.GetComponent<Image>() : null;
        if (sprite == null)
        {
            if (layer != null) layer.enabled = false;
            return;
        }
        if (layer == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = bag.gameObject.layer;
            go.transform.SetParent(bag.transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            layer = go.AddComponent<Image>();
            layer.raycastTarget = false;
        }
        layer.preserveAspect = bag.preserveAspect;
        layer.sprite = sprite;
        layer.color = color;
        layer.enabled = true;
    }

    private static Color WithAlpha(Color c, float a) { c.a *= a; return c; }
}
