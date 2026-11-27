using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

// the info panel for the selected plant or insect, built from Tommy's plant info panel pack
// (Downloads\gd_plant_info_panel\README.md). every piece is greyscale art tinted here: the panel,
// text shadows, portrait and skill frame by the element, the pips by their path, the buttons by
// their state (PixelButton). the prefab holds the layout in art pixels times 2 (1 art pixel = 2
// canvas units, every child anchored to its parent's top left). the panel grows 14 art pixels per
// level bar shown (oxygen, temperature) and moves everything under them down. an insect only gets
// the top part: name, portrait, health and stats. colours and timings: PlantInfoPanelStyle
public class PlantUpgradeUI : EntityInfoPanel
{
    [System.Serializable]
    public class InfoBar
    {
        public RectTransform root;
        public Image fill;
        public Image shine;
        [Tooltip("health only: the chunk just lost, behind the fill")]
        public Image chunk;
        public TMP_Text text;
    }

    [System.Serializable]
    public class PathRow
    {
        public TMP_Text nameText;
        [Tooltip("the 5 pips: empty, filled or max")]
        public Image[] pips;
        [Tooltip("drawn over each pip: the pulsing rim of the next one the player can buy")]
        public Image[] pipNext;
        [Tooltip("drawn over each pip: the max sprite, shown a moment when it's bought")]
        public Image[] pipFlash;
        public Button button;
        public PixelButton look;
        public GameObject plus;
        public GameObject lockGlyph;
        public TMP_Text priceText;
        public GameObject sun;

        [System.NonSerialized] public int shownLevel = -1;
        [System.NonSerialized] public float[] flashUntil;
        // the price the price text shows (ShownMaxPrice while it says Max), so the number is only
        // turned into text when it changes
        [System.NonSerialized] public int shownPrice = ShownNoPrice;
    }

    private const int ShownNoPrice = int.MinValue;
    private const int ShownMaxPrice = int.MinValue + 1;

    public static PlantUpgradeUI instance;

    // the layout in art pixels (README), times Px for canvas units
    private const float Px = 2f;
    private const int LevelBarStep = 14;
    // round 6: the mulch row's slots grew to 32 at the game's 232 width, so the mulch inset is 4
    // taller than the design's 38 and the panel 218 (the design's 214). the wood panel (Tommy,
    // 2026-11-25): the name sits on its plate at the top, so everything under the old name line moved
    // up 8, and the panel ends 10 under the content for the frame's bottom flourishes: 214 for a
    // plant, 92 for an insect. the panel is 4 wider than the window it sits in (the art's outer 2 px
    // each side are see through), so the content sits 2 further right: the first level bar 81 across
    // and 31 down
    // Tommy's 80 px portrait frame (2026-11-26, UsePortraitFrame) made the top 14 taller and the inset and
    // its bars start 10 further right: 214, 92, 68 and 81 before
    private const int PlantPanelHeight = 228;
    private const int InsectPanelHeight = 108;
    private const int TopInsetHeight = 82;
    private const int LevelBarX = 91;
    private const int FirstLevelBarY = 31;
    private const int SideButtonGap = 3;

    [Header("Layout")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private RectTransform topInset;
    [Tooltip("everything under the level bars, moved down 14 art pixels per bar")]
    [SerializeField] private RectTransform shiftedRoot;
    [SerializeField] private GameObject skillGroup;
    [Tooltip("paths and mulches, plants only")]
    [SerializeField] private GameObject lowerSection;
    [Tooltip("the target button's place. a plant without targeting gets its auto cast button here instead")]
    [SerializeField] private RectTransform actionSlot;
    [Tooltip("buttons the design has no place for (auto cast next to targeting, orbit radius, transplant, " +
        "relocate, flying first), stacked outside the panel's left edge")]
    [SerializeField] private RectTransform sideButtons;

    [Header("Element colours")]
    [SerializeField] private Image panelBackground;
    [Tooltip("the insets and strips, tinted with the panel colour like the panel itself")]
    [SerializeField] private Image[] panelTinted;
    [Tooltip("texts in the panel colour's text shadow")]
    [SerializeField] private PixelTextEffect[] textShadows;
    [SerializeField] private TMP_Text[] panelTexts;
    [SerializeField] private Image portraitFrame;
    [SerializeField] private Image portraitBackdrop;

    [Header("Targeting")]
    [Tooltip("the mode's name in the display between the two arrow buttons, centred on whole art pixels")]
    [SerializeField] private TMP_Text targetingModeText;
    [Tooltip("the arrow buttons and the display, shown for plants that use targeting")]
    [SerializeField] private GameObject targetingControl;

    [Header("Minions (Burgeon)")]
    [SerializeField] private Button relocateMinionsButton;   // visible only for plants that command minions
    [SerializeField] private float relocateIndicatorRadius = 0.5f;

    [Header("Flying Priority (Cattail)")]
    [SerializeField] private Button flyingToggleButton;      // visible only for plants with a flying first toggle
    [SerializeField] private TMP_Text flyingToggleText;

    [Header("Orbit Radius (Tansy)")]
    [SerializeField] private Button radiusToggleButton;      // visible on every plant while Tansy is in the loadout
    [SerializeField] private TMP_Text radiusToggleText;

    [Header("Bars")]
    [SerializeField] private InfoBar healthBar;
    [SerializeField] private InfoBar oxygenBar;
    [SerializeField] private InfoBar temperatureBar;
    [SerializeField] private RectTransform tempComfortMinIndicator;
    [SerializeField] private RectTransform tempComfortMaxIndicator;

    [Header("Paths (Attack, Passive, Skill)")]
    [SerializeField] private PathRow path1;
    [SerializeField] private PathRow path2;
    [SerializeField] private PathRow path3;

    [Header("Skill")]
    [SerializeField] private Button skillButton;
    [SerializeField] private Image skillFrame;
    [SerializeField] private Image skillIcon;
    [Tooltip("radial fill over the icon while it recharges, full while silenced or not unlocked")]
    [SerializeField] private Image skillCooldownOverlay;
    [SerializeField] private TMP_Text skillCooldownText;
    [Tooltip("the small lock on the skill square while silenced")]
    [SerializeField] private GameObject skillLock;

    [Header("Locked skill (round 6)")]
    [Tooltip("over the Skill strip while the skill isn't unlocked: dim, vines, padlock, price and sun. a click on it unlocks")]
    [SerializeField] private GameObject lockedSkillStrip;
    [SerializeField] private PointerHover lockedSkillHover;
    [SerializeField] private Image lockedStripVines;
    [SerializeField] private Image lockedStripPadlock;
    [SerializeField] private TMP_Text lockedPrice;
    [SerializeField] private Image lockedSun;
    [Tooltip("over the skill square while the skill isn't unlocked: dim, vines and a bigger padlock")]
    [SerializeField] private GameObject lockedSkillSquare;
    [SerializeField] private Image lockedSquareVines;
    [SerializeField] private Image lockedSquarePadlock;
    [Tooltip("the square lights up and unlocks on click like the strip, and either one hovered lights both")]
    [SerializeField] private PointerHover lockedSquareHover;

    [Header("Transplant")]
    [SerializeField] private Button transplantButton;        // visible only while the Transplant fertilizer is active
    [SerializeField] private TMP_Text transplantCooldownText;

    [Header("Auto Cast")]
    [SerializeField] private Button autoCastButton;          // visible only for plants that support auto cast
    [SerializeField] private TMP_Text autoCastButtonText;

    [Header("Status Effects")]
    [SerializeField] private StatusEffectPanel statusEffectPanel;

    [Header("Stats Panels")]
    [SerializeField] private GameObject statsPanels;

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    private Plant selectedPlant;
    private Insect selectedInsect;
    private SpriteRenderer cachedInsectRenderer;
    private int shownLevelBars = -1;
    private bool shownAsPlant;
    // the locked skill price the strip's group was last laid out for
    private int lockedPriceLaidOut = ShownNoPrice;

    // the values the live texts below were last written from, so a text is only made again when
    // what it shows changed. NaN or the ShownNo values mean not written yet
    private float shownHealth = float.NaN, shownMaxHealth = float.NaN, shownShield = float.NaN;
    private float shownAir = float.NaN;
    private float shownTemperature = float.NaN;
    private string shownTemperatureState;
    private int shownSkillSeconds = ShownNoPrice;
    private int shownTransplant = ShownNoPrice;
    private int shownAutoCast = ShownNoPrice;
    private string shownAutoCastLabel;
    // the comfort marks' images, looked up once
    private Image comfortMinImage, comfortMaxImage;

    // fluid health bar, matching the plant widget: the fill eases toward the real share each frame
    // instead of snapping, and resets on a new selection so it never lerps in from the last one
    private Entity _healthBarEntity;
    private float _displayedHealthPct = -1f;
    private const float HealthBarLerpSpeed = 12f;

    // trailing damage chunk behind the fill: freezes at the share before a hit, waits, then drains
    private float _chunkPct = -1f;
    private float _lastPctForChunk = -1f;
    private float _chunkDelayTimer;
    private const float DamageChunkDelay = 0.25f;
    private static readonly Color AllyChunkColor  = Color.white;
    private static readonly Color EnemyChunkColor = new Color(1f, 0.55f, 0f);

    protected override void Awake()
    {
        if (instance != null) return;
        instance = this;
        base.Awake();
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
        // Tommy's flower frame (2026-11-26), its inside in the element's colour like the panel (ApplyElement),
        // the text clear of it (LayoutTooltip's TooltipPadding is the same 20 above and below)
        if (tooltipPanel != null && TooltipFrameLook.Available)
        {
            TooltipFrameLook.Apply(tooltipPanel.GetComponent<Image>(), PlantData.ElementPanelColor(ElementalType.Neutral));
            if (tooltipText != null)
            {
                RectTransform text = tooltipText.rectTransform;
                text.offsetMin = new Vector2(TooltipFrameLook.Inset, TooltipFrameLook.Inset);
                text.offsetMax = new Vector2(-TooltipFrameLook.Inset, -TooltipFrameLook.Inset);
            }
        }
        UseSmallUpgradeButtons();
        UsePortraitFrame();
        UseTargetingClicker();
        // the skill square feels like a button once unlocked (Tommy, 2026-11-26): lit under the pointer,
        // pressed when clicked or cast with Q
        Transform trim = skillIcon != null && skillIcon.transform.parent != null ? skillIcon.transform.parent.Find("SkillFrameTrim") : null;
        skillFeel = SkillButtonFeel.Attach(skillButton, SkillFrameBase,
            trim != null ? trim.GetComponent<Image>() : null, skillIcon, skillCooldownOverlay);
    }

    private SkillButtonFeel skillFeel;

    // the Attack, Passive and Skill upgrade buttons are Tommy's small button (2026-11-25), 112 by 38 at
    // 2 units a pixel, against their strip's right end (2 units in); the price and glyphs move 2 px down
    // so the capitals sit in the middle of its wood (rows 3 to 15 of 19)
    private const int UpgradeButtonDrop = 2;

    private void UseSmallUpgradeButtons()
    {
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        if (style.upgradeButton == null) return;
        foreach (PathRow row in Paths)
        {
            if (row == null || row.look == null) continue;
            row.look.UseArt(style.upgradeButton, style.upgradeButtonTrim, UpgradeButtonDrop);
            RectTransform r = (RectTransform)row.look.transform;
            RectTransform strip = r.parent as RectTransform;
            if (strip != null && Mathf.Approximately(r.anchorMin.x, 0f) && Mathf.Approximately(r.pivot.x, 0f))
                r.anchoredPosition = new Vector2(strip.rect.width - r.sizeDelta.x - 2f, r.anchoredPosition.y);
        }
    }

    protected override void Update()
    {
        base.Update();

        // the insect's description is live (its numbers change): laid out again only when a section changed
        if (tooltipPanel != null && tooltipPanel.activeSelf && selectedInsect != null && tooltipText != null)
            ShowSectionedTooltip(selectedInsect.GetName(), selectedInsect.GetDescriptionSections());

        if (selectedInsect != null && Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
        {
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit == null || hit.GetComponentInParent<Insect>() == null)
                HidePanel();
        }

        // Q works like clicking the skill button (Tommy, 2026-11-27): the button stays down while Q is held, and the
        // skill (its targeting, say) starts when Q is let go, if the same plant is still selected and can still cast it.
        // selecting another plant or closing the panel while it's held lets it go with nothing
        if (qHeldFor != null && qHeldFor != selectedPlant) ReleaseQ();

        if (selectedPlant != null)
        {
            RefreshSkillButton();
            RefreshTransplantButton();
            RefreshAutoCastButton();
            RefreshPaths();
            if (Keyboard.current.qKey.wasPressedThisFrame && selectedPlant.SkillReady)
            {
                qHeldFor = selectedPlant;
                if (skillFeel != null) skillFeel.SetKeyHeld(true);
            }
            if (qHeldFor != null && !Keyboard.current.qKey.isPressed)
            {
                Plant plant = qHeldFor;
                ReleaseQ();
                if (plant == selectedPlant && plant.SkillReady) plant.TriggerSkill();
            }
        }
    }

    // the plant Q was pressed on, while Q is held
    private Plant qHeldFor;

    private void ReleaseQ()
    {
        qHeldFor = null;
        if (skillFeel != null) skillFeel.SetKeyHeld(false);
    }

    // a right click on the targeting display steps the mode back (TargetingClicker) and keeps the panel open
    protected override bool ShouldBlockRightClickClose()
    {
        return SkillTargetingManager.instance.IsTargeting || SkillTargetingManager.instance.WasCancelledThisFrame
            || (targetingClicker != null && targetingClicker.Hovered);
    }

    // --- Show / Hide ---

    public void ShowPanel(Plant plant)
    {
        if (selectedPlant != null) selectedPlant.Deselect();
        selectedInsect?.RefreshHealthBarVisibility();
        selectedInsect = null;
        cachedInsectRenderer = null;

        selectedPlant = plant;
        selectedPlant.Select();

        if (skillGroup != null) skillGroup.SetActive(true);
        if (lowerSection != null) lowerSection.SetActive(true);
        if (flyingToggleText != null)
            flyingToggleText.text = FlyingLabel(plant);
        if (radiusToggleText != null)
            radiusToggleText.text = RadiusLabel(plant);
        ArrangeActionButtons(plant);

        panel.SetActive(true);
        // after the panel and the control are shown, so the name is measured active
        ShowTargetingMode(plant);
        ApplyElement(plant.elementalType);

        entityNameText.text = plant.GetName();
        SetPortrait(plant.data != null ? plant.data.icon : null);

        PlantInfoPanelStyle.SkillFrame frame = PlantInfoPanelStyle.Instance.FrameFor(plant.elementalType);
        if (skillIcon != null)
        {
            Sprite icon = plant.data != null && plant.data.skillIcon != null ? plant.data.skillIcon : frame.fallbackIcon;
            skillIcon.sprite = icon;
            skillIcon.enabled = icon != null;
            FitSkillIcon(icon);
        }

        foreach (PathRow row in Paths) if (row != null) row.shownLevel = -1;

        statusEffectPanel?.Show(plant);
        RefreshStats();
        RefreshPaths();
        RefreshSkillButton();
    }

    public void ShowPanel(Insect insect)
    {
        if (selectedPlant != null) selectedPlant.Deselect();
        selectedPlant = null;

        selectedInsect = insect;
        insect.ShowHealthBar();
        cachedInsectRenderer = insect.GetComponentInChildren<SpriteRenderer>();

        if (skillGroup != null) skillGroup.SetActive(false);
        if (lowerSection != null) lowerSection.SetActive(false);
        HideActionButtons();

        panel.SetActive(true);
        ApplyElement(ElementalType.Neutral);

        // the name line holds one line only: the description is in the portrait's tooltip
        entityNameText.text = insect.GetName();
        SetPortrait(cachedInsectRenderer != null ? cachedInsectRenderer.sprite : null);

        statusEffectPanel?.Show(insect);
        RefreshStats();
    }

    public override void HidePanel()
    {
        if (selectedPlant != null) selectedPlant.Deselect();
        selectedInsect?.RefreshHealthBarVisibility();
        selectedPlant = null;
        selectedInsect = null;
        cachedInsectRenderer = null;
        statusEffectPanel?.Hide();
        statsPanels?.SetActive(false);
        ReleaseQ();
        base.HidePanel();
        HideTooltip();
        lockedSkillTipShown = false;
    }

    public Plant  GetSelectedPlant()  => selectedPlant;
    public Insect GetSelectedInsect() => selectedInsect;

    private PathRow[] Paths => new[] { path1, path2, path3 };

    // --- Element colours ---

    private void ApplyElement(ElementalType element)
    {
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        Color panelColor = PlantData.ElementPanelColor(element);
        Color shadow = PlantData.ElementTextShadowColor(element);
        if (panelBackground != null) panelBackground.color = panelColor;
        if (tooltipPanel != null) TooltipFrameLook.SetInterior(tooltipPanel.GetComponent<Image>(), panelColor);
        if (panelTinted != null)
            foreach (Image image in panelTinted)
                if (image != null) image.color = panelColor;
        if (textShadows != null)
            foreach (PixelTextEffect effect in textShadows)
                if (effect != null) effect.Color = shadow;
        if (panelTexts != null)
            foreach (TMP_Text text in panelTexts)
                if (text != null) text.color = style.text;
        if (portraitFrame != null) portraitFrame.color = PlantData.ElementColor(element);
        if (portraitBackdrop != null) portraitBackdrop.color = PlantData.ElementBackdropColor(element);
        // the skill frame (Tommy, 2026-11-25): its wood takes the element's panel colour; its gold trim
        // and window stay as drawn. without the wood layer the old per element vine frame shows instead
        if (SkillFrameBase != null) SkillFrameBase.color = panelColor;
        else
        {
            PlantInfoPanelStyle.SkillFrame frame = style.FrameFor(element);
            if (skillFrame != null && frame.frame != null) skillFrame.sprite = frame.frame;
        }
    }

    // the skill icon fills the gold frame's 66 unit window (Tommy, 2026-11-25: stretched to it, not kept
    // at whole pixels), centred where the prefab puts it; a non square icon keeps its shape inside it.
    // the cooldown sweep covers the same square
    private const float SkillWindowSize = 66f;
    private Vector2 skillIconCentre;
    private bool skillIconCentreKnown;

    private void FitSkillIcon(Sprite icon)
    {
        if (skillIcon == null || icon == null) return;
        RectTransform rect = skillIcon.rectTransform;
        if (!skillIconCentreKnown)
        {
            skillIconCentre = rect.anchoredPosition + new Vector2(rect.sizeDelta.x * (0.5f - rect.pivot.x), rect.sizeDelta.y * (0.5f - rect.pivot.y));
            skillIconCentreKnown = true;
        }
        Vector2 pixels = icon.rect.size;
        Vector2 size = pixels * (SkillWindowSize / Mathf.Max(pixels.x, pixels.y));
        foreach (Image image in new[] { skillIcon, skillCooldownOverlay })
        {
            if (image == null) continue;
            RectTransform r = image.rectTransform;
            r.sizeDelta = size;
            r.anchoredPosition = skillIconCentre - new Vector2(size.x * (0.5f - r.pivot.x), size.y * (0.5f - r.pivot.y));
        }
    }

    // a skill not unlocked yet has its icon darkened like a locked plant slot (Tommy, 2026-11-25: the icon
    // only, the frame stays as it is), times PlantSlotLook.LockedTint, under the vines and the padlock. the
    // black square the Locked overlay had over the frame and icon is made invisible. it stays on: it's what
    // takes the pointer for the overlay (its hover tooltip and its click to unlock), the vines and padlock
    // don't
    private bool lockedDimCleared;

    private void DarkenSkill(bool locked)
    {
        if (skillIcon != null) skillIcon.color = locked ? PlantSlotLook.LockedTint : Color.white;
        if (!lockedDimCleared && lockedSkillSquare != null)
        {
            lockedDimCleared = true;
            Transform dim = lockedSkillSquare.transform.Find("Dim");
            Image dimImage = dim != null ? dim.GetComponent<Image>() : null;
            if (dimImage != null)
            {
                dim.gameObject.SetActive(true);
                dimImage.color = Color.clear;
                dimImage.raycastTarget = true;
            }
        }
    }

    // the pointer over a locked skill's icon shows what unlocking it costs (Tommy, 2026-11-26): the panel's
    // tooltip, kept up while the pointer stays and the skill stays locked
    private bool lockedSkillTipShown;

    private void RefreshLockedSkillTip(bool show, int cost)
    {
        if (show)
        {
            ShowTooltip($"LOCKED.\nUnlock for: {cost} Sun");
            lockedSkillTipShown = true;
        }
        else if (lockedSkillTipShown)
        {
            lockedSkillTipShown = false;
            HideTooltip();
        }
    }

    // the skill frame's wood (SkillFrameBase, next to the icon), found the first time it's needed
    private Image skillFrameBase;
    private Image SkillFrameBase
    {
        get
        {
            if (skillFrameBase == null && skillIcon != null && skillIcon.transform.parent != null)
            {
                Transform found = skillIcon.transform.parent.Find("SkillFrameBase");
                if (found != null) skillFrameBase = found.GetComponent<Image>();
            }
            return skillFrameBase;
        }
    }

    // Tommy's portrait frame (2026-11-26, PortraitFrameLook): 80 by 80 art px at the panel's 2 units a pixel
    // (160 square), a 64 px plant at exactly that pixel size in its 128 square opening, over the backdrop.
    // it's bigger than the old 128 frame, so the panel's top makes room (art px): the frame at 4, 16; the top
    // inset, the health bar, the stats and the action slot 10 further right and 10 narrower (the level bars
    // too, LevelBarX; the inset now ends with the frame, TopInsetHeight); the stats' second column 7 further
    // left and its numbers 2 narrower, so the row stays clear of the skill square; the paths and mulches 14
    // lower and the panel 14 taller (16 for an insect: PlantPanelHeight, InsectPanelHeight). ApplyElement
    // still tints the band (portraitFrame) and the backdrop (portraitBackdrop); the frame keeps the
    // portrait's hover tooltip (PlantIconTooltip). runs before the targeting button is made, which takes its
    // slot's width
    private const int FrameShiftX = 10;
    private const int LowerShift = 14;

    private void UsePortraitFrame()
    {
        if (!PortraitFrameLook.Available || portraitFrame == null || portraitBackdrop == null || entityIcon == null) return;
        float size = PortraitFrameLook.Size;
        float fx = 4f * Px, fy = 16f * Px;
        PortraitFrameLook.TopLeft(portraitFrame.rectTransform, fx, fy, size, size);
        PortraitFrameLook.MakeFrame(portraitFrame);
        Rect opening = PortraitFrameLook.Opening;
        PortraitFrameLook.TopLeft(portraitBackdrop.rectTransform, fx + opening.x, fy + opening.y, opening.width, opening.height);
        PortraitFrameLook.MakeBackdrop(portraitBackdrop);

        float dx = FrameShiftX * Px;
        if (topInset != null) ShiftRight(topInset, dx);
        foreach (InfoBar bar in new[] { healthBar, oxygenBar, temperatureBar })
        {
            if (bar == null || bar.root == null) continue;
            ShiftRight(bar.root, dx);
            if (bar.text != null) bar.text.rectTransform.sizeDelta = new Vector2(bar.root.sizeDelta.x, bar.text.rectTransform.sizeDelta.y);
        }
        if (shiftedRoot != null && shiftedRoot.Find("Stats") is RectTransform stats)
        {
            stats.anchoredPosition += new Vector2(dx, 0f);
            stats.sizeDelta = new Vector2(70f * Px, stats.sizeDelta.y);
            foreach (RectTransform child in stats)
            {
                bool secondColumn = child.name.StartsWith("Stat3") || child.name.StartsWith("Stat4");
                if (secondColumn) child.anchoredPosition -= new Vector2(7f * Px, 0f);
                if (child.name.EndsWith("Text")) child.sizeDelta = new Vector2(20f * Px, child.sizeDelta.y);
            }
        }
        if (actionSlot != null) ShiftRight(actionSlot, dx);
        if (lowerSection != null) ((RectTransform)lowerSection.transform).anchoredPosition += new Vector2(0f, -LowerShift * Px);
    }

    // dx canvas units further right and as much narrower, its right edge kept
    private static void ShiftRight(RectTransform r, float dx)
    {
        r.anchoredPosition += new Vector2(dx, 0f);
        r.sizeDelta -= new Vector2(dx, 0f);
    }

    // the plant (or insect) as big as fits the portrait window, bottom centred
    private void SetPortrait(Sprite sprite)
    {
        if (entityIcon == null) return;
        entityIcon.sprite = sprite;
        entityIcon.enabled = sprite != null;
        if (sprite == null) return;
        RectTransform window = entityIcon.rectTransform.parent as RectTransform;
        Vector2 area = window != null ? window.rect.size : new Vector2(112f, 112f);
        Vector2 size = sprite.rect.size;
        float scale = Mathf.Min(area.x / Mathf.Max(1f, size.x), area.y / Mathf.Max(1f, size.y));
        RectTransform rect = entityIcon.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(Mathf.Round(size.x * scale), Mathf.Round(size.y * scale));
    }

    // --- Action buttons ---

    private void ArrangeActionButtons(Plant plant)
    {
        bool targeting = plant.UsesTargeting;
        if (targetingControl != null)
            targetingControl.SetActive(targeting);
        if (autoCastButton != null)
        {
            autoCastButton.gameObject.SetActive(plant.UsesAutoCast);
            Transform home = targeting || actionSlot == null ? sideButtons : actionSlot;
            if (home != null && autoCastButton.transform.parent != home)
                autoCastButton.transform.SetParent(home, false);
            ((RectTransform)autoCastButton.transform).anchoredPosition = Vector2.zero;
        }
        if (relocateMinionsButton != null)
            relocateMinionsButton.gameObject.SetActive(plant.CommandsMinions);
        if (flyingToggleButton != null)
            flyingToggleButton.gameObject.SetActive(plant.UsesFlyingToggle);
        if (radiusToggleButton != null)
            radiusToggleButton.gameObject.SetActive(plant.UsesRadiusToggle);
        if (transplantButton != null)
            transplantButton.gameObject.SetActive(TransplantFertilizerEffect.IsActive);
        StackSideButtons();
    }

    private void HideActionButtons()
    {
        if (targetingControl != null) targetingControl.SetActive(false);
        foreach (Button b in new[] { autoCastButton, relocateMinionsButton, flyingToggleButton, radiusToggleButton, transplantButton })
            if (b != null) b.gameObject.SetActive(false);
    }

    // the side buttons one under another, top aligned with the top inset, 3 art pixels apart
    private void StackSideButtons()
    {
        if (sideButtons == null) return;
        float y = 0f;
        foreach (Transform child in sideButtons)
        {
            if (!child.gameObject.activeSelf || !(child is RectTransform rect)) continue;
            rect.anchoredPosition = new Vector2(0f, y);
            y -= rect.sizeDelta.y + SideButtonGap * Px;
        }
    }

    // --- Stats and bars ---

    protected override void RefreshStats()
    {
        statusEffectPanel?.Refresh();

        if (selectedPlant != null)
        {
            RefreshHealthBar();
            ApplyLayout(RefreshLevelBars(selectedPlant), true);
        }
        else if (selectedInsect != null)
        {
            entityNameText.text = selectedInsect.GetName();
            RefreshHealthBar();
            ApplyLayout(RefreshLevelBars(null), false);
        }
    }

    // panel height, top inset height and how far down everything under the level bars sits
    private void ApplyLayout(int levelBars, bool isPlant)
    {
        if (levelBars == shownLevelBars && isPlant == shownAsPlant) return;
        shownLevelBars = levelBars;
        shownAsPlant = isPlant;
        int d = LevelBarStep * levelBars;
        if (panelRect != null)
            panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, ((isPlant ? PlantPanelHeight : InsectPanelHeight) + d) * Px);
        if (topInset != null)
            topInset.sizeDelta = new Vector2(topInset.sizeDelta.x, (TopInsetHeight + d) * Px);
        if (shiftedRoot != null)
            shiftedRoot.anchoredPosition = new Vector2(shiftedRoot.anchoredPosition.x, -d * Px);
    }

    // the art pixels a bar's fill spans at 100%: the bar frame's width less its 1 pixel border on
    // each side (142 since the panel matches the right column's width)
    private static float InnerWidth(RectTransform inside)
    {
        RectTransform bar = inside != null ? inside.parent as RectTransform : null;
        return bar != null ? Mathf.Max(1f, Mathf.Round(bar.rect.width / Px) - 2f) : 1f;
    }

    // fill width in whole art pixels, rounded down
    private static void SetFill(Image fill, Image shine, float fraction)
    {
        if (fill == null) return;
        float width = Mathf.Floor(InnerWidth(fill.rectTransform) * Mathf.Clamp01(fraction)) * Px;
        // each image on its own, no array made per call
        ApplyFillWidth(fill, width);
        ApplyFillWidth(shine, width);
    }

    private static void ApplyFillWidth(Image image, float width)
    {
        if (image == null) return;
        RectTransform rect = image.rectTransform;
        rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        image.enabled = width > 0f;
    }

    private void RefreshHealthBar()
    {
        float health, maxHealth, shield = 0f;
        Entity entity;

        if (selectedPlant != null)
        {
            entity    = selectedPlant;
            health    = selectedPlant.health;
            maxHealth = selectedPlant.maxHealth;
            shield    = selectedPlant.TotalShield;
        }
        else if (selectedInsect != null)
        {
            entity    = selectedInsect;
            health    = selectedInsect.health;
            maxHealth = selectedInsect.maxHealth;
            shield    = selectedInsect.TotalShield;
        }
        else return;

        float pct = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;
        if (entity != _healthBarEntity)
        {
            _healthBarEntity = entity;
            _displayedHealthPct = pct;
            _chunkPct = pct;
            _lastPctForChunk = pct;
            _chunkDelayTimer = 0f;
        }
        else
        {
            _displayedHealthPct = Mathf.Lerp(_displayedHealthPct, pct, 1f - Mathf.Exp(-HealthBarLerpSpeed * Time.unscaledDeltaTime));
        }

        if (healthBar == null) return;
        SetFill(healthBar.fill, healthBar.shine, _displayedHealthPct);
        if (healthBar.fill != null)
        {
            // the same green, yellow and red as the bar over the plant (PlantWidget)
            PlantWidgetStyle widget = PlantWidget.Style;
            healthBar.fill.color = widget != null ? widget.HealthColor(_displayedHealthPct)
                : _displayedHealthPct <= 0.25f ? Color.red : _displayedHealthPct <= 0.5f ? Color.yellow : Color.green;
        }

        RefreshDamageChunk(entity, pct);

        // the same numbers give the same text, so it is only made again when one changed
        if (healthBar.text != null && (health != shownHealth || maxHealth != shownMaxHealth || shield != shownShield))
        {
            shownHealth = health;
            shownMaxHealth = maxHealth;
            shownShield = shield;
            string shieldSuffix = shield > 0f ? $" <color=#888888>(+{shield:F0})</color>" : "";
            healthBar.text.text = $"{health:F0}/{maxHealth:F0}{shieldSuffix}";
        }
    }

    // hold then drain, like the plant widget's chunk: freezes at the share before a hit, waits
    // DamageChunkDelay with no further drop, then eases down at the same speed as the fill
    private void RefreshDamageChunk(Entity entity, float pct)
    {
        Image chunk = healthBar != null ? healthBar.chunk : null;
        if (chunk == null) return;

        if (pct < _lastPctForChunk)
        {
            _chunkDelayTimer = DamageChunkDelay;
        }
        else if (pct > _chunkPct)
        {
            _chunkPct = pct;
            _chunkDelayTimer = 0f;
        }
        _lastPctForChunk = pct;

        if (_chunkDelayTimer > 0f)
            _chunkDelayTimer -= Time.unscaledDeltaTime;
        else if (_chunkPct > pct)
            _chunkPct = Mathf.Lerp(_chunkPct, pct, 1f - Mathf.Exp(-HealthBarLerpSpeed * Time.unscaledDeltaTime));

        bool visible = _chunkPct > pct + 0.001f;
        if (!visible) { chunk.enabled = false; return; }
        SetFill(chunk, null, _chunkPct);
        bool ally = entity is Plant || (entity is Insect insect && insect.team == Team.Friendly);
        chunk.color = ally ? AllyChunkColor : EnemyChunkColor;
    }

    // the level's extra bars, top to bottom: oxygen while submerged (underwater levels), else
    // temperature on hot and cold levels (the same rule as the old single bar). returns how many show
    private int RefreshLevelBars(Plant plant)
    {
        bool oxygen = plant != null && plant.HasEffect<SubmergedEffect>();
        bool temperature = plant != null && !oxygen && WeatherManager.instance != null &&
            (WeatherManager.instance.temperature == TemperatureType.Hot ||
             WeatherManager.instance.temperature == TemperatureType.Cold);

        int count = 0;
        if (ShowBar(oxygenBar, oxygen, count)) { RefreshOxygenBar(plant); count++; }
        if (ShowBar(temperatureBar, temperature, count)) { RefreshTemperatureBar(plant); count++; }
        return count;
    }

    private static bool ShowBar(InfoBar bar, bool show, int slot)
    {
        if (bar == null || bar.root == null) return false;
        if (bar.root.gameObject.activeSelf != show) bar.root.gameObject.SetActive(show);
        if (show) bar.root.anchoredPosition = new Vector2(LevelBarX * Px, -(FirstLevelBarY + LevelBarStep * slot) * Px);
        return show;
    }

    private void RefreshOxygenBar(Plant plant)
    {
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        SetFill(oxygenBar.fill, oxygenBar.shine, plant.air / 100f);
        if (oxygenBar.fill != null) oxygenBar.fill.color = plant.air <= 0f ? style.oxygenEmpty : style.oxygen;
        if (oxygenBar.text != null && plant.air != shownAir)
        {
            shownAir = plant.air;
            oxygenBar.text.text = $"{plant.air:F0}/100";
        }
    }

    private void RefreshTemperatureBar(Plant plant)
    {
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        float min = plant.temperatureMin, max = plant.temperatureMax;
        SetFill(temperatureBar.fill, temperatureBar.shine, Fraction(plant.temperature, min, max));

        string state;
        Color color;
        if (plant.temperature < plant.comfortMin)      { state = "Cold";    color = style.temperatureCold; }
        else if (plant.temperature > plant.comfortMax) { state = "Hot";     color = style.temperatureHot; }
        else                                           { state = "Comfort"; color = style.temperatureComfort; }
        if (temperatureBar.fill != null) temperatureBar.fill.color = color;
        if (temperatureBar.text != null && (plant.temperature != shownTemperature || state != shownTemperatureState))
        {
            shownTemperature = plant.temperature;
            shownTemperatureState = state;
            temperatureBar.text.text = $"{plant.temperature:F1}° {state}";
        }

        if (comfortMinImage == null && tempComfortMinIndicator != null) comfortMinImage = tempComfortMinIndicator.GetComponent<Image>();
        if (comfortMaxImage == null && tempComfortMaxIndicator != null) comfortMaxImage = tempComfortMaxIndicator.GetComponent<Image>();
        PlaceComfortMark(tempComfortMinIndicator, comfortMinImage, Fraction(plant.comfortMin, min, max));
        PlaceComfortMark(tempComfortMaxIndicator, comfortMaxImage, Fraction(plant.comfortMax, min, max));
    }

    private static float Fraction(float value, float min, float max) =>
        max > min ? Mathf.Clamp01((value - min) / (max - min)) : 0f;

    // a 1 pixel mark inside the bar's frame, on the art pixel for that share of the bar
    private static void PlaceComfortMark(RectTransform mark, Image image, float fraction)
    {
        if (mark == null) return;
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        if (image != null) image.color = style.comfortMark;
        float inner = InnerWidth(mark);
        float x = 1f + Mathf.Clamp(Mathf.Round(inner * fraction), 0f, inner - 1f);
        mark.anchoredPosition = new Vector2(x * Px, mark.anchoredPosition.y);
    }

    // --- Paths ---

    private void RefreshPaths()
    {
        if (selectedPlant == null) return;
        bool allLocked = AllPathsLockedForLevel1Intro;
        bool otherLocked = OtherPathsTutorialBlocked;
        int sun = GameManager.instance != null ? GameManager.instance.SunCount : 0;
        Plant p = selectedPlant;

        RefreshPath(path1, 0, p.GetPath1Name(), p.path1Level, p.effectivePath1Level, true, p.GetPath1Cost(), allLocked, sun);
        RefreshPath(path2, 1, p.GetPath2Name(), p.path2Level, p.effectivePath2Level, true, p.GetPath2Cost(), otherLocked, sun);
        RefreshPath(path3, 2, p.GetPath3Name(), p.path3Level, p.effectivePath3Level, p.path3Unlocked, p.GetPath3Cost(), otherLocked, sun);
        RefreshLockedSkill(p, sun);
    }

    // round 6: while the skill isn't unlocked, vines over the Skill strip with a padlock, the unlock
    // price at twice the font size and the sun centred as one group (padlock, 5 px, price, 4 px,
    // sun), and vines with a bigger padlock over the skill square. hovering either lightens both.
    // the price and sun go grey while it can't be afforded. a click on either unlocks (wired to
    // OnPath3UpgradeClicked in the prefab)
    private void RefreshLockedSkill(Plant p, int sun)
    {
        bool locked = !p.path3Unlocked;
        if (lockedSkillStrip != null && lockedSkillStrip.activeSelf != locked) lockedSkillStrip.SetActive(locked);
        if (lockedSkillSquare != null && lockedSkillSquare.activeSelf != locked) lockedSkillSquare.SetActive(locked);
        DarkenSkill(locked);
        int cost = p.GetPath3Cost();
        RefreshLockedSkillTip(locked && lockedSquareHover != null && lockedSquareHover.IsHovered, cost);
        if (!locked) return;

        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        bool affordable = sun >= cost;
        bool hover = (lockedSkillHover != null && lockedSkillHover.IsHovered)
            || (lockedSquareHover != null && lockedSquareHover.IsHovered);
        Color vines = hover ? style.lockedVinesHover : style.lockedVines;
        Color glyph = hover ? style.lockedTextHover : style.lockedText;
        if (lockedStripVines != null) lockedStripVines.color = vines;
        if (lockedSquareVines != null) lockedSquareVines.color = vines;
        // Tommy's padlock (2026-11-25) is drawn in its own colours, only brightened under the pointer
        Color padlockColor = style.PadlockColor(hover);
        if (lockedStripPadlock != null) lockedStripPadlock.color = padlockColor;
        if (lockedSquarePadlock != null) lockedSquarePadlock.color = padlockColor;
        if (lockedPrice != null) lockedPrice.color = affordable ? glyph : style.locked.text;
        if (lockedSun != null) lockedSun.color = affordable ? style.normal.sun : style.locked.sun;

        // compared as a number, so the text is only made when the price changed
        if (lockedPrice == null || cost == lockedPriceLaidOut) return;
        lockedPriceLaidOut = cost;
        string price = cost.ToString();
        lockedPrice.text = price;
        RectTransform strip = lockedSkillStrip.transform as RectTransform;
        float stripWidth = strip != null ? Mathf.Round(strip.rect.width / Px) : 214f;
        float priceWidth = Mathf.Ceil(lockedPrice.GetPreferredValues(price).x / Px);
        float groupX = Mathf.Floor((stripWidth - (33f + priceWidth)) / 2f);
        RectTransform priceRect = lockedPrice.rectTransform;
        priceRect.sizeDelta = new Vector2(priceWidth * Px, priceRect.sizeDelta.y);
        priceRect.anchoredPosition = new Vector2((groupX + 15f) * Px, priceRect.anchoredPosition.y);
        if (lockedStripPadlock != null)
        {
            // Tommy's padlock, 22 units wide here (11 px), starts the group: padlock, 4 px, price, 4 px, sun
            // (14 px), 33 px and the price in all, centred on the strip
            RectTransform padlock = lockedStripPadlock.rectTransform;
            padlock.anchoredPosition = new Vector2(groupX * Px, padlock.anchoredPosition.y);
        }
        if (lockedSun != null)
        {
            RectTransform sunRect = lockedSun.rectTransform;
            sunRect.anchoredPosition = new Vector2((groupX + 15f + priceWidth + 4f) * Px, sunRect.anchoredPosition.y);
        }
    }

    // one path strip: label, five pips and the buy button. the button's look: max at 5/5, locked
    // while it can't be afforded, is capped early by the level or held by the tutorial, else normal.
    // a path that isn't unlocked yet (the skill path) looks the same way as the others (Tommy,
    // 2026-11-20): normal when its unlock can be afforded, locked when not. its lock glyph stays in
    // place of the plus, and it unlocks on click when the player has the sun
    private void RefreshPath(PathRow row, int pathIndex, string label, int level, int effectiveLevel,
        bool unlocked, int cost, bool blocked, int sun)
    {
        if (row == null) return;
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        if (row.nameText != null) row.nameText.text = label;

        bool trueMax = level >= Plant.absoluteLevelCap;
        bool capped = !trueMax && unlocked && level >= Plant.pathLevelCap;
        bool affordable = sun >= cost;
        bool canBuyNext = unlocked && !trueMax && !capped && !blocked && affordable;

        if (row.button != null) row.button.interactable = !trueMax && !capped && !blocked;
        if (row.look != null)
            row.look.SetLook(trueMax ? PixelButton.Look.Max
                : capped || blocked || !affordable ? PixelButton.Look.Locked
                : PixelButton.Look.Normal);
        if (row.plus != null) row.plus.SetActive(unlocked && !trueMax && !capped);
        if (row.lockGlyph != null) row.lockGlyph.SetActive(!trueMax && (!unlocked || capped));
        if (row.sun != null) row.sun.SetActive(!trueMax && !capped);
        if (row.priceText != null)
        {
            row.priceText.gameObject.SetActive(!capped);
            int price = trueMax ? ShownMaxPrice : cost;
            if (price != row.shownPrice)
            {
                row.shownPrice = price;
                row.priceText.text = trueMax ? "Max" : cost.ToString();
            }
        }

        if (row.pips == null) return;
        int count = row.pips.Length;
        if (row.flashUntil == null || row.flashUntil.Length != count) row.flashUntil = new float[count];
        if (row.shownLevel >= 0 && level > row.shownLevel)
            for (int i = row.shownLevel; i < level && i < count; i++)
                row.flashUntil[i] = Time.unscaledTime + style.buyFlashSeconds;
        row.shownLevel = level;

        Color pathColor = style.PathColor(pathIndex);
        bool bonus = !trueMax && effectiveLevel > level;
        float nextAlpha = Wave(style.nextPulseSeconds, style.nextPulseMin, style.nextPulseMax);
        float breath = Wave(style.maxBreathSeconds, style.maxBreathMin, 1f);

        for (int i = 0; i < count; i++)
        {
            Image pip = row.pips[i];
            if (pip == null) continue;
            Sprite sprite;
            float alpha = 1f;
            if (trueMax) sprite = style.breatheMax ? style.pipFilled : style.pipMax;
            else if (i < level) sprite = style.pipFilled;
            else if (bonus && i < effectiveLevel) { sprite = style.pipFilled; alpha = style.bonusPipAlpha; }
            else { sprite = style.pipEmpty; if (!unlocked) alpha = style.lockedPathAlpha; }
            if (sprite != null) pip.sprite = sprite;
            pip.color = WithAlpha(pathColor, alpha);

            Image next = row.pipNext != null && i < row.pipNext.Length ? row.pipNext[i] : null;
            if (next != null)
            {
                next.enabled = canBuyNext && i == level;
                next.color = WithAlpha(pathColor, nextAlpha);
            }

            Image flash = row.pipFlash != null && i < row.pipFlash.Length ? row.pipFlash[i] : null;
            if (flash != null)
            {
                float flashAlpha = Time.unscaledTime < row.flashUntil[i] ? 1f
                    : trueMax && style.breatheMax ? breath : 0f;
                flash.enabled = flashAlpha > 0f;
                flash.color = WithAlpha(pathColor, flashAlpha);
            }
        }
    }

    private static float Wave(float seconds, float min, float max) =>
        Mathf.Lerp(min, max, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI / Mathf.Max(0.01f, seconds)));

    private static Color WithAlpha(Color c, float a) { c.a = a; return c; }

    // --- Skill ---

    private void RefreshSkillButton()
    {
        if (skillButton == null) return;
        Plant p = selectedPlant;
        bool unlocked = p.path3Unlocked;
        bool silenced = p.IsSilenced;
        bool cooling = unlocked && p.skillCooldownTimer > 0f;
        skillButton.interactable = p.SkillReady;

        // not unlocked: the vines overlay dims it instead (RefreshLockedSkill)
        float cover = !unlocked ? 0f
            : silenced || p.IsChanneling ? 1f
            : cooling && p.skillCooldown > 0f ? Mathf.Clamp01(p.skillCooldownTimer / p.skillCooldown)
            : cooling ? 1f : 0f;
        if (skillCooldownOverlay != null)
        {
            skillCooldownOverlay.enabled = cover > 0f;
            skillCooldownOverlay.fillAmount = cover;
            skillCooldownOverlay.color = PlantInfoPanelStyle.Instance.cooldownOverlay;
        }
        bool smallLock = unlocked && silenced;
        if (skillLock != null && skillLock.activeSelf != smallLock)
            skillLock.SetActive(smallLock);
        // whole seconds, so the text only changes once a second (-1: shows nothing)
        int seconds = cooling && !silenced ? Mathf.CeilToInt(p.skillCooldownTimer) : -1;
        if (skillCooldownText != null && seconds != shownSkillSeconds)
        {
            shownSkillSeconds = seconds;
            skillCooldownText.text = seconds != -1 ? seconds.ToString() : "";
        }
    }

    private void RefreshTransplantButton()
    {
        if (transplantButton == null) return;
        bool fertilizerActive = TransplantFertilizerEffect.IsActive;
        if (transplantButton.gameObject.activeSelf != fertilizerActive)
        {
            transplantButton.gameObject.SetActive(fertilizerActive);
            StackSideButtons();
        }
        if (!fertilizerActive) return;

        // visible confirmation that the click registered: without it, picking a destination tile
        // right after clicking Move looked like the button did nothing at all
        bool targeting = PlantSelector.instance != null && PlantSelector.instance.transplantMode
            && PlantSelector.instance.transplantingPlant == selectedPlant;
        bool ready = selectedPlant.TransplantReady;
        transplantButton.interactable = ready && !targeting;
        // ShownNoPrice for "Pick a tile", ShownMaxPrice for "Move", else the whole seconds left
        int state = targeting ? ShownNoPrice : ready ? ShownMaxPrice : Mathf.CeilToInt(selectedPlant.transplantCooldownTimer);
        if (transplantCooldownText != null && (state != shownTransplant || !transplantShown))
        {
            transplantShown = true;
            shownTransplant = state;
            transplantCooldownText.text = targeting ? "Pick a tile"
                : ready ? "Move"
                : $"Move {state}s";
        }
    }

    private bool transplantShown;

    private void RefreshAutoCastButton()
    {
        if (autoCastButton == null || !selectedPlant.UsesAutoCast) return;
        bool targeting = SkillTargetingManager.instance.IsPlantTargeting;
        if (autoCastButtonText == null) return;
        // 2 picking, 1 on, 0 off; built again only when that or the plant's label changes
        int state = targeting ? 2 : selectedPlant.IsAutoCasting ? 1 : 0;
        string label = selectedPlant.AutoCastLabel;
        if (state == shownAutoCast && label == shownAutoCastLabel) return;
        shownAutoCast = state;
        shownAutoCastLabel = label;
        autoCastButtonText.text = targeting ? "Pick one" : $"{ShortAutoCastLabel(selectedPlant)}: {(selectedPlant.IsAutoCasting ? "On" : "Off")}";
    }

    // "Auto Cast" doesn't fit the 73 pixel button with its state, so it reads "Auto"
    private static string ShortAutoCastLabel(Plant plant) =>
        plant.AutoCastLabel == "Auto Cast" ? "Auto" : plant.AutoCastLabel;

    private static string FlyingLabel(Plant plant) => plant.prioritizeFlying ? "Flyers" : "Default";
    private static string RadiusLabel(Plant plant) => $"Orbit {plant.orbitRadius}";

    // holding shift while clicking an upgrade button repeats it until it stops succeeding
    // (level cap reached or sun runs out), instead of just the one level a plain click grants
    private static bool IsShiftHeld() =>
        Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

    // button callbacks, wired in the prefab
    // true before Level1PlantInfo has taught the upgrade system at all: blocks every path, on any
    // plant, until its Attack upgrade page is reached (see TutorialManager)
    private static bool AllPathsLockedForLevel1Intro =>
        TutorialManager.instance != null && TutorialManager.instance.Level1UpgradesLocked();

    // true while Level1PlantInfo's upgrade page is up: it's about the Attack (path 1) upgrade, so
    // path 2 and 3 are blocked for the duration (see TutorialContent). also true for the stricter
    // lock above, since that blocks everything including path 1
    private static bool OtherPathsTutorialBlocked =>
        AllPathsLockedForLevel1Intro ||
        (TutorialManager.instance != null && TutorialManager.instance.IsWaitingForEvent(TutorialEvents.UpgradeAcornAttackPath));

    public void OnPath1UpgradeClicked()
    {
        if (selectedPlant == null || AllPathsLockedForLevel1Intro) return;
        bool success;
        if (IsShiftHeld())
        {
            success = false;
            while (selectedPlant.UpgradePath1()) success = true;
        }
        else
        {
            success = selectedPlant.UpgradePath1();
        }

        if (success && selectedPlant.data.plantName == "AcornKnight")
            TutorialManager.instance?.NotifyEvent(TutorialEvents.UpgradeAcornAttackPath);

        RefreshPaths();
    }

    public void OnPath2UpgradeClicked()
    {
        if (selectedPlant == null || OtherPathsTutorialBlocked) return;
        if (IsShiftHeld())
            while (selectedPlant.UpgradePath2()) { }
        else
            selectedPlant.UpgradePath2();
        RefreshPaths();
    }

    public void OnPath3UpgradeClicked()
    {
        if (selectedPlant == null || OtherPathsTutorialBlocked) return;
        bool shift = IsShiftHeld();
        if (!selectedPlant.path3Unlocked)
        {
            // level 1 keeps skills locked until its skill tutorial asks for the unlock (Tommy)
            if (TutorialManager.instance != null && TutorialManager.instance.Level1SkillLocked()) return;
            if (selectedPlant.UnlockPath3() && selectedPlant.data.plantName == "AcornKnight")
                TutorialManager.instance?.NotifyEvent(TutorialEvents.UnlockAcornSkill);
        }
        else if (!shift)
            selectedPlant.UpgradePath3();

        if (shift)
            while (selectedPlant.UpgradePath3()) { }

        RefreshPaths();
    }

    public void OnSkillButtonClicked()
    {
        if (selectedPlant == null || !selectedPlant.SkillReady) return;
        selectedPlant.TriggerSkill();
    }

    // enters transplant mode on the selected plant: the next tile click (if valid for this plant,
    // see Tile.OnMouseDown) relocates it there for free. cancels like every other mode, via right
    // click or Escape (see PlantSelector.Update)
    public void OnTransplantButtonClicked()
    {
        if (selectedPlant == null || !TransplantFertilizerEffect.IsActive || !selectedPlant.TransplantReady) return;
        PlantSelector.instance?.SetTransplantMode(selectedPlant);
    }

    public void OnAutoCastButtonClicked()
    {
        if (selectedPlant == null || !selectedPlant.UsesAutoCast) return;
        selectedPlant.ToggleAutoCast();
    }

    // round 6: two arrow buttons around a display with the mode's name. left steps back through the
    // modes, right forward, both wrapping round. since Tommy's 2026-11-26 the arrows are hidden and the
    // display is clicked instead (UseTargetingClicker): left click next, right click back
    public void OnTargetingPrevClicked() => StepTargeting(-1);
    public void OnTargetingNextClicked() => StepTargeting(1);

    // the arrows hidden and the display made a button like the upgrade buttons (Tommy, 2026-11-26): Tommy's
    // small button, the slot's full width (stretched at the art's plain middle, sprite borders 21 and 33),
    // at its own height, 2 units an art pixel, its trim over the wood; the mode's capitals in the middle of
    // the wood (rows 3 to 15 of 19). the button itself takes the pointer, the mode's name doesn't
    // (TargetingClicker does the clicks, the look and the tooltip)
    private const float TargetingLabelTop = 3f * Px;

    private void UseTargetingClicker()
    {
        if (targetingControl == null || targetingModeText == null) return;
        foreach (string arrow in new[] { "Prev", "Next" })
        {
            Transform found = targetingControl.transform.Find(arrow);
            if (found != null) found.gameObject.SetActive(false);
        }
        RectTransform display = targetingModeText.transform.parent as RectTransform;
        if (display == null || display == targetingControl.transform) return;
        RectTransform slot = (RectTransform)targetingControl.transform;
        Image image = display.GetComponent<Image>();
        PlantInfoPanelStyle style = PlantInfoPanelStyle.Instance;
        float height = display.sizeDelta.y;
        if (image != null && style.upgradeButton != null)
        {
            image.sprite = style.upgradeButton;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            height = style.upgradeButton.rect.height * Px;
            if (style.upgradeButtonTrim != null && display.Find("Trim") == null)
            {
                GameObject go = new GameObject("Trim", typeof(RectTransform), typeof(Image));
                go.layer = display.gameObject.layer;
                RectTransform r = (RectTransform)go.transform;
                r.SetParent(display, false);
                r.SetAsFirstSibling();
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = r.offsetMax = Vector2.zero;
                Image trim = go.GetComponent<Image>();
                trim.sprite = style.upgradeButtonTrim;
                trim.type = Image.Type.Sliced;
                trim.pixelsPerUnitMultiplier = 1f;
                trim.raycastTarget = false;
            }
        }
        display.anchoredPosition = new Vector2(0f, 0f);
        display.sizeDelta = new Vector2(slot.rect.width, height);
        if (image != null) image.raycastTarget = true;
        targetingModeText.raycastTarget = false;
        targetingClicker = display.GetComponent<TargetingClicker>();
        if (targetingClicker == null) targetingClicker = display.gameObject.AddComponent<TargetingClicker>();
        targetingClicker.ui = this;
        targetingClicker.Build(image, targetingModeText, TargetingLabelTop);
    }

    private TargetingClicker targetingClicker;

    // what each mode picks among the insects in range (Plant.FindNearest and the others)
    private static string TargetingMeaning(TARGETING mode) => mode switch
    {
        TARGETING.Nearest   => "the closest to the plant",
        TARGETING.First     => "the furthest along the path",
        TARGETING.Last      => "the furthest back on the path",
        TARGETING.Strongest => "the highest max health",
        TARGETING.Fastest   => "the fastest right now (slows count)",
        TARGETING.Slowest   => "the slowest right now (slows count)",
        _ => "",
    };

    // the targeting display's tooltip: every mode, the plant's own one marked, and the clicks under them
    public void ShowTargetingTooltip()
    {
        if (selectedPlant == null || !selectedPlant.UsesTargeting) return;
        var modes = new System.Text.StringBuilder();
        foreach (TARGETING mode in System.Enum.GetValues(typeof(TARGETING)))
        {
            if (modes.Length > 0) modes.Append('\n');
            if (mode == selectedPlant.targeting)
                modes.Append($"<color=green><b>> {mode}</b></color>: {TargetingMeaning(mode)}");
            else
                modes.Append($"<b>{mode}</b>: <color=#BBBBBB>{TargetingMeaning(mode)}</color>");
        }
        ShowSectionedTooltip("Targeting", new[] { modes.ToString() },
            "<color=grey>[Left click to go next]\n[Right click to go back]</color>");
    }

    private void StepTargeting(int step)
    {
        if (selectedPlant == null || !selectedPlant.UsesTargeting) return;
        int count = System.Enum.GetValues(typeof(TARGETING)).Length;
        selectedPlant.targeting = (TARGETING)((((int)selectedPlant.targeting + step) % count + count) % count);
        ShowTargetingMode(selectedPlant);
    }

    // the display holds 72 art pixels at the game's width, so every mode fits by its full name,
    // "Strongest" included (the 205 pixel design shortens it to "Strong")
    private void ShowTargetingMode(Plant plant)
    {
        if (targetingModeText == null || plant == null) return;
        targetingModeText.text = plant.targeting.ToString();
        PixelLayout.CentreOnPixels(targetingModeText, Px);
    }

    // toggle the selected plant between prioritizing flyers and normal targeting
    public void OnFlyingToggleClicked()
    {
        if (selectedPlant == null || !selectedPlant.UsesFlyingToggle) return;
        selectedPlant.prioritizeFlying = !selectedPlant.prioritizeFlying;
        if (flyingToggleText != null)
            flyingToggleText.text = FlyingLabel(selectedPlant);
    }

    // cycle the selected plant's fixed orbit radius, wrapping back to 1 past its current max
    public void OnRadiusToggleClicked()
    {
        if (selectedPlant == null || !selectedPlant.UsesRadiusToggle) return;
        int max = Mathf.Max(1, selectedPlant.MaxOrbitRadius);
        selectedPlant.orbitRadius = selectedPlant.orbitRadius >= max ? 1 : selectedPlant.orbitRadius + 1;
        if (radiusToggleText != null)
            radiusToggleText.text = RadiusLabel(selectedPlant);
    }

    // aim a point, then relocate the selected plant's minion formation there
    public void OnRelocateMinionsClicked()
    {
        if (selectedPlant == null || !selectedPlant.CommandsMinions) return;
        Plant plant = selectedPlant;   // capture in case selection changes before confirm
        // clamp the aim point to within the plant's attack range
        SkillTargetingManager.instance.BeginTargeting(relocateIndicatorRadius,
            point => plant.RelocateMinionFormation(point), plant.transform.position, plant.attackRange);
    }

    // --- Tooltips ---

    // the panel's own spot and its text's font size as built: every layout starts from them, so a
    // long tooltip never leaves the next one moved or shrunk
    private bool tooltipBaseRead;
    private Vector2 tooltipBasePosition;
    private float tooltipBaseFontSize;
    private Vector2Int tooltipLaidOutScreen;
    // the text sits 20 above and 20 below inside the panel (its rect's height is the panel's minus 40)
    private const float TooltipPadding = 40f;
    private const float TooltipMinFontSize = 6f;
    private static readonly Vector3[] tooltipCorners = new Vector3[4];

    // path tooltips call this every frame while hovered: it lays the panel out again only when the
    // text or the screen size changed. an Attack, Passive or Skill text (one a plant marked with
    // Plant.MaxLevelBreak) is shown in sections (LayoutSections), anything else as one text
    public void ShowTooltip(string text)
    {
        if (tooltipPanel == null || tooltipText == null) return;
        ReadTooltipBase();
        bool wasShown = tooltipPanel.activeSelf;
        tooltipPanel.SetActive(true);
        Vector2Int screen = new Vector2Int(Screen.width, Screen.height);
        if (wasShown && text == shownTooltip && screen == tooltipLaidOutScreen) return;
        if (SplitSections(text, out string title, out string[] parts, out string footer))
            LayoutSections(title, parts, footer);
        else
        {
            tooltipText.text = text;
            LayoutTooltip();
        }
        shownTooltip = text;
    }

    // the text the panel shows (null after a text laid out some other way)
    private string shownTooltip;

    // a tooltip in sections (a plant's or an insect's description, from the icon): the title on top and a
    // framed box per section, empty ones left out, then the footer. laid out again only when one changed
    public void ShowSectionedTooltip(string title, string[] sections, string footer = null)
    {
        if (tooltipPanel == null || tooltipText == null) return;
        ReadTooltipBase();
        bool wasShown = tooltipPanel.activeSelf;
        tooltipPanel.SetActive(true);
        sections ??= System.Array.Empty<string>();
        string key = (title ?? "") + "\u0001" + string.Join("\u0001", sections) + "\u0001" + (footer ?? "");
        Vector2Int screen = new Vector2Int(Screen.width, Screen.height);
        if (wasShown && key == shownTooltip && screen == tooltipLaidOutScreen) return;
        LayoutSections(title, sections, footer);
        shownTooltip = key;
    }

    private void ReadTooltipBase()
    {
        if (tooltipBaseRead) return;
        tooltipBaseRead = true;
        tooltipBasePosition = ((RectTransform)tooltipPanel.transform).anchoredPosition;
        tooltipBaseFontSize = tooltipText.fontSize;
    }

    // as tall as its text, at most the screen's height (Tommy, 2026-11-23). text too long for that
    // gets the biggest font size that fits instead of spilling over, and the panel is kept on screen.
    // everything is measured in the root canvas's own units: the canvas renders through the camera,
    // so world corners are not screen pixels (comparing them to Screen.height made the panel jump
    // up and down every frame once it was taller than the screen)
    private void LayoutTooltip()
    {
        HideSections();
        shownTooltip = null;
        // a panel just switched on has its text's measurements made here first
        Canvas.ForceUpdateCanvases();
        RectTransform tipRect = (RectTransform)tooltipPanel.transform;
        Canvas canvas = tooltipPanel.GetComponentInParent<Canvas>();
        RectTransform root = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
        float maxHeight = root != null ? root.rect.height : Screen.height;
        float maxTextHeight = maxHeight - TooltipPadding;

        tooltipText.fontSize = tooltipBaseFontSize;
        float textHeight = tooltipText.preferredHeight;
        if (textHeight > maxTextHeight)
        {
            // halving the range between the smallest size and the normal one
            float fits = TooltipMinFontSize, tooBig = tooltipBaseFontSize;
            for (int i = 0; i < 8; i++)
            {
                float size = (fits + tooBig) * 0.5f;
                tooltipText.fontSize = size;
                if (tooltipText.preferredHeight <= maxTextHeight) fits = size;
                else tooBig = size;
            }
            tooltipText.fontSize = fits;
            textHeight = tooltipText.preferredHeight;
        }
        tipRect.anchoredPosition = tooltipBasePosition;
        tipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Min(textHeight + TooltipPadding, maxHeight));
        tooltipLaidOutScreen = new Vector2Int(Screen.width, Screen.height);
        if (root != null) KeepTooltipOnScreen(tipRect, root);
    }

    // its top and bottom in the root canvas's units; at most screen high, so one move puts it all on
    private static void KeepTooltipOnScreen(RectTransform tipRect, RectTransform root)
    {
        tipRect.GetWorldCorners(tooltipCorners);
        float bottom = root.InverseTransformPoint(tooltipCorners[0]).y;
        float top = root.InverseTransformPoint(tooltipCorners[1]).y;
        float shift = 0f;
        if (top > root.rect.yMax) shift = root.rect.yMax - top;
        else if (bottom < root.rect.yMin) shift = root.rect.yMin - bottom;
        if (shift == 0f) return;
        Transform parent = tipRect.parent != null ? tipRect.parent : root;
        Vector3 move = parent.InverseTransformVector(root.TransformVector(new Vector3(0f, shift, 0f)));
        tipRect.anchoredPosition += new Vector2(0f, move.y);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    // --- The Attack, Passive and Skill tooltips in sections (Tommy, 2026-11-26) ---
    // a path's text (Plant.GetPath1Description and the others) is cut where the plant marked it: its title
    // on top, then three boxes in Tommy's section frame (SectionFrameLook), the description, the upgrades
    // and the max level bonus, then the level and the shift hint under them. the boxes are the kit's inset
    // box in the plant's element colour, like the panel; the texts are copies of the panel's own text

    private RectTransform sectionRoot;
    private TMP_Text sectionTitle, sectionFooter;
    private readonly System.Collections.Generic.List<Image> sectionBoxes = new System.Collections.Generic.List<Image>();
    private readonly System.Collections.Generic.List<TMP_Text> sectionTexts = new System.Collections.Generic.List<TMP_Text>();
    // canvas units: between two sections, and from a box's edge to its text (clear of the frame)
    private const float SectionGap = 8f;
    private const float SectionTextPadX = 16f;
    private const float SectionTextPadY = 14f;

    // false for a text no plant marked (an insect's, a hint): that one is shown whole
    private static bool SplitSections(string text, out string title, out string[] parts, out string footer)
    {
        title = footer = null;
        parts = null;
        if (string.IsNullOrEmpty(text)) return false;
        int max = text.IndexOf(Plant.MaxLevelBreak, System.StringComparison.Ordinal);
        if (max < 0) return false;
        // a plant that doesn't mark its upgrades (the retired ones) has them in its description's box
        int upgrades = text.IndexOf(Plant.UpgradesBreak, System.StringComparison.Ordinal);
        if (upgrades > max) upgrades = -1;
        string head = text.Substring(0, upgrades >= 0 ? upgrades : max);
        int lineEnd = head.IndexOf('\n');
        title = CleanSection(lineEnd >= 0 ? head.Substring(0, lineEnd) : head);
        string description = lineEnd >= 0 ? head.Substring(lineEnd + 1) : "";
        int upgradesStart = upgrades + Plant.UpgradesBreak.Length;
        string upgradeLines = upgrades >= 0 ? text.Substring(upgradesStart, max - upgradesStart) : "";
        string rest = text.Substring(max + Plant.MaxLevelBreak.Length);
        // the level line and the shift hint follow the max level bonus
        int level = rest.LastIndexOf("\n\nLevel: [", System.StringComparison.Ordinal);
        string bonus = level >= 0 ? rest.Substring(0, level) : rest;
        footer = level >= 0 ? CleanSection(rest.Substring(level)) : "";
        parts = new[] { CleanSection(description), CleanSection(upgradeLines), CleanSection(bonus) };
        return true;
    }

    private static string CleanSection(string s) =>
        s.Replace(Plant.UpgradesBreak, "").Replace(Plant.MaxLevelBreak, "").Trim();

    private void LayoutSections(string title, string[] parts, string footer)
    {
        BuildSections();
        while (sectionBoxes.Count < parts.Length) AddSection();
        tooltipText.gameObject.SetActive(false);
        sectionRoot.gameObject.SetActive(true);
        Color boxColor = TooltipFrameLook.InteriorFor(selectedPlant != null ? (Entity)selectedPlant : selectedInsect);
        sectionTitle.text = title;
        sectionTitle.gameObject.SetActive(!string.IsNullOrEmpty(title));
        sectionFooter.text = footer;
        sectionFooter.gameObject.SetActive(!string.IsNullOrEmpty(footer));
        for (int i = 0; i < sectionBoxes.Count; i++)
        {
            bool shown = i < parts.Length && !string.IsNullOrEmpty(parts[i]);
            sectionTexts[i].text = shown ? parts[i] : "";
            sectionTexts[i].gameObject.SetActive(shown);
            sectionBoxes[i].gameObject.SetActive(shown);
            sectionBoxes[i].color = boxColor;
            SectionFrameLook.Recolor(sectionBoxes[i]);
        }

        Canvas.ForceUpdateCanvases();
        RectTransform tipRect = (RectTransform)tooltipPanel.transform;
        Canvas canvas = tooltipPanel.GetComponentInParent<Canvas>();
        RectTransform root = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
        float maxHeight = root != null ? root.rect.height : Screen.height;
        float panelWidth = tipRect.rect.width;

        // too tall for the screen: every text at the biggest font size that fits, as the one text tooltip
        float height = PlaceSections(tooltipBaseFontSize, panelWidth);
        if (height > maxHeight)
        {
            float fits = TooltipMinFontSize, tooBig = tooltipBaseFontSize;
            for (int i = 0; i < 8; i++)
            {
                float size = (fits + tooBig) * 0.5f;
                if (PlaceSections(size, panelWidth) <= maxHeight) fits = size;
                else tooBig = size;
            }
            height = PlaceSections(fits, panelWidth);
        }
        tipRect.anchoredPosition = tooltipBasePosition;
        tipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Min(height, maxHeight));
        tooltipLaidOutScreen = new Vector2Int(Screen.width, Screen.height);
        if (root != null) KeepTooltipOnScreen(tipRect, root);
    }

    // places every shown section at this font size, top down inside the room the one text has (clear of
    // the flower frame), and returns the panel height they need
    private float PlaceSections(float fontSize, float panelWidth)
    {
        RectTransform room = tooltipText.rectTransform;
        float left = room.offsetMin.x, right = -room.offsetMax.x, top = -room.offsetMax.y, bottom = room.offsetMin.y;
        float width = panelWidth - left - right;
        float y = top;
        bool first = true;
        void Gap()
        {
            if (!first) y += SectionGap;
            first = false;
        }
        if (sectionTitle.gameObject.activeSelf)
        {
            Gap();
            y += PlaceSectionText(sectionTitle, fontSize, left, y, width);
        }
        for (int i = 0; i < sectionBoxes.Count; i++)
        {
            if (!sectionBoxes[i].gameObject.activeSelf) continue;
            Gap();
            float textHeight = PlaceSectionText(sectionTexts[i], fontSize, left + SectionTextPadX, y + SectionTextPadY, width - 2f * SectionTextPadX);
            float boxHeight = textHeight + 2f * SectionTextPadY;
            RectTransform box = sectionBoxes[i].rectTransform;
            box.anchoredPosition = new Vector2(left, -y);
            box.sizeDelta = new Vector2(width, boxHeight);
            y += boxHeight;
        }
        if (sectionFooter.gameObject.activeSelf)
        {
            Gap();
            y += PlaceSectionText(sectionFooter, fontSize, left, y, width);
        }
        return Mathf.Ceil(y + bottom);
    }

    // x and y canvas units from the panel's top left; returns the text's height
    private static float PlaceSectionText(TMP_Text text, float fontSize, float x, float y, float width)
    {
        text.fontSize = fontSize;
        float height = Mathf.Ceil(text.GetPreferredValues(text.text, width, 0f).y);
        RectTransform r = text.rectTransform;
        r.anchoredPosition = new Vector2(x, -y);
        r.sizeDelta = new Vector2(width, height);
        return height;
    }

    private void BuildSections()
    {
        if (sectionRoot != null) return;
        GameObject go = new GameObject("Sections", typeof(RectTransform));
        go.layer = tooltipPanel.layer;
        sectionRoot = (RectTransform)go.transform;
        sectionRoot.SetParent(tooltipPanel.transform, false);
        sectionRoot.anchorMin = Vector2.zero;
        sectionRoot.anchorMax = Vector2.one;
        sectionRoot.offsetMin = sectionRoot.offsetMax = Vector2.zero;
        sectionRoot.SetSiblingIndex(tooltipText.transform.GetSiblingIndex());
        sectionTitle = NewSectionText("Title");
        sectionFooter = NewSectionText("Footer");
        for (int i = 0; i < 3; i++) AddSection();
    }

    // one more box and its text (the text after its box, so it draws over it)
    private void AddSection()
    {
        InsectShowcaseStyle kit = InsectShowcaseStyle.Instance;
        int n = sectionBoxes.Count + 1;
        GameObject boxObject = new GameObject("Section " + n, typeof(RectTransform), typeof(Image));
        boxObject.layer = sectionRoot.gameObject.layer;
        RectTransform r = (RectTransform)boxObject.transform;
        r.SetParent(sectionRoot, false);
        AnchorTopLeft(r);
        Image box = boxObject.GetComponent<Image>();
        box.sprite = kit.insetBox;
        box.type = kit.insetBox != null ? Image.Type.Sliced : Image.Type.Simple;
        box.pixelsPerUnitMultiplier = 1f;
        box.raycastTarget = false;
        SectionFrameLook.Add(box);
        sectionBoxes.Add(box);
        sectionTexts.Add(NewSectionText("Section Text " + n));
    }

    // a copy of the panel's text (its font, size, colour and pixel shadow), placed by PlaceSectionText
    private TMP_Text NewSectionText(string name)
    {
        TMP_Text text = Instantiate(tooltipText, sectionRoot);
        text.name = name;
        for (int i = text.transform.childCount - 1; i >= 0; i--) Destroy(text.transform.GetChild(i).gameObject);
        text.gameObject.SetActive(true);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        AnchorTopLeft(text.rectTransform);
        return text;
    }

    private static void AnchorTopLeft(RectTransform r)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
    }

    // the one text back, the sections away
    private void HideSections()
    {
        if (sectionRoot != null) sectionRoot.gameObject.SetActive(false);
        if (tooltipText != null && !tooltipText.gameObject.activeSelf) tooltipText.gameObject.SetActive(true);
    }
}
