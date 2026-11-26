using UnityEngine;
using TMPro;

// every sprite, colour and size of the loadout screen's insect panel (InsectShowcase), from Tommy's
// pixel kit: the plant info panel's pieces (Sprites/UI/InfoPanel), the stat icons (Sprites/UI/icons)
// and the element icons. lives at Resources/InsectShowcaseStyle.asset so tuning it in the inspector
// changes the panel the next time it opens. sizes are in art pixels: 1 art pixel = 2 canvas units
[CreateAssetMenu(fileName = "InsectShowcaseStyle", menuName = "Scriptable Objects/Insect Showcase Style")]
public class InsectShowcaseStyle : ScriptableObject
{
    private static InsectShowcaseStyle _instance;
    public static InsectShowcaseStyle Instance
    {
        get
        {
            if (_instance == null) _instance = Resources.Load<InsectShowcaseStyle>("InsectShowcaseStyle");
            if (_instance == null) _instance = CreateInstance<InsectShowcaseStyle>();
            return _instance;
        }
    }

    [Header("Kit (the plant info panel's pieces)")]
    public TMP_FontAsset font;
    [Tooltip("12 by 12, sliced 4: the panel and tooltip body, tinted with the panel colour")]
    public Sprite panelBase;
    [Tooltip("drawn over panelBase, untinted")]
    public Sprite panelShine;
    [Tooltip("the in game right panel's wood (ui_panel_tall_base, Tommy 2026-11-25): the loadout screen's Insects, " +
        "Current Loadout and Available Plants panels and the plant tooltip wear it too, tinted with the level's " +
        "biome colour (GameHUD) like the right panel (TallPanelLook)")]
    public Sprite tallPanelBase;
    [Tooltip("its gold trim and corner scrollwork (ui_panel_tall_trim), drawn over the wood untinted")]
    public Sprite tallPanelTrim;
    [Tooltip("the fertilizer cards' seed packet interior (Tommy, 2026-11-26, seed_packet_interior): greyscale parchment " +
        "at the back of the card, tinted with the fertilizer's element colour (FertilizerCard)")]
    public Sprite seedPacketBase;
    [Tooltip("the packet's frame (seed_packet_frame: wood, gold corners, vines), drawn as is over the card's contents")]
    public Sprite seedPacketTrim;
    [Tooltip("the 4 by 2 line under the title")]
    public Sprite seam;
    [Tooltip("8 by 8, sliced 2: the insect cards and the ability box, tinted with the panel colour")]
    public Sprite insetBox;
    [Tooltip("8 by 8, sliced 2: the chips (conditions, tags) and the stat cells, tinted with the panel colour")]
    public Sprite stripSunken;
    [Tooltip("Tommy's section frame (ui_frame_wood_leaves_grey, 2026-11-26): 45 by 44, greyscale wood with light corner " +
        "nails, sliced 6, drawn over the edge of every box that sections text (SectionFrameLook)")]
    public Sprite sectionFrame;
    [Tooltip("how far a section frame's colour is its box's colour lightened towards white (proposal 0.45: a frame in the " +
        "box's own colour all but vanishes into it)")]
    [Range(0f, 1f)] public float sectionFrameLighten = 0.45f;

    [Header("Kit pieces the escape menu uses (SettingsSkin)")]
    [Tooltip("8 by 11, sliced 2: a volume bar's frame, as the plant info panel's health bar")]
    public Sprite barFrame;
    [Tooltip("4 by 9: a volume bar's fill, stretched")]
    public Sprite barFill;
    [Tooltip("drawn over barFill, untinted")]
    public Sprite barFillShine;
    [Tooltip("28 by 28, sliced 2: an indicator toggle's box")]
    public Sprite toggleBox;
    [Tooltip("15 by 15: an indicator toggle's tick (the bloom pip)")]
    public Sprite toggleTick;
    [Tooltip("the volume bars' fill")]
    public Color volumeFill = Hex("#5ED45E");
    [Tooltip("a ticked toggle's pip")]
    public Color toggleTickColor = Hex("#5ED45E");

    [Header("Stat icons (Sprites/UI/icons, 16 by 16)")]
    public Sprite health;
    public Sprite movementSpeed;
    public Sprite armor;
    public Sprite magicArmor;
    public Sprite attackDamage;
    public Sprite attackSpeed;

    [Header("Element icons, for the resistances")]
    public Sprite fire;
    public Sprite water;
    public Sprite grass;
    public Sprite poison;
    public Sprite ice;
    public Sprite wind;

    [Header("Colours")]
    [Tooltip("the panel, cards, chips and tooltips are this colour (proposal: a brick red for the enemy side)")]
    public Color panel = Hex("#7A3B2A");
    [Tooltip("the 1 pixel shadow under every text; the plant info panel uses the panel colour x 0.38")]
    public Color textShadow = Hex("#2E1610");
    public Color text = Hex("#FFF1D6");
    [Tooltip("the tier, the tooltip categories, a 0 resistance, an ability of None")]
    public Color mutedText = Hex("#D2BC9C");
    [Tooltip("the word Ability over the ability")]
    public Color label = Hex("#FFD84A");
    [Tooltip("the NEW and Recommended badges")]
    public Color badge = Hex("#FFC939");
    public Color badgeText = Hex("#4A2814");
    [Tooltip("the portrait's backdrop is the insect's name colour lightened this much toward white")]
    [Range(0f, 1f)] public float portraitLighten = 0.6f;
    [Tooltip("the Weaknesses title in the insect tooltip (elements below 0: the insect takes more damage from them)")]
    public Color weak = Hex("#5ED45E");
    [Tooltip("the Resistances title in the insect tooltip (elements above 0: the insect takes less damage from them)")]
    public Color resists = Hex("#FF7A66");

    [Header("Plant sections (Current Loadout, Available Plants): the same panel, brighter")]
    [Tooltip("their panels, and the inset boxes behind the two plant grids (proposal: a bright leaf green)")]
    public Color plantPanel = Hex("#5DB33A");
    [Tooltip("the 1 pixel shadow under the two section titles (the panel colour x 0.38)")]
    public Color plantTitleShadow = Hex("#234416");
    [Tooltip("art pixels of panel around each section's title and grid (less between the two when they're close)")]
    public int plantPanelPad = 10;

    [Header("Tag colours")]
    public Color passive = Hex("#C9BFA2");
    public Color mildlyAggressive = Hex("#FFB973");
    public Color aggressive = Hex("#FF7A66");
    public Color flying = Hex("#8FD0FF");

    [Header("Element colours (the resistance names)")]
    public Color fireText = Hex("#FFA500");
    public Color waterText = Hex("#4FC3F7");
    public Color grassText = Hex("#7CC04A");
    public Color poisonText = Hex("#C07BE0");
    public Color iceText = Hex("#00FFFF");
    public Color windText = Hex("#B2EBF2");

    [Header("Sizes (art pixels; 1 art pixel = 2 canvas units)")]
    public int panelWidth = 240;
    [Tooltip("space between the insect panel and the screen's left, top and bottom edges, and between the plant " +
        "tooltip and the right, top and bottom ones. 0: both hug their side (Tommy). the plant tooltip is the screen's " +
        "whole height; the insect panel is as tall as its insects need, up to the whole height, then scrolls")]
    public int margin = 0;
    public int portraitSize = 36;
    public int tooltipWidth = 250;
    public int chipTooltipWidth = 180;
    [Tooltip("canvas units: the font's native 10 pixels")]
    public float textSize = 20f;
    [Tooltip("canvas units: twice the native size")]
    public float titleSize = 40f;
    [Tooltip("canvas units: the Recommended badge on a plant square, the size of the square's own plant name")]
    public float plantBadgeTextSize = 14f;

    public Sprite ElementIcon(ElementalType element) => element switch
    {
        ElementalType.Fire => fire,
        ElementalType.Water => water,
        ElementalType.Grass => grass,
        ElementalType.Poison => poison,
        ElementalType.Ice => ice,
        ElementalType.Wind => wind,
        _ => null
    };

    public Color ElementText(ElementalType element) => element switch
    {
        ElementalType.Fire => fireText,
        ElementalType.Water => waterText,
        ElementalType.Grass => grassText,
        ElementalType.Poison => poisonText,
        ElementalType.Ice => iceText,
        ElementalType.Wind => windText,
        _ => text
    };

    private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
}
