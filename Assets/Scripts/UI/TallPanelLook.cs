using UnityEngine;
using UnityEngine.UI;

// the in game right panel's look (Tommy, 2026-11-25: "the elegant one on the right"), for other panels:
// ui_panel_tall_base, greyscale wood sliced and tinted with the level's biome colour like the right panel
// (GameHUD.TryGetWood), and ui_panel_tall_trim over it untinted, its gold lines and corner scrollwork.
// the loadout screen's Insects, Current Loadout and Available Plants panels and its plant tooltip wear it.
// the sprites are on InsectShowcaseStyle (tallPanelBase, tallPanelTrim)
public static class TallPanelLook
{
    // art pixels a panel's contents keep from its edge: the frame's green band and gold lines are 7 px,
    // and the corner scrollwork curls in to about 9 px
    public const int Inset = 10;

    // the daytime green, when there's no level (or its biome has no colour)
    private static readonly Color DefaultWood = new Color32(0x55, 0x7D, 0x46, 0xFF);

    public static Color Wood
    {
        get
        {
            if (GameHUD.instance != null && GameHUD.instance.TryGetWood(ProceduralLevel.CurrentConfig, out Color wood)) return wood;
            return DefaultWood;
        }
    }

    public static bool Available => InsectShowcaseStyle.Instance.tallPanelBase != null;

    // the panel's own Image becomes the wood; a Trim child (stretched over it, first child, out of any
    // layout group) is made or found and given the trim. any old Shine child is switched off
    public static void Apply(Image body)
    {
        InsectShowcaseStyle style = InsectShowcaseStyle.Instance;
        if (body == null || style.tallPanelBase == null) return;
        body.sprite = style.tallPanelBase;
        body.type = Image.Type.Sliced;
        body.pixelsPerUnitMultiplier = 1f;
        body.color = Wood;

        Transform shine = body.transform.Find("Shine");
        if (shine != null) shine.gameObject.SetActive(false);
        if (style.tallPanelTrim == null) return;

        Transform found = body.transform.Find("Trim");
        Image trim = found != null ? found.GetComponent<Image>() : null;
        if (trim == null)
        {
            GameObject go = new GameObject("Trim", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.layer = body.gameObject.layer;
            RectTransform r = (RectTransform)go.transform;
            r.SetParent(body.transform, false);
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            trim = go.GetComponent<Image>();
            trim.raycastTarget = false;
        }
        trim.sprite = style.tallPanelTrim;
        trim.type = Image.Type.Sliced;
        trim.pixelsPerUnitMultiplier = 1f;
        trim.color = Color.white;
        trim.transform.SetAsFirstSibling();
    }
}
