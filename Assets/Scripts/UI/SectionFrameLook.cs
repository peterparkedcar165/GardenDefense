using UnityEngine;
using UnityEngine.UI;

// Tommy's section frame (2026-11-26, ui_frame_wood_leaves_grey: 45 by 44, greyscale wood with light corner
// nails and a clear middle) around every box that sections text: the loadout screen's insect cards and the
// insect tooltip's Ability, Weaknesses and Resistances boxes, the plant tooltip's Attack, Passive, Skill and
// element boxes, the fertilizer cards' description and the plant info panel's Attack, Passive and Skill
// tooltips (their description, upgrades and max level bonus). it lies over its box's edge, in the box's own
// colour lightened (InsectShowcaseStyle.sectionFrameLighten), its corners kept and its edges stretched
// along the grain (sprite borders 6), 2 canvas units an art pixel. the sprite is InsectShowcaseStyle.sectionFrame
public static class SectionFrameLook
{
    // art pixels of frame inside a box's edge: the outline, the 3 px wood and the inner outline
    public const int Border = 5;
    // canvas units a box's contents keep from its edge: the frame and 3 art px of room
    public const float Inset = 16f;

    private const string ChildName = "SectionFrame";

    public static bool Available => InsectShowcaseStyle.Instance.sectionFrame != null;

    // the frame on a box, in the box's colour: a SectionFrame child stretched over it, drawn over everything
    // the box already holds and kept out of any layout group. running it again just recolours it
    public static Image Add(Image box) => box == null ? null : Add(box.rectTransform, box.color);

    public static Image Add(RectTransform box, Color boxColor)
    {
        InsectShowcaseStyle style = InsectShowcaseStyle.Instance;
        if (box == null || style.sectionFrame == null) return null;
        Transform found = box.Find(ChildName);
        Image frame = found != null ? found.GetComponent<Image>() : null;
        if (frame == null)
        {
            GameObject go = new GameObject(ChildName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.layer = box.gameObject.layer;
            RectTransform r = (RectTransform)go.transform;
            r.SetParent(box, false);
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            frame = go.GetComponent<Image>();
            frame.raycastTarget = false;
        }
        frame.sprite = style.sectionFrame;
        frame.type = Image.Type.Sliced;
        frame.fillCenter = false;
        frame.pixelsPerUnitMultiplier = 1f;
        frame.color = ColorFor(boxColor);
        frame.transform.SetAsLastSibling();
        return frame;
    }

    // a box that changed colour gives its frame the new one
    public static void Recolor(Image box)
    {
        if (box == null) return;
        Transform found = box.transform.Find(ChildName);
        Image frame = found != null ? found.GetComponent<Image>() : null;
        if (frame != null) frame.color = ColorFor(box.color);
    }

    public static Color ColorFor(Color boxColor)
    {
        Color c = Color.Lerp(boxColor, Color.white, InsectShowcaseStyle.Instance.sectionFrameLighten);
        c.a = 1f;
        return c;
    }
}
