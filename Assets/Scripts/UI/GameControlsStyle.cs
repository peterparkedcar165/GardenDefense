using UnityEngine;

// sprites and button colours of the game controls under the plant info panel (GameHUD,
// GameControlButton): the start, pause and resume button and the speed button. lives at
// Resources/GameControlsStyle.asset so tuning it in the inspector changes them in every level.
// spec: Downloads\gd_game_controls\README.md. the wave text, the countdown bar and its text keep
// their colours on their own objects in the Canvas prefab. layout: Bigger Planks (2026-11-21): two long
// buttons side by side, icon beside word, over a timer bar as wide as both, with the wave number at
// its left end and the countdown at its right end
[CreateAssetMenu(fileName = "GameControlsStyle", menuName = "Scriptable Objects/Game Controls Style")]
public class GameControlsStyle : ScriptableObject
{
    private static GameControlsStyle _instance;
    public static GameControlsStyle Instance
    {
        get
        {
            if (_instance == null) _instance = Resources.Load<GameControlsStyle>("GameControlsStyle");
            if (_instance == null) _instance = CreateInstance<GameControlsStyle>();
            return _instance;
        }
    }

    [System.Serializable]
    public struct ButtonColors
    {
        [Tooltip("tint of the greyscale button sprite while idle")]
        public Color face;
        [Tooltip("the label and the icon")]
        public Color text;
        [Tooltip("the copy 1 pixel under the label and the icon. about the face colour times 0.3 looks right")]
        public Color shadow;
        [Tooltip("the face's tint while hovered and while pressed (each state also has its own sprite)")]
        public Color faceHover;
        public Color facePressed;

        public ButtonColors(Color face, Color faceHover, Color facePressed, Color text, Color shadow)
        {
            this.face = face;
            this.faceHover = faceHover;
            this.facePressed = facePressed;
            this.text = text;
            this.shadow = shadow;
        }
    }

    [Header("Button sprites (Sprites/UI/GameControls)")]
    [Tooltip("Tommy's medium button (2026-11-25, ui_button_medium_base): greyscale wood tinted with the state's colour, " +
        "83 by 20, its 20 px ornaments at each end kept and the middle stretched (sliced 20 0 20 0). it has one look, so " +
        "hover and pressed use the same sprites in their own colours")]
    public Sprite buttonIdle;
    [Tooltip("the gold trim (ui_button_medium_trim), drawn over the wood untinted")]
    public Sprite buttonIdleShine;
    public Sprite buttonHover;
    public Sprite buttonHoverShine;
    [Tooltip("the icon and label drop 2 pixels while pressed")]
    public Sprite buttonPressed;
    public Sprite buttonPressedShine;
    [Tooltip("art pixels: every button's height (the sprite's own), and the room at each end the word keeps " +
        "clear of (the ornaments)")]
    public int buttonHeight = 20;
    public int buttonSidePad = 20;

    [Header("Large button (the main menu's, Tommy 2026-11-25)")]
    [Tooltip("ui_button_leafy_large_base: greyscale wood tinted with the state's colour like the medium one, 119 by 31. " +
        "drawn at least its own width; a wider one keeps its 25 px leafy ends and repeats the middle (tiled 25 0 25 0), " +
        "so the grain isn't stretched")]
    public Sprite largeButton;
    [Tooltip("ui_button_leafy_large_trim: the gold border and leafy vines, drawn over the wood untinted")]
    public Sprite largeButtonTrim;
    [Tooltip("art pixels: its height, the narrowest it's drawn (the art's own width), the room at each end the word " +
        "keeps clear of (the leaves reach 19 px in), and the word's cap top (centred on the wood between the gold " +
        "lines, rows 6 to 24)")]
    public int largeButtonHeight = 31;
    public int largeButtonWidth = 119;
    public int largeButtonSidePad = 22;
    public int largeLabelTop = 12;

    [Header("Icons")]
    public Sprite playIcon;
    public Sprite pauseIcon;
    public Sprite speedIcon;

    [Header("Inside the buttons (art pixels from the button's top; everything 2 lower while pressed)")]
    [Tooltip("the icon, this gap, then the word: one group centred on the button by their drawn pixels")]
    public int iconGap = 6;
    [Tooltip("centred on the word's capitals (rows 6 to 12 of the 20 px button), like the speed icon: play and " +
        "pause are 9 px tall (play_small, pause_small; Tommy 2026-11-25: the 15 px ones were too big for the frame)")]
    public int playIconTop = 5;
    public int pauseIconTop = 5;
    public int speedIconTop = 5;
    [Tooltip("cap top of the word (Start, Pause, Resume, 1x, and every plank button's word)")]
    public int labelTop = 6;

    [Header("Timer")]
    [Tooltip("art pixels from each end of the timer bar to the drawn pixels of \"Wave 3/8\" (left) and " +
        "\"Next wave 24s\" (right)")]
    public int timerTextInset = 6;

    [Header("Button colours")]
    [Tooltip("before the first wave")]
    public ButtonColors start = new ButtonColors(
        new Color32(0x62, 0xB8, 0x3C, 0xFF), new Color32(0x94, 0xDC, 0x5A, 0xFF), new Color32(0x42, 0x90, 0x2A, 0xFF),
        new Color32(0xF4, 0xFF, 0xE6, 0xFF), new Color32(0x1E, 0x4A, 0x14, 0xFF));
    [Tooltip("while the game runs")]
    public ButtonColors pause = new ButtonColors(
        new Color32(0xF0, 0xB4, 0x2A, 0xFF), new Color32(0xFF, 0xD8, 0x4A, 0xFF), new Color32(0xC8, 0x8A, 0x18, 0xFF),
        new Color32(0xFF, 0xFB, 0xE8, 0xFF), new Color32(0x5E, 0x3C, 0x06, 0xFF));
    [Tooltip("while paused")]
    public ButtonColors resume = new ButtonColors(
        new Color32(0x62, 0xB8, 0x3C, 0xFF), new Color32(0x94, 0xDC, 0x5A, 0xFF), new Color32(0x42, 0x90, 0x2A, 0xFF),
        new Color32(0xF4, 0xFF, 0xE6, 0xFF), new Color32(0x1E, 0x4A, 0x14, 0xFF));
    [Tooltip("a plank that leaves or undoes: the main menu's Exit (proposal: a brick red)")]
    public ButtonColors danger = new ButtonColors(
        new Color32(0xD0, 0x58, 0x3A, 0xFF), new Color32(0xEE, 0x7C, 0x5C, 0xFF), new Color32(0xA8, 0x40, 0x2A, 0xFF),
        new Color32(0xFF, 0xF1, 0xE8, 0xFF), new Color32(0x4E, 0x17, 0x0C, 0xFF));
    [Tooltip("a plank that can't be clicked: a locked level, Next with nothing picked (Tommy, 2026-11-25: grey, " +
        "not faded). the face is the info panel's locked button grey; the words lighter so they still read (proposal)")]
    public ButtonColors locked = new ButtonColors(
        new Color32(0x8D, 0x8D, 0x8D, 0xFF), new Color32(0x8D, 0x8D, 0x8D, 0xFF), new Color32(0x8D, 0x8D, 0x8D, 0xFF),
        new Color32(0xC8, 0xC8, 0xC8, 0xFF), new Color32(0x3A, 0x3A, 0x3A, 0xFF));
    [Tooltip("the speed button, always")]
    public ButtonColors speed = new ButtonColors(
        new Color32(0x4A, 0x9E, 0xDC, 0xFF), new Color32(0x7C, 0xC8, 0xF4, 0xFF), new Color32(0x34, 0x78, 0xB8, 0xFF),
        new Color32(0xF0, 0xFA, 0xFF, 0xFF), new Color32(0x18, 0x3A, 0x66, 0xFF));
}
