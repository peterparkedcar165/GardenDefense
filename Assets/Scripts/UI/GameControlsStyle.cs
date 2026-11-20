using UnityEngine;

// sprites and button colours of the game controls under the plant info panel (GameHUD,
// GameControlButton): the start, pause and resume button and the speed button. lives at
// Resources/GameControlsStyle.asset so tuning it in the inspector changes them in every level.
// spec: Downloads\gd_game_controls\README.md. the wave text, the countdown bar and its text keep
// their colours on their own objects in the Canvas prefab
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
        [Tooltip("tint of the greyscale button sprite. hover and pressed get lighter or darker from their own sprites, " +
            "not from this colour")]
        public Color face;
        [Tooltip("the label and the icon")]
        public Color text;
        [Tooltip("the copy 1 pixel under the label and the icon. about the face colour times 0.3 looks right")]
        public Color shadow;

        public ButtonColors(Color face, Color text, Color shadow)
        {
            this.face = face;
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

    [Header("Button colours")]
    [Tooltip("before the first wave")]
    public ButtonColors start = new ButtonColors(
        new Color32(0x62, 0xB8, 0x3C, 0xFF), new Color32(0xF4, 0xFF, 0xE6, 0xFF), new Color32(0x1E, 0x4A, 0x14, 0xFF));
    [Tooltip("while the game runs")]
    public ButtonColors pause = new ButtonColors(
        new Color32(0xF0, 0xB4, 0x2A, 0xFF), new Color32(0xFF, 0xFB, 0xE8, 0xFF), new Color32(0x5E, 0x3C, 0x06, 0xFF));
    [Tooltip("while paused")]
    public ButtonColors resume = new ButtonColors(
        new Color32(0x62, 0xB8, 0x3C, 0xFF), new Color32(0xF4, 0xFF, 0xE6, 0xFF), new Color32(0x1E, 0x4A, 0x14, 0xFF));
    [Tooltip("the speed button, always")]
    public ButtonColors speed = new ButtonColors(
        new Color32(0x4A, 0x9E, 0xDC, 0xFF), new Color32(0xF0, 0xFA, 0xFF, 0xFF), new Color32(0x18, 0x3A, 0x66, 0xFF));
}
