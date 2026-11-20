using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// the bar widget over a plant (spec: Downloads\gd_plant_widget\README.md): a slim health bar,
// generic extra bars stacked under it (each a value from 0 to 1 and a color, added and removed at
// runtime), and a circle on the bar's left end that fills clockwise as the skill charges and pulses
// when it's ready. a world space canvas built in code, one canvas unit = one art pixel, every
// position and frame a whole pixel; only fill lengths are free, so they glide (see PlaceFill). the owner feeds it every frame (SetHealth, SetSkill,
// SetExtraBar, SetVisible); the look comes from Resources/PlantWidgetStyle.asset
public class PlantWidget : MonoBehaviour
{
    private const int HealthBarHeight = 6;
    private const int ExtraBarHeight = 5;
    private const int CircleSize = 16;
    // the circle's 16 px box starts this far left of the bar, so the 12 px circle overlaps the
    // bar's left end by 3 px
    private const int CircleLeftOfBar = 11;
    // same easing speed as the old health bar (Entity.HealthBarLerpSpeed)
    private const float LerpSpeed = 12f;
    private const float DamageChunkDelay = 0.25f;

    private static PlantWidgetStyle _style;
    public static PlantWidgetStyle Style
    {
        get
        {
            if (_style == null) _style = Resources.Load<PlantWidgetStyle>("PlantWidgetStyle");
            return _style;
        }
    }

    private class Bar
    {
        public string key;
        public Color? color;
        public float target;
        public float displayed = -1f;
        public Image frame;
        public Image fill;
    }

    private Transform owner;
    private float baseHeightUnits;
    private Canvas canvas;
    private RectTransform framesRoot, fillsRoot;
    private Image healthFrame, healthFill, shieldFill, chunkFill;
    private Image circleInside, circleCharge, circleOutline, circleGlow;
    private readonly List<Bar> extraBars = new List<Bar>();
    private bool layoutDirty = true;
    private int builtWidth = -1;

    private float health, maxHealth, shield;
    private float displayedHealth = -1f, displayedShield = -1f;
    private float chunkProtection = -1f, lastProtection = -1f, chunkDelay;

    private bool skillUnlocked, skillReady;
    private float chargeTarget, displayedCharge = -1f;

    // one small square per equipped mulch, coloured by its rarity, in a row just above the health
    // bar (Tommy: plain coloured squares, not mini icons)
    private const int MarkerSize = 3;
    private const int MarkerGap = 1;
    private readonly List<Image> mulchMarkers = new List<Image>();
    private int markerCount;

    // height from the owner's own sprite (FollowSpriteTop): the highest point of its visible
    // pixels, in world units above the owner, taken as the highest seen over its first moments of
    // idle animation so a bobbing petal can't poke through the bar. negative infinity until then
    private const float MeasureSeconds = 1.5f;
    // the 12 px circle hangs this far below its middle, and it's the lowest part of the widget
    private const int CircleVisibleHalf = 6;
    private SpriteRenderer heightSource;
    private int heightOffsetPixels;
    private float measuredTop = float.NegativeInfinity;
    private float measureUntil;

    private float PixelsPerUnit => Style != null && Style.barHealth != null ? Style.barHealth.pixelsPerUnit : 32f;
    private int Width => Style != null ? Mathf.Max(5, Style.barWidth) : 34;

    // baseHeightUnits: how far above the owner's position the middle of the circle sits before the
    // style's verticalOffset, normally the owner's old health bar height
    public static PlantWidget Create(Transform owner, float baseHeightUnits)
    {
        if (Style == null)
        {
            Debug.LogWarning("PlantWidget: Resources/PlantWidgetStyle.asset not found");
            return null;
        }
        GameObject go = new GameObject("PlantWidget", typeof(RectTransform));
        go.transform.SetParent(owner, false);
        PlantWidget widget = go.AddComponent<PlantWidget>();
        widget.owner = owner;
        widget.baseHeightUnits = baseHeightUnits;
        widget.Build();
        return widget;
    }

    // sits the widget just above this sprite's visible top instead of at the fixed base height, so
    // tall and short plants each get their bar right above them. the sprite's tight mesh follows its
    // opaque pixels, so transparent padding in a sheet doesn't count. extraOffsetPixels is the
    // plant's own nudge (PlantData.widgetHeightOffset)
    public void FollowSpriteTop(SpriteRenderer source, int extraOffsetPixels)
    {
        heightSource = source;
        heightOffsetPixels = extraOffsetPixels;
        measuredTop = float.NegativeInfinity;
        measureUntil = Time.time + MeasureSeconds;
        MeasureSpriteTop();
    }

    private void MeasureSpriteTop()
    {
        if (heightSource == null || owner == null || Time.time > measureUntil) return;
        Sprite sprite = heightSource.sprite;
        if (sprite == null) return;
        Transform source = heightSource.transform;
        float top = float.NegativeInfinity;
        foreach (Vector2 vertex in sprite.vertices)
            top = Mathf.Max(top, source.TransformPoint(vertex).y);
        top -= owner.position.y;
        if (top > measuredTop) measuredTop = top;
    }

    public void SetVisible(bool visible)
    {
        if (canvas != null && canvas.enabled != visible) canvas.enabled = visible;
    }

    // called once per frame by the owner. eases like the old bar: the fill moves toward the real
    // value, a shield that pops just vanishes, and a hit leaves a white chunk that holds for a
    // moment then drains
    public void SetHealth(float currentHealth, float currentMaxHealth, float currentShield, bool shieldJustExpired)
    {
        health = currentHealth;
        maxHealth = currentMaxHealth;
        shield = currentShield;
        float t = 1f - Mathf.Exp(-LerpSpeed * Time.unscaledDeltaTime);

        if (displayedHealth < 0f) { displayedHealth = health; displayedShield = shield; chunkProtection = health + shield; lastProtection = chunkProtection; }
        displayedHealth = Mathf.Lerp(displayedHealth, health, t);
        displayedShield = shieldJustExpired ? shield : Mathf.Lerp(displayedShield, shield, t);

        float protection = health + shield;
        if (shieldJustExpired) { chunkProtection = protection; chunkDelay = 0f; }
        else if (protection < lastProtection) chunkDelay = DamageChunkDelay;
        else if (protection > chunkProtection) { chunkProtection = protection; chunkDelay = 0f; }
        lastProtection = protection;

        if (chunkDelay > 0f) chunkDelay -= Time.unscaledDeltaTime;
        else if (chunkProtection > protection) chunkProtection = Mathf.Lerp(chunkProtection, protection, t);
    }

    // charge from 0 to 1. a recharge eases up, a skill being used drops it at once
    public void SetSkill(bool unlocked, float charge, bool ready)
    {
        skillUnlocked = unlocked;
        skillReady = unlocked && ready;
        chargeTarget = unlocked ? Mathf.Clamp01(charge) : 0f;
        displayedCharge = EaseUpOnly(displayedCharge, chargeTarget);
    }

    // adds the bar the first time a key is used, then updates it. color null = the style's color
    // for its position in the stack
    public void SetExtraBar(string key, float value, Color? color = null)
    {
        Bar bar = FindBar(key);
        if (bar == null)
        {
            bar = new Bar { key = key };
            extraBars.Add(bar);
            layoutDirty = true;
        }
        bar.color = color;
        bar.target = Mathf.Clamp01(value);
        bar.displayed = EaseUpOnly(bar.displayed, bar.target);
    }

    public void RemoveExtraBar(string key)
    {
        Bar bar = FindBar(key);
        if (bar == null) return;
        if (bar.frame != null) Destroy(bar.frame.gameObject);
        if (bar.fill != null) Destroy(bar.fill.gameObject);
        extraBars.Remove(bar);
        layoutDirty = true;
    }

    public bool HasExtraBar(string key) => FindBar(key) != null;

    // the owner's equipped mulches as rarity colours, in slot order. only called when they change
    public void SetMulchMarkers(List<Color> colors)
    {
        int count = colors != null ? colors.Count : 0;
        while (mulchMarkers.Count < count)
            mulchMarkers.Add(NewImage("MulchMarker", transform, null, Image.Type.Simple, Color.white));
        for (int i = 0; i < mulchMarkers.Count; i++)
        {
            bool show = i < count;
            mulchMarkers[i].enabled = show;
            if (show) mulchMarkers[i].color = colors[i];
        }
        if (count != markerCount) layoutDirty = true;
        markerCount = count;
    }

    private Bar FindBar(string key)
    {
        foreach (Bar bar in extraBars)
            if (bar.key == key) return bar;
        return null;
    }

    // same feel as the old cooldown bars: rising values ease in, a drop snaps
    private static float EaseUpOnly(float displayed, float target)
    {
        if (displayed < 0f || target <= displayed) return target;
        return Mathf.Lerp(displayed, target, 1f - Mathf.Exp(-LerpSpeed * Time.unscaledDeltaTime));
    }

    private void Build()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingLayerName = Style.sortingLayer;
        canvas.sortingOrder = Style.sortingOrder;
        // reference pixels per unit = the sprites' own, so a sprite pixel (and a 9 slice border)
        // is exactly one canvas unit
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.referencePixelsPerUnit = PixelsPerUnit;
        scaler.dynamicPixelsPerUnit = 1f;

        RectTransform rect = (RectTransform)transform;
        rect.sizeDelta = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        // back to front: bar frames, bar fills, circle inside, charge, outline, glow
        framesRoot = NewContainer("Frames");
        fillsRoot = NewContainer("Fills");

        healthFrame = NewImage("HealthFrame", framesRoot, Style.barHealth, Image.Type.Sliced, Color.white);
        chunkFill = NewImage("DamageChunk", fillsRoot, Style.fillHealth, Image.Type.Sliced, Style.damageChunk);
        shieldFill = NewImage("Shield", fillsRoot, Style.fillHealth, Image.Type.Sliced, Style.shield);
        healthFill = NewImage("Health", fillsRoot, Style.fillHealth, Image.Type.Sliced, Style.healthHigh);

        circleInside = NewImage("CircleInside", transform, Style.circleInside, Image.Type.Simple, Style.circleInsideColor);
        circleCharge = NewImage("CircleCharge", transform, Style.circleCharge, Image.Type.Filled, Style.chargeColor);
        circleCharge.fillMethod = Image.FillMethod.Radial360;
        circleCharge.fillOrigin = (int)Image.Origin360.Top;
        circleCharge.fillClockwise = true;
        circleCharge.fillAmount = 0f;
        circleOutline = NewImage("CircleOutline", transform, Style.circleOutline, Image.Type.Simple, Style.circleOutlineColor);
        circleGlow = NewImage("CircleGlow", transform, Style.circleGlow, Image.Type.Simple, Style.readyGlowColor);
        circleGlow.enabled = false;

        canvas.enabled = false;
    }

    private RectTransform NewContainer(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        return rect;
    }

    private static Image NewImage(string name, Transform parent, Sprite sprite, Image.Type type, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        // every piece is placed by its top left corner, in whole canvas units (art pixels)
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 1f);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.type = type;
        image.color = color;
        image.raycastTarget = false;
        image.maskable = false;
        image.pixelsPerUnitMultiplier = 1f;
        return image;
    }

    private static void Place(Image image, int left, int top, int width, int height)
    {
        RectTransform rect = image.rectTransform;
        rect.anchoredPosition = new Vector2(left, top);
        rect.sizeDelta = new Vector2(width, height);
    }

    // the layout rules, in art pixels with y up and (0, 0) the middle of the circle:
    // the bar is centred on the plant's x, the circle's box starts 11 px left of the bar, bars
    // stack down sharing one border row, and the stack's middle lines up with the circle's middle
    // (one bar is centred on it)
    private void Layout()
    {
        layoutDirty = false;
        int width = Width;
        builtWidth = width;
        int left = -(width / 2);

        int count = 1 + extraBars.Count;
        int stackHeight = HealthBarHeight + extraBars.Count * ExtraBarHeight - (count - 1);
        int circleTop = CircleSize / 2;
        int top = circleTop - Mathf.FloorToInt((CircleSize - stackHeight) / 2f);

        Place(healthFrame, left, top, width, HealthBarHeight);
        int barTop = top + 1 - HealthBarHeight;
        for (int i = 0; i < extraBars.Count; i++)
        {
            Bar bar = extraBars[i];
            if (bar.frame == null)
            {
                bar.frame = NewImage("ExtraFrame", framesRoot, Style.barThin, Image.Type.Sliced, Color.white);
                bar.fill = NewImage("ExtraFill", fillsRoot, Style.fillThin, Image.Type.Sliced, Color.white);
            }
            Place(bar.frame, left, barTop, width, ExtraBarHeight);
            barTop += 1 - ExtraBarHeight;
        }

        // mulch squares: a row from the bar's left end, one pixel above its top border
        for (int i = 0; i < markerCount && i < mulchMarkers.Count; i++)
            Place(mulchMarkers[i], left + i * (MarkerSize + MarkerGap), top + 1 + MarkerSize, MarkerSize, MarkerSize);

        int circleLeft = left - CircleLeftOfBar;
        Place(circleInside, circleLeft, circleTop, CircleSize, CircleSize);
        Place(circleCharge, circleLeft, circleTop, CircleSize, CircleSize);
        Place(circleOutline, circleLeft, circleTop, CircleSize, CircleSize);
        Place(circleGlow, circleLeft, circleTop, CircleSize, CircleSize);
    }

    // a fill sits 1 px inside its bar and is exactly as long as its share of the inside, fractions
    // of a pixel included (tommy's call, 2026-11-15: the readme rounded to whole pixels, which made
    // the eased fills step a pixel at a time). its left end and height stay on whole pixels, only
    // its length is free, so the bar glides as it grows and shrinks
    private void PlaceFill(Image fill, Image frame, float fraction)
    {
        float fillWidth = (Width - 2) * Mathf.Clamp01(fraction);
        bool show = fillWidth > 0.01f;
        if (fill.enabled != show) fill.enabled = show;
        if (!show) return;
        RectTransform frameRect = frame.rectTransform;
        RectTransform rect = fill.rectTransform;
        rect.anchoredPosition = new Vector2(Mathf.Round(frameRect.anchoredPosition.x) + 1f, Mathf.Round(frameRect.anchoredPosition.y) - 1f);
        rect.sizeDelta = new Vector2(fillWidth, Mathf.Round(frameRect.sizeDelta.y) - 2f);
    }

    private void LateUpdate()
    {
        if (owner == null) { Destroy(gameObject); return; }
        MeasureSpriteTop();
        if (Style == null || canvas == null || !canvas.enabled) return;
        if (layoutDirty || builtWidth != Width) Layout();

        // one canvas unit = one art pixel, times the style's whole number scale, whatever the
        // owner's own scale is. snapped to the pixel grid so nothing ever lands between pixels
        float ppu = PixelsPerUnit;
        float worldScale = Mathf.Max(1, Style.pixelScale) / ppu;
        Vector3 parentScale = owner.lossyScale;
        transform.localScale = new Vector3(
            worldScale / (Mathf.Approximately(parentScale.x, 0f) ? 1f : parentScale.x),
            worldScale / (Mathf.Approximately(parentScale.y, 0f) ? 1f : parentScale.y), 1f);
        // middle of the circle: just above the plant's measured sprite top (its lowest edge a gap
        // above the sprite), or the old fixed height until a sprite has been measured
        float heightPixels = float.IsNegativeInfinity(measuredTop)
            ? Mathf.Round(baseHeightUnits * ppu)
            : Mathf.Ceil(measuredTop * ppu) + Style.gapAboveSprite + CircleVisibleHalf;
        heightPixels += Style.verticalOffset + heightOffsetPixels;
        Vector3 pos = owner.position;
        transform.position = new Vector3(Mathf.Round(pos.x * ppu) / ppu, Mathf.Round(pos.y * ppu) / ppu + heightPixels / ppu, pos.z);

        float totalDisplay = maxHealth + shield;
        if (totalDisplay > 0f)
        {
            // the color follows the bar as it's drawn (the eased health), so it changes as the bar
            // visibly passes each threshold rather than jumping ahead of it
            healthFill.color = Style.HealthColor(maxHealth > 0f ? displayedHealth / maxHealth : 0f);
            PlaceFill(healthFill, healthFrame, displayedHealth / totalDisplay);
            bool hasShield = displayedShield > 0.001f || shield > 0f;
            PlaceFill(shieldFill, healthFrame, hasShield ? (displayedHealth + displayedShield) / totalDisplay : 0f);
            bool chunk = chunkProtection > health + shield + 0.001f;
            PlaceFill(chunkFill, healthFrame, chunk ? chunkProtection / totalDisplay : 0f);
        }

        for (int i = 0; i < extraBars.Count; i++)
        {
            Bar bar = extraBars[i];
            bar.fill.color = bar.color ?? Style.ExtraBarColor(i);
            PlaceFill(bar.fill, bar.frame, bar.displayed);
        }

        circleCharge.fillAmount = skillUnlocked ? Mathf.Clamp01(displayedCharge) : 0f;
        if (circleGlow.enabled != skillReady) circleGlow.enabled = skillReady;
        if (skillReady)
        {
            float period = Mathf.Max(0.01f, Style.glowPulsePeriod);
            float wave = (Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI / period) + 1f) * 0.5f;
            Color glow = Style.readyGlowColor;
            glow.a = Mathf.Lerp(Style.glowMinAlpha, Style.glowMaxAlpha, wave);
            circleGlow.color = glow;
        }
    }
}
