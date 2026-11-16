using UnityEngine;

// every number and color of the plant widget (PlantWidget), shared by every plant. lives at
// Resources/PlantWidgetStyle.asset so tuning it in the inspector changes all plants at once.
// spec: Downloads\gd_plant_widget\README.md. all sizes are in art pixels of the widget sprites
[CreateAssetMenu(fileName = "PlantWidgetStyle", menuName = "Scriptable Objects/Plant Widget Style")]
public class PlantWidgetStyle : ScriptableObject
{
    [Header("Sprites (Sprites/UI/PlantWidget)")]
    public Sprite barHealth;
    public Sprite barThin;
    public Sprite fillHealth;
    public Sprite fillThin;
    public Sprite circleInside;
    public Sprite circleCharge;
    public Sprite circleOutline;
    public Sprite circleGlow;

    [Header("Size and placement")]
    [Tooltip("width of every bar in art pixels. whole numbers only, at least 5. only the middle stretches")]
    [Min(5)] public int barWidth = 42;
    [Tooltip("whole number scale of the whole widget, 1 or 2. the base size comes from the sprites' Pixels Per " +
        "Unit: 64 (now) is half the size of 32. never resize it any other way")]
    [Min(1)] public int pixelScale = 1;
    [Tooltip("widget pixels between the top of the plant's own sprite and the bottom of the widget. each plant " +
        "is measured on its own, so a tall plant gets its bar higher than a short one")]
    [Min(0)] public int gapAboveSprite = 2;
    [Tooltip("widget pixels to move every plant's widget up (or down, negative) on top of that. one plant alone: " +
        "its PlantData's Widget Height Offset")]
    public int verticalOffset = 0;
    public string sortingLayer = "Plants";
    public int sortingOrder = 50;

    [Header("When it shows")]
    [Tooltip("show it on every plant all the time, not just when hovered, selected or hurt")]
    public bool alwaysVisible = false;
    [Tooltip("keep it showing on a plant whose skill is unlocked, so its charge can be read at a glance " +
        "(the old skill bar was always shown too)")]
    public bool showWhileSkillUnlocked = true;

    [Header("Health")]
    [Tooltip("on: the fill turns from high to mid to low color as the plant loses health. off: healthFixed")]
    public bool colorHealthByAmount = true;
    public Color healthHigh = new Color32(0x4C, 0xAF, 0x50, 0xFF);
    public Color healthMid = new Color32(0xFF, 0xC9, 0x3C, 0xFF);
    public Color healthLow = new Color32(0xE5, 0x48, 0x3F, 0xFF);
    [Tooltip("high above this share of max health, e.g. 0.5 = 50%")]
    [Range(0f, 1f)] public float highAbove = 0.5f;
    [Tooltip("mid above this share of max health, low at or below it")]
    [Range(0f, 1f)] public float midAbove = 0.25f;
    public Color healthFixed = new Color32(0x4C, 0xAF, 0x50, 0xFF);
    [Tooltip("shield shown past the health, same as the old bar")]
    public Color shield = new Color(0.55f, 0.55f, 0.55f, 1f);
    [Tooltip("the chunk just lost, shown for a moment behind the health before it drains")]
    public Color damageChunk = Color.white;

    [Header("Extra bars")]
    [Tooltip("by position under the health bar: the first entry is the second bar, the next the third, " +
        "and so on. a bar past the end of the list reuses the last color")]
    public Color[] extraBarColors = { new Color32(0x3F, 0xA9, 0xF5, 0xFF), new Color32(0xB0, 0xBE, 0xC5, 0xFF) };
    [Tooltip("sun generation cooldown bars (the sunflower's), the same gold as Sun in tooltips")]
    public Color sunBarColor = new Color32(0xFF, 0xD7, 0x00, 0xFF);

    [Header("Skill circle")]
    public Color circleInsideColor = new Color32(0x2E, 0x2E, 0x2E, 0xFF);
    public Color circleOutlineColor = new Color32(0x0E, 0x0E, 0x0E, 0xFF);
    public Color chargeColor = new Color32(0x5B, 0xD8, 0x4A, 0xFF);
    public Color readyGlowColor = new Color32(0xD6, 0xFF, 0xB0, 0xFF);
    [Tooltip("seconds for one full pulse of the ready glow")]
    public float glowPulsePeriod = 1.4f;
    [Range(0f, 1f)] public float glowMinAlpha = 0.7f;
    [Range(0f, 1f)] public float glowMaxAlpha = 1f;

    public Color ExtraBarColor(int index)
    {
        if (extraBarColors == null || extraBarColors.Length == 0) return Color.white;
        return extraBarColors[Mathf.Clamp(index, 0, extraBarColors.Length - 1)];
    }

    public Color HealthColor(float fraction)
    {
        if (!colorHealthByAmount) return healthFixed;
        if (fraction > highAbove) return healthHigh;
        if (fraction > midAbove) return healthMid;
        return healthLow;
    }
}
