using UnityEngine;

// every sprite and colour the Skill Tree screen's look adds (2026-11-24, Tommy's pixel kit), next to
// the kit pieces it borrows from InsectShowcaseStyle (panel, shine, seam, strips, bars, font) and
// PlantInfoPanelStyle (skill frames, plant bar portrait, padlock, sun, pips). lives at
// Resources/SkillTreeStyle.asset so tuning it in the inspector changes the screen the next time it
// opens. the screen's layout (where every plant row, text, node and button sits) is the scene's and the
// prefabs'; this only dresses it. 1 art pixel = 2 canvas units
[CreateAssetMenu(fileName = "SkillTreeStyle", menuName = "Scriptable Objects/Skill Tree Style")]
public class SkillTreeStyle : ScriptableObject
{
    private static SkillTreeStyle _instance;
    public static SkillTreeStyle Instance
    {
        get
        {
            if (_instance == null) _instance = Resources.Load<SkillTreeStyle>("SkillTreeStyle");
            if (_instance == null) _instance = CreateInstance<SkillTreeStyle>();
            return _instance;
        }
    }

    [Header("Sprites")]
    [Tooltip("1 by 1 white: the vine links between nodes, a node's window, the dimming over a locked node")]
    public Sprite white;
    [Tooltip("40 by 40, the round 6 locked skill vines: over a locked node's frame")]
    public Sprite vinesSquare;
    [Tooltip("a node whose stat is attack range shows this (the other stat icons come from InsectShowcaseStyle)")]
    public Sprite attackRange;

    [Header("Screen")]
    [Tooltip("the board behind everything, panel_base with its shine (proposal: the kit previews' dark slate, a little green)")]
    public Color board = Hex("#26302B");
    [Tooltip("the box around Unspent Points, Confirm and Reset (proposal: the neutral fertilizer card's earthy brown)")]
    public Color dock = Hex("#7A5230");
    [Tooltip("the scroll bars' track and handle")]
    public Color scrollTrack = Hex("#161C19");
    public Color scrollHandle = Hex("#C8A070");
    [Tooltip("Reset's plank (Confirm uses the game controls' green Start colours)")]
    public GameControlsStyle.ButtonColors resetPlank = new GameControlsStyle.ButtonColors(
        Hex("#D0583A"), Hex("#EE7C5C"), Hex("#A8402A"), Hex("#FFF1E8"), Hex("#4E170C"));

    [Header("Plant cards")]
    [Tooltip("the Exp bar's fill (proposal: the wave timer's blue)")]
    public Color expFill = Hex("#4FC3F7");
    [Tooltip("the number of points a plant can spend, and the Level number, when above 0")]
    public Color points = Hex("#FFD84A");
    [Tooltip("the word Points, and a plant's points at 0")]
    public Color mutedText = Hex("#D2BC9C");

    [Header("Nodes")]
    [Tooltip("the window in a node's vine frame, by state. one that can be picked takes its plant's portrait backdrop colour")]
    public Color windowLocked = Hex("#3A3A3A");
    public Color windowPending = Hex("#A8ECFF");
    public Color windowConfirmed = Hex("#FFE38A");
    [Tooltip("the rank under a node, by state")]
    public Color rankLocked = Hex("#9C9C9C");
    public Color rankAvailable = Hex("#FFF1D6");
    public Color rankPending = Hex("#33E6FF");
    public Color rankConfirmed = Hex("#FFD84A");
    [Tooltip("the 1 pixel shadow under the ranks and the outline round the links")]
    public Color outline = Hex("#1A1208");
    [Tooltip("no longer used (2026-11-25): a locked node's frame and icon are darkened with " +
        "PlantInfoPanelStyle.lockedSlotTint, like a locked plant slot, instead of this black over them")]
    [Range(0f, 1f)] public float lockedDim = 0.58f;

    [Header("Links between nodes (2 art px wide with a 1 px outline)")]
    [Tooltip("a link into a node that can be picked, is picked or is bought (the round 6 vines' green)")]
    public Color linkLit = Hex("#8FD25C");
    [Tooltip("a link into a node still locked")]
    public Color linkDark = Hex("#4E5A50");

    public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
}
