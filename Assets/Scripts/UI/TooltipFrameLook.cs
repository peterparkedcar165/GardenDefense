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
}
