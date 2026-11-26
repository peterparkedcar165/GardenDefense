using UnityEngine;
using UnityEngine.UI;

// the plant portraits' frame (Tommy, 2026-11-26, Downloads/gd_frame_portrait, rebuilt to his 80 by 80
// with an 8 px band and a clear 64 by 64 opening: Sprites/UI/PortraitFrame/ui_frame_portrait_80_*), on the
// plant info panel's portrait and the loadout screen's plant tooltip. drawn whole at 2 canvas units an art
// pixel (160 by 160), so a 64 px plant shows at exactly the panel's own pixel size in the 128 square
// opening. three layers: the backdrop behind the plant (portrait_backdrop_64, tinted with the element's
// backdrop colour), the plant, and the frame over it, its grey band tinted with the element's colour and
// its gold corner brackets as drawn. sprites on PlantInfoPanelStyle (portraitBackdrop, portraitFrameBand,
// portraitFrameTrim)
public static class PortraitFrameLook
{
    public const float Px = 2f;
    // the frame, and the opening the plant shows in, in canvas units from the frame's top left
    public const float Size = 80f * Px;
    public static readonly Rect Opening = new Rect(8f * Px, 8f * Px, 64f * Px, 64f * Px);

    public static bool Available
    {
        get
        {
            PlantInfoPanelStyle s = PlantInfoPanelStyle.Instance;
            return s.portraitFrameBand != null;
        }
    }

    // the backdrop: an Image the opening's size
    public static void MakeBackdrop(Image image)
    {
        Sprite backdrop = PlantInfoPanelStyle.Instance.portraitBackdrop;
        if (image == null || backdrop == null) return;
        image.sprite = backdrop;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.pixelsPerUnitMultiplier = 1f;
    }

    // the frame: its band on this Image (tinted by the caller) and a Trim child, the gold brackets
    public static void MakeFrame(Image image)
    {
        PlantInfoPanelStyle s = PlantInfoPanelStyle.Instance;
        if (image == null) return;
        image.sprite = s.portraitFrameBand;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.pixelsPerUnitMultiplier = 1f;
        if (s.portraitFrameTrim == null || image.transform.Find("Trim") != null) return;
        GameObject go = new GameObject("Trim", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.layer = image.gameObject.layer;
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(image.transform, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        go.GetComponent<LayoutElement>().ignoreLayout = true;
        Image trim = go.GetComponent<Image>();
        trim.sprite = s.portraitFrameTrim;
        trim.type = Image.Type.Simple;
        trim.raycastTarget = false;
    }

    // a top left anchored rect: (x, y) canvas units from its parent's top left, w by h
    public static void TopLeft(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(x, -y);
        r.sizeDelta = new Vector2(w, h);
    }
}
