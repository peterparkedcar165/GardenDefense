using UnityEngine;
using UnityEngine.UI;

// Tommy's flower frame (2026-11-26, ui_frame_flowers: 49 by 48, a wooden frame with a gold line, pink
// flowers on its corners and a dark inside) on every tooltip panel: the plant info panel's hover
// tooltip (stats, Attack, Passive, Skill, the locked skill), the stats and status effect panels, the
// mulch tooltip, the skill tree's tooltip, the loadout screen's insect, tag and locked slot tooltips.
// drawn as is (not tinted), its corners kept and its edges and inside repeated (sprite borders 13,
// tiled), 2 canvas units an art pixel. the sprite is PlantInfoPanelStyle.tooltipFrame
public static class TooltipFrameLook
{
    // art pixels of frame on each side: 2 clear, then the 5 px wood and gold band
    public const int Frame = 7;
    // canvas units a panel's contents keep from its edge (10 art px), clear of the band and the flowers
    public const float Inset = 20f;

    public static bool Available => PlantInfoPanelStyle.Instance.tooltipFrame != null;

    public static void Apply(Image image)
    {
        Sprite frame = PlantInfoPanelStyle.Instance.tooltipFrame;
        if (image == null || frame == null) return;
        image.sprite = frame;
        image.type = Image.Type.Tiled;
        image.pixelsPerUnitMultiplier = 1f;
        image.color = Color.white;
        // the kit panels these replace had a shine drawn over them
        Transform shine = image.transform.Find("Shine");
        if (shine != null) shine.gameObject.SetActive(false);
    }

    // the frame with its inside in a colour (Tommy, 2026-11-26: the info panel's tooltips in the plant's
    // element colour): the panel's own Image is the inside alone (tooltipInterior, the info panel wood's
    // grey, so the tint comes out the panel's own shade), and a FlowerFrame child, its first, is the hollow
    // frame over it, drawn as is. without those two sprites it is the plain frame above
    private const string HollowName = "FlowerFrame";

    public static void Apply(Image image, Color interior)
    {
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        if (style.tooltipInterior == null || style.tooltipFrameHollow == null)
        {
            Apply(image);
            return;
        }
        if (image == null) return;
        image.sprite = style.tooltipInterior;
        image.type = Image.Type.Tiled;
        image.pixelsPerUnitMultiplier = 1f;
        image.color = interior;
        Transform shine = image.transform.Find("Shine");
        if (shine != null) shine.gameObject.SetActive(false);

        Transform found = image.transform.Find(HollowName);
        Image frame = found != null ? found.GetComponent<Image>() : null;
        if (frame == null)
        {
            GameObject go = new GameObject(HollowName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.layer = image.gameObject.layer;
            RectTransform r = (RectTransform)go.transform;
            r.SetParent(image.transform, false);
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            frame = go.GetComponent<Image>();
            frame.raycastTarget = false;
        }
        frame.sprite = style.tooltipFrameHollow;
        frame.type = Image.Type.Tiled;
        frame.pixelsPerUnitMultiplier = 1f;
        frame.color = Color.white;
        // over the inside, under the panel's contents
        frame.transform.SetAsFirstSibling();
    }

    // a new colour for a panel's inside (only one Apply gave an inside of its own)
    public static void SetInterior(Image image, Color interior)
    {
        if (image != null && image.sprite != null && image.sprite == PlantInfoPanelStyle.Instance.tooltipInterior)
            image.color = interior;
    }

    // the info panel's colour for what it shows: a plant's element, an insect's neutral grey (as the panel)
    public static Color InteriorFor(Entity entity) =>
        PlantData.ElementPanelColor(entity is Plant plant ? plant.elementalType : ElementalType.Neutral);
}
