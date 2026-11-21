using UnityEngine;

// sprites and button colours of the game controls under the plant info panel (GameHUD,
// GameControlButton): the start, pause and resume button and the speed button. lives at
// Resources/GameControlsStyle.asset so tuning it in the inspector changes them in every level.
// spec: Downloads\gd_game_controls\README.md. the wave text, the countdown bar and its text keep
// their colours on their own objects in the Canvas prefab. layout: Buttons Left (2026-11-21): two 34 by 34
// square buttons with icons, the speed one over its 1x, and the wave timer filling the rest of the row
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
    public Sprite buttonIdle;
    [Tooltip("white light edges and nail heads, drawn over the button in the same rect")]
    public Sprite buttonIdleShine;
    public Sprite buttonHover;
    public Sprite buttonHoverShine;
    [Tooltip("the face sits 2 pixels lower, so the icon and label drop 2 pixels too")]
    public Sprite buttonPressed;
    public Sprite buttonPressedShine;

    [Header("Icons")]
    public Sprite playIcon;
    public Sprite pauseIcon;
    public Sprite speedIcon;

    [Header("Inside the 34 by 34 buttons (art pixels from the button's top left; 2 lower while pressed)")]
    public Vector2Int playIconAt = new Vector2Int(14, 8);
    public Vector2Int pauseIconAt = new Vector2Int(13, 8);
    public Vector2Int speedIconAt = new Vector2Int(12, 6);
    [Tooltip("cap top of the speed label (1x, 2x), which is centred on the button by its drawn pixels")]
    public int speedLabelTop = 18;

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
    [Tooltip("the speed button, always")]
    public ButtonColors speed = new ButtonColors(
        new Color32(0x4A, 0x9E, 0xDC, 0xFF), new Color32(0x7C, 0xC8, 0xF4, 0xFF), new Color32(0x34, 0x78, 0xB8, 0xFF),
        new Color32(0xF0, 0xFA, 0xFF, 0xFF), new Color32(0x18, 0x3A, 0x66, 0xFF));
}
