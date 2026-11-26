using UnityEngine;
using UnityEngine.UI;

// Tommy's round vine frame on the plant portraits (2026-11-26, Sprites/UI/VineFrames): the plant info
// panel's portrait and the loadout screen's plant tooltip. three layers, all 51 by 48 art pixels stretched
// to the portrait's own size (Tommy: the portraits stay their original size, the frame stretched to fit):
// the inside, grey wood planks tinted with the element's backdrop colour; the plant, its original size,
// in the middle; and the frame over it, its grey band tinted with the element's colour and its vines and
// gold as drawn. sprites on PlantInfoPanelStyle (portraitInterior, portraitFrameBand, portraitFrameTrim)
public static class VinePortraitLook
{
    public static bool Available
    {
        get
        {
            PlantInfoPanelStyle s = PlantInfoPanelStyle.Instance;
            return s.portraitFrameBand != null && s.portraitInterior != null;
        }
    }

    // the inside: an Image the frame's size
    public static void MakeInterior(Image image)
    {
        if (image == null) return;
        image.sprite = PlantInfoPanelStyle.Instance.portraitInterior;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.pixelsPerUnitMultiplier = 1f;
    }

    // the frame: its band on this Image (tinted by the caller) and a Trim child, the vines and gold
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

    // a new empty child rect for the plant, inside parent (which is the frame's size): window is its place
    // in canvas units from the parent's top left
    public static RectTransform NewWindow(Transform parent, Rect window)
    {
        GameObject go = new GameObject("Window", typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        TopLeft(r, window.x, window.y, window.width, window.height);
        return r;
    }
}
