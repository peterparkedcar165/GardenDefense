using UnityEngine;

// every sprite, colour and timing of the plant info panel that isn't per element, shared by the
// panel (PlantUpgradeUI) and its buttons (PixelButton). lives at Resources/PlantInfoPanelStyle.asset
// so tuning it in the inspector changes the panel at once. spec: Downloads\gd_plant_info_panel\README.md
// (and Downloads\gd_upgrade_pips for the pips). the per element colours (panel, text shadow,
// portrait) are in PlantData next to the other element colours
[CreateAssetMenu(fileName = "PlantInfoPanelStyle", menuName = "Scriptable Objects/Plant Info Panel Style")]
public class PlantInfoPanelStyle : ScriptableObject
{
    private static PlantInfoPanelStyle _instance;
    public static PlantInfoPanelStyle Instance
    {
        get
        {
            if (_instance == null) _instance = Resources.Load<PlantInfoPanelStyle>("PlantInfoPanelStyle");
            if (_instance == null) _instance = CreateInstance<PlantInfoPanelStyle>();
            return _instance;
        }
    }

    [System.Serializable]
    public struct ButtonLook
    {
        public Color face;
        public Color text;
        [Tooltip("the 1 pixel shadow under the text and glyphs. alpha 0: no shadow")]
        public Color shadow;
        public Color sun;
        [Tooltip("off hides the sun glyph (max)")]
        public bool showSun;

        public ButtonLook(string face, string text, string shadow, string sun, bool showSun = true)
        {
            this.face = Hex(face);
            this.text = Hex(text);
            this.shadow = string.IsNullOrEmpty(shadow) ? Color.clear : Hex(shadow);
            this.sun = Hex(sun);
            this.showSun = showSun;
        }
    }

    [System.Serializable]
    public struct SkillFrame
    {
        public ElementalType element;
        [Tooltip("the 40 by 40 vine frame (Downloads\\gd_skill_frames)")]
        public Sprite frame;
        [Tooltip("shown in the frame when the plant's PlantData has no skill icon")]
        public Sprite fallbackIcon;
    }

    [Header("Text")]
    [Tooltip("name, stats, path labels and \"Mulches\". the shadow under them is the element's (PlantData.ElementTextShadowColor)")]
    public Color text = Hex("#FFF1D6");
    [Tooltip("the numbers on the bars, with a 1 pixel outline")]
    public Color barText = Color.white;
    public Color barOutline = Hex("#141414");

    [Header("Pips (path colours are Tommy's)")]
    public Sprite pipEmpty;
    public Sprite pipNext;
    public Sprite pipFilled;
    public Sprite pipMax;
    public Color pathAttack = Hex("#5ED45E");
    public Color pathPassive = Hex("#FFD84A");
    public Color pathSkill = Hex("#33E6FF");
    [Tooltip("seconds for one pulse of the next buyable pip")]
    public float nextPulseSeconds = 1.2f;
    [Range(0f, 1f)] public float nextPulseMin = 0.45f;
    [Range(0f, 1f)] public float nextPulseMax = 1f;
    [Tooltip("a path that isn't unlocked yet shows its empty pips at this alpha")]
    [Range(0f, 1f)] public float lockedPathAlpha = 0.55f;
    [Tooltip("pips only reached through a temporary level bonus show filled at this alpha")]
    [Range(0f, 1f)] public float bonusPipAlpha = 0.35f;
    [Tooltip("seconds a just bought pip shows the max sprite")]
    public float buyFlashSeconds = 0.2f;
    [Tooltip("optional: at 5/5 the max pips breathe instead of staying solid")]
    public bool breatheMax = false;
    public float maxBreathSeconds = 1.4f;
    [Range(0f, 1f)] public float maxBreathMin = 0.55f;

    [Header("Buttons")]
    public Sprite button;
    public Sprite buttonPressed;
    [Tooltip("the 7 by 7 sun after a price drawn in code (the mulch Sell popup), tinted with the look's sun colour")]
    public Sprite sunGlyph;
    [Tooltip("can buy")]
    public ButtonLook normal = new ButtonLook("#FFB973", "#FFF1D6", "#4A2814", "#FFC939");
    [Tooltip("pointer over")]
    public ButtonLook hover = new ButtonLook("#FFDBA2", "#FFF1D6", "#4A2814", "#FFC939");
    [Tooltip("pointer down, with the pressed sprite")]
    public ButtonLook pressed = new ButtonLook("#FFB973", "#FFF1D6", "#4A2814", "#FFC939");
    [Tooltip("path not unlocked yet, or can't afford")]
    public ButtonLook locked = new ButtonLook("#8D8D8D", "#9C9C9C", "#3A3A3A", "#9B9B9B");
    [Tooltip("path at 5/5")]
    public ButtonLook max = new ButtonLook("#E5E5E5", "#D0D0D0", "", "#9B9B9B", false);

    [Header("Skill")]
    public SkillFrame[] skillFrames;
    [Tooltip("darkens the skill icon while it recharges or is silenced")]
    public Color cooldownOverlay = new Color(0f, 0f, 0f, 0.55f);

    [Header("Locked skill (round 6): vines and a padlock over the Skill strip and the skill square")]
    public Color lockedVines = Hex("#8FD25C");
    [Tooltip("while the pointer is over the Skill strip")]
    public Color lockedVinesHover = Hex("#A5DB7C");
    [Tooltip("the padlocks and the unlock price. while the unlock can't be afforded the price and the sun take " +
        "the locked button look's text and sun colours; otherwise the sun is the normal look's")]
    public Color lockedText = Hex("#FFF1D6");
    public Color lockedTextHover = Color.white;

    [Header("Plant slots: the squares of the loadout screen and the plant bar (Tommy, 2026-11-22)")]
    [Tooltip("the bevel frame the plant bar's buttons use (portrait_bevel_grey)")]
    public Sprite slotFrame;
    [Tooltip("the backdrop in the frame's window (portrait_backdrop_grey)")]
    public Sprite slotBackdrop;
    [Tooltip("over a slot that isn't unlocked yet, tinted with lockedText")]
    public Sprite slotPadlock;
    [Tooltip("frame and backdrop of a slot that isn't unlocked yet")]
    public Color lockedSlotTint = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Header("Level bars (the colours the old temperature and oxygen bars had)")]
    public Color oxygen = Hex("#B2EBF2");
    public Color oxygenEmpty = Color.red;
    public Color temperatureCold = Color.cyan;
    public Color temperatureComfort = Color.green;
    public Color temperatureHot = new Color(1f, 0.5f, 0f);
    [Tooltip("the two marks on the temperature bar where the plant's comfort range starts and ends")]
    public Color comfortMark = Hex("#141414");

    public Color PathColor(int path) => path == 0 ? pathAttack : path == 1 ? pathPassive : pathSkill;

    public SkillFrame FrameFor(ElementalType element)
    {
        if (skillFrames != null)
        {
            foreach (SkillFrame f in skillFrames)
                if (f.element == element) return f;
            // neutral and anything without its own frame borrows the first one
            if (skillFrames.Length > 0) return skillFrames[0];
        }
        return default;
    }

    private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
}
