using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LoadoutSelectionUI : MonoBehaviour
{
    public static LoadoutSelectionUI instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private PlantRegistry plantRegistry;
    [SerializeField] private Transform unlockedContainer;
    [SerializeField] private Transform selectedContainer;
    [SerializeField] private LoadoutSlot slotPrefab;
    [SerializeField] private Button confirmButton;

    [Header("Insect Preview")]
    [Tooltip("the old insect strip's grid: its section is hidden once the insect panel (InsectShowcase) is built")]
    [SerializeField] private Transform insectContainer;
    [Tooltip("the old insect tooltip, kept hidden")]
    [SerializeField] private GameObject insectTooltipPanel;

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipName;
    [SerializeField] private Image tooltipIcon;
    [SerializeField] private TMP_Text tooltipDescription;
    [SerializeField] private TMP_Text tooltipAttackTitle;
    [SerializeField] private TMP_Text tooltipAttackDescription;
    [SerializeField] private TMP_Text tooltipPassiveTitle;
    [SerializeField] private TMP_Text tooltipPassiveDescription;
    [SerializeField] private TMP_Text tooltipSkillTitle;
    [SerializeField] private TMP_Text tooltipSkillDescription;
    [SerializeField] private TMP_Text tooltipElementalType;
    [SerializeField] private TMP_Text tooltipElementalBonus;
    [SerializeField] private TMP_Text tooltipDamageType;
    [SerializeField] private TMP_Text tooltipStats;

    private List<string> selectedLoadout = new List<string>();
    private List<LoadoutSlot> unlockedSlots = new List<LoadoutSlot>();
    // the Current Loadout grid: every plant slot there is (SaveData.MaxLoadoutSlots), picked plants
    // first, then the open slots, then the locked ones (Tommy, 2026-11-22)
    private readonly List<LoadoutSlotSquare> selectedSquares = new List<LoadoutSlotSquare>();
    // the small box over a locked square saying which level unlocks it, built on first use
    private RectTransform slotHint;
    private TMP_Text slotHintText;
    private const float SlotHintGap = 6f;
    // the insect panel on the left (Tommy, 2026-11-24), built the first time the screen opens
    private InsectShowcase showcase;

    // Show Map (Tommy, 2026-11-24): hides everything on the screen but its own button, the dark
    // backdrop included, so the whole map shows, for as long as the pointer is over it (2026-11-25: hover,
    // no click); the pointer leaving brings the screen back as it was
    private PlankButton mapButton;
    private bool mapShown;
    private Image backdrop;
    private bool plantPanelsBuilt;

    public bool IsOpen => panel.activeSelf;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
        Canvas rootCanvas = transform.root.GetComponent<Canvas>();
        if (rootCanvas != null) rootCanvas.sortingOrder = 50;
        panel.SetActive(false);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
        if (insectTooltipPanel != null) insectTooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen || SceneTransition.IsTransitioning || TutorialManager.BlocksEscape) return;
        if (!UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (SettingsManager.instance != null && SettingsManager.instance.IsOpen)
            SettingsManager.instance.Close();
        else
            SettingsManager.instance?.Open();
    }

    public void Hide()
    {
        StopIntro();
        CompleteFlights();
        SetMapShown(false);
        panel.SetActive(false);
        selectedLoadout.Clear();
        HideTooltip();
        HideSlotHint();
        showcase?.HideTips();
    }

    // Re-opens the loadout with the previously confirmed selection still in the slots.
    public void ShowWithCurrentSelection()
    {
        selectedLoadout = new List<string>(SaveManager.instance.selectedLoadout);

        List<string> unlocked = SaveManager.instance.saveData.unlockedPlants;
        if (unlocked.Count < 1)
        {
            GameManager.instance?.OnLoadoutConfirmed();
            return;
        }

        panel.SetActive(true);
        StopIntro();
        SetMapShown(false);
        BuildScreenDressing();
        PopulateInsectPanel();
        RefreshUI();
    }

    public void Show()
    {
        SaveManager.instance.selectedLoadout.Clear();
        selectedLoadout.Clear();

        List<string> unlocked = SaveManager.instance.saveData.unlockedPlants;

        // if no plants unlocked, skip straight to fertilizers
        if (unlocked.Count < 1)
        {
            GameManager.instance?.OnLoadoutConfirmed();
            return;
        }

        panel.SetActive(true);
        SetMapShown(false);
        BuildScreenDressing();
        PopulateInsectPanel();
        RefreshUI();
        PlayIntro();
    }

    // ---------- the level's opening (Tommy, 2026-11-24) ----------
    // the level fades in from the loading screen with nothing of this screen on it, only the map and
    // the game's own panel. as soon as the fade is done (no wait after it, Tommy 2026-11-26: it was half a
    // second), the insect panel, the plant panels and the buttons slide in from their own sides of the
    // screen (PlayIntro) into their places, a little one after another, and the dark backdrop fades in
    // with them. the level is paused,
    // so it all runs on unscaled time
    private const float IntroRise = 0.45f;
    private const float IntroStagger = 0.08f;
    private Coroutine intro;
    // each part, where it rests, when it starts and how far off screen it starts (it slides in from there)
    private readonly List<(RectTransform rect, Vector2 home, float delay, Vector2 offset)> introMovers =
        new List<(RectTransform, Vector2, float, Vector2)>();
    private float backdropAlpha = -1f;
    private CanvasGroup panelGroup;
    private readonly List<System.Action> afterIntro = new List<System.Action>();

    public bool IntroPlaying => intro != null;

    // runs the action once the panels are in place (at once when no intro is playing): the level 1
    // tutorial waits for them
    public void AfterIntro(System.Action action)
    {
        if (action == null) return;
        if (intro == null) action();
        else afterIntro.Add(action);
    }

    private void PlayIntro()
    {
        StopIntro();
        if (panelGroup == null) panelGroup = panel.GetComponent<CanvasGroup>();
        if (panelGroup == null) panelGroup = panel.AddComponent<CanvasGroup>();
        if (backdrop == null) backdrop = panel.GetComponent<Image>();
        if (backdrop != null && backdropAlpha < 0f) backdropAlpha = backdrop.color.a;

        // what slides in: every shown part of the screen (the tooltips stay hidden), each from its own side
        // (Tommy, 2026-11-26): the insect panel from the left, the current loadout (its section and the green
        // panel behind it) from the top, the available plants, their panel and the buttons from the bottom.
        // the insect panel first, then the plant panels, then the buttons. everything coming from one side
        // travels the same distance, enough to put the furthest in fully off screen, so parts that belong
        // together stay together
        RectTransform panelRect = (RectTransform)panel.transform;
        Canvas.ForceUpdateCanvases();
        Transform loadoutSection = SectionOf(selectedContainer), plantsSection = SectionOf(unlockedContainer);
        introMovers.Clear();
        float fromLeft = 0f, fromTop = 0f, fromBottom = 0f;
        var sides = new List<int>();   // 0 left, 1 top, 2 bottom, per mover
        Vector3[] corners = new Vector3[4];
        foreach (Transform child in panel.transform)
        {
            if (!child.gameObject.activeSelf || !(child is RectTransform rt)) continue;
            bool insects = showcase != null && child == showcase.transform;
            bool plantPart = child == loadoutSection || child == plantsSection || child.name == "PlantsPanel";
            float delay = insects ? 0f : plantPart ? IntroStagger : 2f * IntroStagger;
            // the current loadout's green panel goes with it (Tommy: it came up from the bottom)
            bool loadoutPart = child == loadoutSection || (loadoutPanel != null && child == loadoutPanel);
            int side = insects ? 0 : loadoutPart ? 1 : 2;
            rt.GetWorldCorners(corners);
            Vector3 low = panelRect.InverseTransformPoint(corners[0]), high = panelRect.InverseTransformPoint(corners[2]);
            if (side == 0) fromLeft = Mathf.Max(fromLeft, high.x - panelRect.rect.xMin + 40f);
            else if (side == 1) fromTop = Mathf.Max(fromTop, panelRect.rect.yMax - low.y + 40f);
            else fromBottom = Mathf.Max(fromBottom, high.y - panelRect.rect.yMin + 40f);
            introMovers.Add((rt, rt.anchoredPosition, delay, Vector2.zero));
            sides.Add(side);
        }
        for (int i = 0; i < introMovers.Count; i++)
        {
            var m = introMovers[i];
            Vector2 offset = sides[i] == 0 ? new Vector2(-fromLeft, 0f) : sides[i] == 1 ? new Vector2(0f, fromTop) : new Vector2(0f, -fromBottom);
            introMovers[i] = (m.rect, m.home, m.delay, offset);
            m.rect.anchoredPosition = m.home + offset;
        }
        SetBackdropAlpha(0f);
        // the backdrop still takes the pointer, so nothing under the screen can be clicked meanwhile
        panelGroup.interactable = false;
        intro = StartCoroutine(Intro());
    }

    private System.Collections.IEnumerator Intro()
    {
        while (SceneTransition.IsTransitioning) yield return null;
        float maxDelay = 0f;
        foreach (var m in introMovers) maxDelay = Mathf.Max(maxDelay, m.delay);
        // no sliding sound here (Tommy, 2026-11-26: the fertilizer cards only, for now)
        for (float t = 0f; t < IntroRise + maxDelay; t += Time.unscaledDeltaTime)
        {
            foreach (var m in introMovers)
            {
                if (m.rect == null) continue;
                float k = Mathf.Clamp01((t - m.delay) / IntroRise);
                float eased = 1f - Mathf.Pow(1f - k, 3f);   // quick at first, settling into place
                m.rect.anchoredPosition = m.home + m.offset * (1f - eased);
            }
            SetBackdropAlpha(Mathf.Clamp01(t / IntroRise));
            yield return null;
        }
        FinishIntro();
    }

    // everything in its place at once (the intro ended, or the screen closed or changed meanwhile)
    private void StopIntro()
    {
        if (intro == null) return;
        StopCoroutine(intro);
        FinishIntro();
    }

    private void FinishIntro()
    {
        foreach (var m in introMovers)
            if (m.rect != null) m.rect.anchoredPosition = m.home;
        introMovers.Clear();
        SetBackdropAlpha(1f);
        if (panelGroup != null) panelGroup.interactable = true;
        intro = null;
        var waiting = new List<System.Action>(afterIntro);
        afterIntro.Clear();
        foreach (System.Action action in waiting) action();
    }

    // share of the backdrop's own darkness
    private void SetBackdropAlpha(float share)
    {
        if (backdrop == null || backdropAlpha < 0f) return;
        Color c = backdrop.color;
        c.a = backdropAlpha * share;
        backdrop.color = c;
    }

    // once, the first time the screen opens: the green panels behind Current Loadout and Available
    // Plants, and the Show Map button
    private void BuildScreenDressing()
    {
        // every opening: the plant tooltips' numbers are read again (the skill tree can have changed)
        PlantPreview.ClearCache();
        if (!plantPanelsBuilt)
        {
            plantPanelsBuilt = true;
            // the plant cards' size and the sections' places first: the green panels are measured from them
            UseCardCells();
            LayOutPlantSections();
            BuildPlantPanels();
        }
        if (mapButton == null)
        {
            backdrop = panel.GetComponent<Image>();
            if (backdrop != null && backdropAlpha < 0f) backdropAlpha = backdrop.color.a;
            RectTransform panelRect = (RectTransform)panel.transform;
            BuildConfirmPlank();
            // hover, not click (Tommy, 2026-11-25): the pointer over Show Map fades everything else out
            // to show the whole map, the pointer leaving it brings the selection screen back
            mapButton = PlankButton.Create(panelRect, "Show Map", GameControlsStyle.Instance.speed,
                InsectShowcaseStyle.Instance.font, null);
            mapButton.Pressable = false;
            UIHoverRelay hover = mapButton.gameObject.AddComponent<UIHoverRelay>();
            hover.onEnter = () => { if (!IntroPlaying) SetMapShown(true, false); };
            hover.onExit = () => SetMapShown(false, false);
            PlaceBottomButtons();
        }
    }

    // Next and Show Map bigger and the same size (Tommy, 2026-11-25): both as wide as the wider one's word
    // needs, BottomButtonScale times the plank's size (1 art px = 3 canvas units). centred on the screen,
    // Show Map under Next (Tommy, 2026-11-26), BottomButtonGap apart, Show Map BottomMargin above the
    // screen's bottom
    private const float BottomButtonScale = 1.5f;
    private const float BottomButtonGap = 12f;
    private const float BottomMargin = 16f;
    // canvas units: the buttons' height as drawn (buttonHeight art px at 2 units a px, times the scale)
    private static float BottomButtonHeight => GameControlsStyle.Instance.buttonHeight * 2f * BottomButtonScale;
    // the buttons' middles, in the loadout panel's space
    private static float MapCentreY(RectTransform panelRect) => panelRect.rect.yMin + BottomMargin + BottomButtonHeight * 0.5f;
    private static float NextCentreY(RectTransform panelRect) => MapCentreY(panelRect) + BottomButtonHeight + BottomButtonGap;

    private void PlaceBottomButtons()
    {
        PlankButton next = confirmButton != null ? confirmButton.GetComponent<PlankButton>() : null;
        if (next == null || mapButton == null) return;
        RectTransform panelRect = (RectTransform)panel.transform;
        float width = Mathf.Max(next.NaturalWidth, mapButton.NaturalWidth);
        Vector2 centre = panelRect.rect.center;
        foreach ((RectTransform r, float y) in new[] { (next.Rect, NextCentreY(panelRect)), (mapButton.Rect, MapCentreY(panelRect)) })
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(width, r.sizeDelta.y);
            r.localScale = Vector3.one * BottomButtonScale;
            // a centre anchored child's position counts from the panel's middle
            r.anchoredPosition = new Vector2(0f, Mathf.Round(y - centre.y));
        }
    }

    // the insect panel's kit, in a bright green, behind each plant section's title and grid (Tommy):
    // panel_base and its shine around both, the grids' dark boxes swapped for the inset box
    private void BuildPlantPanels()
    {
        InsectShowcaseStyle s = InsectShowcaseStyle.Instance;
        RectTransform panelRect = (RectTransform)panel.transform;
        Transform loadoutSection = SectionOf(selectedContainer);
        Transform plantsSection = SectionOf(unlockedContainer);
        if (loadoutSection == null || plantsSection == null) return;

        // each title's drawn text clear of its grid's box: they overlapped the dark boxes (Tommy, 2026-11-24)
        PlaceTitle(loadoutSection, selectedContainer as RectTransform);
        PlaceTitle(plantsSection, BoxIn(plantsSection));

        Rect upper = BoundsIn(panelRect, loadoutSection);
        Rect lower = BoundsIn(panelRect, plantsSection);
        if (upper.yMin < lower.yMin) { Rect t = upper; upper = lower; lower = t; Transform tt = loadoutSection; loadoutSection = plantsSection; plantsSection = tt; }
        const float Px = 2f;
        float pad = s.plantPanelPad * Px;
        // room for both panels' pads between the sections: the upper section moves up, as far as the screen allows
        float need = 2f * pad + 4f * Px;
        float gap = upper.yMin - lower.yMax;
        if (gap < need)
        {
            float room = panelRect.rect.yMax - 8f * Px - (upper.yMax + pad);
            float up = Mathf.Round(Mathf.Min(need - gap, Mathf.Max(0f, room)));
            if (up > 0f)
            {
                ((RectTransform)loadoutSection).anchoredPosition += new Vector2(0f, up);
                upper = BoundsIn(panelRect, loadoutSection);
                gap = upper.yMin - lower.yMax;
            }
        }
        // the two panels never touch: when the sections are still close the pads between them shrink
        float between = Mathf.Min(pad, Mathf.Max(0f, Mathf.Floor((gap - 4f * Px) / (2f * Px))) * Px);
        float width = Mathf.Max(upper.width, lower.width) + 2f * pad;
        loadoutPanel = AddPlantPanel(panelRect, loadoutSection, upper, width, pad, between);
        AddPlantPanel(panelRect, plantsSection, lower, width, between, pad);

        foreach (Transform section in new[] { loadoutSection, plantsSection })
            foreach (Transform child in section)
            {
                // the dark box behind each grid becomes the kit's inset box
                Image box = child.GetComponent<Image>();
                if (box != null && (child == selectedContainer || child.GetComponent<ScrollRect>() != null))
                {
                    box.sprite = s.insetBox;
                    box.type = Image.Type.Sliced;
                    box.pixelsPerUnitMultiplier = 1f;
                    box.color = TallPanelLook.Available ? TallPanelLook.Wood : s.plantPanel;
                }
                // the title gets the panel's 1 pixel shadow
                TMP_Text title = child.GetComponent<TMP_Text>();
                if (title != null && title.GetComponent<PixelTextEffect>() == null)
                    title.gameObject.AddComponent<PixelTextEffect>().Color = s.plantTitleShadow;
            }
    }

    // the green panel behind the current loadout: it slides in with it, from the top (PlayIntro)
    private RectTransform loadoutPanel;

    private RectTransform AddPlantPanel(RectTransform panelRect, Transform section, Rect bounds, float width, float padTop, float padBottom)
    {
        InsectShowcaseStyle s = InsectShowcaseStyle.Instance;
        const float Px = 2f;
        float pad = s.plantPanelPad * Px;
        GameObject go = new GameObject("PlantsPanel", typeof(RectTransform), typeof(Image));
        go.layer = panel.layer;
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(panelRect, false);
        r.SetSiblingIndex(section.GetSiblingIndex());   // drawn behind its section
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        float height = bounds.height + padTop + padBottom;
        float centerY = bounds.yMin - padBottom + height * 0.5f;
        r.sizeDelta = new Vector2(Mathf.Round(width / Px) * Px, Mathf.Round(height / Px) * Px);
        r.anchoredPosition = new Vector2(Mathf.Round(bounds.center.x), Mathf.Round(centerY));
        Image body = go.GetComponent<Image>();
        body.sprite = s.panelBase;
        body.type = Image.Type.Sliced;
        body.pixelsPerUnitMultiplier = 1f;
        body.color = s.plantPanel;
        body.raycastTarget = false;

        // the in game right panel's wood and gold trim (Tommy, 2026-11-25); the kit's shine without it
        if (TallPanelLook.Available) { TallPanelLook.Apply(body); return r; }
        GameObject shineGO = new GameObject("Shine", typeof(RectTransform), typeof(Image));
        shineGO.layer = go.layer;
        RectTransform shine = (RectTransform)shineGO.transform;
        shine.SetParent(r, false);
        shine.anchorMin = Vector2.zero;
        shine.anchorMax = Vector2.one;
        shine.offsetMin = shine.offsetMax = Vector2.zero;
        Image shineImage = shineGO.GetComponent<Image>();
        shineImage.sprite = s.panelShine;
        shineImage.type = Image.Type.Sliced;
        shineImage.pixelsPerUnitMultiplier = 1f;
        shineImage.raycastTarget = false;
        return r;
    }

    // the child of the loadout panel that holds t (a plant section)
    private Transform SectionOf(Transform t)
    {
        while (t != null && t.parent != panel.transform) t = t.parent;
        return t;
    }

    // a plant section's grid box: its scroll view, or the grid itself
    private RectTransform BoxIn(Transform section)
    {
        foreach (Transform child in section)
            if (child == selectedContainer || child.GetComponent<ScrollRect>() != null) return (RectTransform)child;
        return null;
    }

    // art pixels between a section title's drawn letters and the top of its grid's box
    private const float TitleGap = 4f;

    // moves a section's title up or down until its drawn text ends TitleGap pixels above the box
    private static void PlaceTitle(Transform section, RectTransform box)
    {
        if (box == null) return;
        TMP_Text title = TitleOf(section);
        if (title == null) return;
        title.ForceMeshUpdate();
        Bounds drawn = title.textBounds;
        if (drawn.size.y <= 0f) return;
        float textBottom = section.InverseTransformPoint(title.rectTransform.TransformPoint(new Vector3(drawn.center.x, drawn.min.y, 0f))).y;
        Vector3[] corners = new Vector3[4];
        box.GetWorldCorners(corners);
        float boxTop = section.InverseTransformPoint(corners[1]).y;
        float move = Mathf.Round(boxTop + TitleGap * 2f - textBottom);
        if (move != 0f) title.rectTransform.anchoredPosition += new Vector2(0f, move);
    }

    private static TMP_Text TitleOf(Transform section)
    {
        foreach (Transform child in section)
        {
            TMP_Text title = child.GetComponent<TMP_Text>();
            if (title != null) return title;
        }
        return null;
    }

    // how tall a section title's drawn letters are
    private static float TitleHeight(Transform section)
    {
        TMP_Text title = TitleOf(section);
        if (title == null) return 0f;
        title.ForceMeshUpdate();
        return Mathf.Max(0f, title.textBounds.size.y);
    }

    // ---------- the cards' size and the two sections' places (Tommy, 2026-11-25) ----------
    // the plant cards half again the art's size, touching, each grid centred in its dark box. Current
    // Loadout: its eight cards in two rows of four. Available Plants: five a row, its box as tall as the
    // screen leaves above Next (it scrolls). both boxes centred on the screen, each title TitleGap pixels
    // over its box, the two green panels' pads between them (BuildPlantPanels draws the panels after)
    private const float CardScale = 1.5f;
    private static readonly Vector2 CardCell = new Vector2(Mathf.Round(PlantCardLook.ArtWidth * CardScale),
        Mathf.Round(PlantCardLook.ArtHeight * CardScale));
    private const int LoadoutColumns = 4;
    // canvas units: the inset box's 2 pixel border, then the room between it and the cards
    private const float BoxBorder = 4f;
    private const float CardInset = 4f;
    // canvas units between the plants panel's bottom and the top of Next
    private const float AboveNext = 12f;

    private void LayOutPlantSections()
    {
        RectTransform panelRect = (RectTransform)panel.transform;
        Transform loadoutSection = SectionOf(selectedContainer);
        Transform plantsSection = SectionOf(unlockedContainer);
        RectTransform loadoutBox = selectedContainer as RectTransform;
        RectTransform plantsBox = plantsSection != null ? BoxIn(plantsSection) : null;
        if (loadoutSection == null || plantsSection == null || loadoutBox == null || plantsBox == null) return;

        const float Px = 2f;
        float pad = InsectShowcaseStyle.Instance.plantPanelPad * Px;
        float edge = BoxBorder + CardInset;
        float centreX = panelRect.rect.center.x;

        // Current Loadout: from the screen's top, its panel's pad and its title down
        float loadoutTop = Even(panelRect.rect.yMax - 8f * Px - pad - TitleHeight(loadoutSection) - TitleGap * Px, false);
        Vector2 loadoutSize = new Vector2(LoadoutColumns * CardCell.x + 2f * edge,
            Mathf.CeilToInt((float)SaveData.MaxLoadoutSlots / LoadoutColumns) * CardCell.y + 2f * edge);
        ContentSizeFitter fitter = loadoutBox.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;
        PlaceBox(loadoutBox, loadoutSection, panelRect, centreX, loadoutTop, loadoutSize);
        GridLayoutGroup grid = loadoutBox.GetComponent<GridLayoutGroup>();
        if (grid != null) grid.padding = new RectOffset((int)edge, (int)edge, (int)edge, (int)edge);

        // Available Plants: under it, room for both panels' pads between them (what BuildPlantPanels
        // wants), down to just above Next
        float loadoutBottom = loadoutTop - loadoutSize.y;
        float plantsTop = Even(loadoutBottom - (2f * pad + 4f * Px) - TitleHeight(plantsSection) - TitleGap * Px, false);
        // the top of Next, where PlaceBottomButtons puts it (Show Map is under it)
        float nextTop = NextCentreY(panelRect) + BottomButtonHeight * 0.5f;
        float plantsBottom = Even(nextTop + AboveNext + pad, true);
        Vector2 plantsSize = new Vector2(LevelsPerSection * CardCell.x + 2f * edge, Mathf.Max(CardCell.y + 2f * edge, plantsTop - plantsBottom));
        PlaceBox(plantsBox, plantsSection, panelRect, centreX, plantsTop, plantsSize);
        // the cards scroll inside the box's border, not over it
        ScrollRect scroll = plantsBox.GetComponent<ScrollRect>();
        RectTransform viewport = scroll != null ? scroll.viewport : null;
        if (viewport != null)
        {
            viewport.offsetMin = new Vector2(BoxBorder, BoxBorder);
            viewport.offsetMax = new Vector2(-BoxBorder, -BoxBorder);
        }

        PlaceTitle(loadoutSection, loadoutBox);
        PlaceTitle(plantsSection, plantsBox);
        foreach (Transform section in new[] { loadoutSection, plantsSection })
        {
            TMP_Text title = TitleOf(section);
            if (title == null) continue;
            Vector2 at = title.rectTransform.anchoredPosition;
            title.rectTransform.anchoredPosition = new Vector2(at.x + XIn(section, panelRect, centreX) - TitleCentreX(title, section), at.y);
        }
    }

    // a box centred on x with its top at top (both in the loadout panel's space), size wide and tall
    private static void PlaceBox(RectTransform box, Transform section, RectTransform panelRect, float x, float top, Vector2 size)
    {
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
        box.pivot = new Vector2(0.5f, 1f);
        box.sizeDelta = size;
        Vector3 local = section.InverseTransformPoint(panelRect.TransformPoint(new Vector3(x, top, 0f)));
        // the section's own rect centre is where a centre anchored child's position starts
        Vector2 origin = ((RectTransform)section).rect.center;
        box.anchoredPosition = new Vector2(Mathf.Round(local.x - origin.x), Mathf.Round(local.y - origin.y));
    }

    private static float XIn(Transform section, RectTransform panelRect, float x) =>
        section.InverseTransformPoint(panelRect.TransformPoint(new Vector3(x, 0f, 0f))).x;

    private static float TitleCentreX(TMP_Text title, Transform section)
    {
        RectTransform r = title.rectTransform;
        return section.InverseTransformPoint(r.TransformPoint(r.rect.center)).x;
    }

    // a value on the 2 unit pixel grid, rounded down or up
    private static float Even(float v, bool up) => (up ? Mathf.Ceil(v / 2f) : Mathf.Floor(v / 2f)) * 2f;

    // what a section's active children cover (a text's drawn letters too), in the loadout panel's own space
    private static Rect BoundsIn(RectTransform space, Transform section)
    {
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        Vector3[] corners = new Vector3[4];
        void Add(Vector3 world)
        {
            Vector2 p = space.InverseTransformPoint(world);
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
        foreach (Transform child in section)
        {
            if (!child.gameObject.activeSelf || !(child is RectTransform rt)) continue;
            rt.GetWorldCorners(corners);
            foreach (Vector3 corner in corners) Add(corner);
            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.ForceMeshUpdate();
                Bounds drawn = text.textBounds;
                if (drawn.size.y > 0f)
                {
                    Add(rt.TransformPoint(drawn.min));
                    Add(rt.TransformPoint(drawn.max));
                }
            }
        }
        return min.x > max.x ? new Rect(0f, 0f, 0f, 0f) : Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // Show Map: everything on the loadout panel but its button hides, and the dark backdrop goes, so
    // the whole map shows. off again, exactly what was showing comes back
    // hovering the button fades the screen out and back (Tommy, 2026-11-24: a lerp, not a snap); opening,
    // closing and confirming put it back at once
    private void SetMapShown(bool shown) => SetMapShown(shown, true);

    private const float MapFadeTime = 0.25f;
    private Coroutine mapFade;
    // 1: the whole screen shows, 0: only the map (and the button)
    private float mapShare = 1f;
    private readonly List<CanvasGroup> mapFaded = new List<CanvasGroup>();

    private void SetMapShown(bool shown, bool instant)
    {
        if (mapFade != null) { StopCoroutine(mapFade); mapFade = null; }
        CompleteFlights();
        if (shown != mapShown)
        {
            mapShown = shown;
            if (shown)
            {
                HideTooltip();
                HideSlotHint();
                showcase?.HideTips();
                // every shown part but the button fades, each through its own canvas group
                mapFaded.Clear();
                foreach (Transform child in panel.transform)
                {
                    if (mapButton != null && child == mapButton.transform) continue;
                    if (!child.gameObject.activeSelf) continue;
                    CanvasGroup group = child.GetComponent<CanvasGroup>();
                    if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();
                    mapFaded.Add(group);
                }
            }
        }
        float target = shown ? 0f : 1f;
        if (instant || !isActiveAndEnabled) ApplyMapShare(target);
        else mapFade = StartCoroutine(FadeMap(target));
    }

    private System.Collections.IEnumerator FadeMap(float target)
    {
        float from = mapShare;
        for (float t = 0f; t < MapFadeTime; t += Time.unscaledDeltaTime)
        {
            float k = t / MapFadeTime;
            ApplyMapShare(Mathf.Lerp(from, target, k * k * (3f - 2f * k)));
            yield return null;
        }
        ApplyMapShare(target);
        mapFade = null;
    }

    // every faded part and the dark backdrop at this share of their look; hidden parts can't be clicked
    private void ApplyMapShare(float share)
    {
        mapShare = share;
        foreach (CanvasGroup group in mapFaded)
        {
            if (group == null) continue;
            group.alpha = share;
            group.blocksRaycasts = !mapShown;
        }
        SetBackdropAlpha(share);
        if (!mapShown && share >= 1f) mapFaded.Clear();
    }

    // the insect panel (InsectShowcase): the level's conditions and a card per insect its waves spawn.
    // no live Insect exists on this screen, so it reads the prefabs (ProceduralLevel.GetInsectPrefabs)
    private void PopulateInsectPanel()
    {
        if (showcase == null)
        {
            showcase = InsectShowcase.Create((RectTransform)panel.transform);
            // the old strip and its tooltip stay in the scene, hidden
            if (insectContainer != null && insectContainer.parent != null) insectContainer.parent.gameObject.SetActive(false);
            if (insectTooltipPanel != null) insectTooltipPanel.SetActive(false);
        }
        SpawnManager spawner = FindAnyObjectByType<SpawnManager>();
        showcase.Show(ProceduralLevel.CurrentConfig, spawner != null ? spawner.GetInsectPrefabs() : null);
    }

    // a plant this level recommends (LevelConfig.recommendedPlants, Tommy's picks from Level 6 on)
    // gets a gold Recommended badge straddling the top of its square
    private static void AddRecommendedBadge(LoadoutSlot slot, PlantData data)
    {
        LevelConfig config = ProceduralLevel.CurrentConfig;
        if (config == null || config.recommendedPlants == null || slot == null) return;
        if (System.Array.IndexOf(config.recommendedPlants, data) < 0) return;
        RectTransform badge = InsectShowcase.AddBadge((RectTransform)slot.transform, "Recommended",
            InsectShowcaseStyle.Instance.plantBadgeTextSize);
        badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 1f);
        badge.pivot = new Vector2(0.5f, 0.5f);
        badge.anchoredPosition = Vector2.zero;
    }

    public void RefreshUI()
    {
        // a glide still under way lands first: its slots are about to be rebuilt
        CompleteFlights();
        BuildAvailablePlants();

        // hidden first, so the grid lays out only the new squares this frame
        foreach (var s in selectedSquares)
            if (s != null) { s.gameObject.SetActive(false); Destroy(s.gameObject); }
        selectedSquares.Clear();
        HideSlotHint();

        UseCardCells();
        for (int i = 0; i < SaveData.MaxLoadoutSlots; i++)
            selectedSquares.Add(LoadoutSlotSquare.Create(selectedContainer, CardScale, ShowSlotHint, HideSlotHint));

        int filled = 0;
        foreach (string plantName in selectedLoadout)
        {
            PlantData data = GetPlantData(plantName);
            if (data == null || filled >= selectedSquares.Count) continue;
            LoadoutSlotSquare square = selectedSquares[filled++];
            LoadoutSlot slot = Instantiate(slotPrefab, square.transform);
            slot.Initialize(data, OnSelectedSlotClicked, ShowTooltip, HideTooltip);
            square.ShowPlant(slot, data);
            square.SetCost(SkillTreeManager.GetEffectiveSunCost(data).ToString(), CardCostColor);
            AddRecommendedBadge(slot, data);
        }

        int open = SaveManager.instance.saveData.MaxLoadoutSize;
        for (int i = filled; i < selectedSquares.Count; i++)
        {
            if (i < open) selectedSquares[i].ShowEmpty();
            else selectedSquares[i].ShowLocked(SaveData.SlotUnlockHint(i));
        }

        confirmButton.interactable = selectedLoadout.Count > 0;
    }

    // Next (the scene's ConfirmButton) is the game controls' green plank (Tommy, 2026-11-25), turned into
    // one in place (PlankButton.Adopt, so its Confirm wiring stays), as wide as it was and centred where
    // it was; grey while there's nothing to confirm (the plank follows the button's interactable). saved
    // in the scene by Tools > Garden Defense > Bake Planks in Open Scene, else made when the screen opens.
    // when the screen opens it's sized and placed with Show Map (PlaceBottomButtons)
    public void BuildConfirmPlank()
    {
        if (confirmButton == null || confirmButton.GetComponent<PlankButton>() != null) return;
        RectTransform rect = (RectTransform)confirmButton.transform;
        float width = Mathf.Round(rect.sizeDelta.x / 2f) * 2f;
        PlankButton plank = PlankButton.Adopt(confirmButton, GameControlsStyle.Instance.start, InsectShowcaseStyle.Instance.font);
        rect.sizeDelta = new Vector2(Mathf.Max(plank.Rect.sizeDelta.x, width), plank.Rect.sizeDelta.y);
    }

    // ---------- Available Plants (Tommy, 2026-11-24) ----------
    // the plants in sections of five levels, one row each, no labels: levels 1 to 5 at the bottom, later
    // ones above. the registry's slot N is the plant beating level N unlocks (slot 0 the one every save
    // starts with), so a section holds the plants that become available within its five levels. each
    // plant is Tommy's plant card (2026-11-25, PlantCardLook): the plant in its window, its sun cost in
    // its slot (the card has no room for the name; the tooltip has it). a plant not unlocked yet keeps its
    // place as a solid black silhouette that costs ?. shown: the section of the level being played and
    // every section holding an unlocked plant
    private const int LevelsPerSection = 5;
    private bool availablePrepared;

    // the container's grid becomes a column of sections, each with its own grid
    private void PrepareAvailableContainer()
    {
        if (availablePrepared) return;
        availablePrepared = true;
        GridLayoutGroup oldGrid = unlockedContainer.GetComponent<GridLayoutGroup>();
        if (oldGrid != null) DestroyImmediate(oldGrid);   // one layout group per object
        VerticalLayoutGroup column = unlockedContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        // the cards touch, across sections too (Tommy, 2026-11-25); the box's border is kept clear by its
        // viewport (LayOutPlantSections)
        int inset = (int)CardInset;
        column.padding = new RectOffset(inset, inset, inset, inset);
        column.spacing = 0f;
        column.childAlignment = TextAnchor.UpperCenter;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;
    }

    private void BuildAvailablePlants()
    {
        PrepareAvailableContainer();
        for (int i = unlockedContainer.childCount - 1; i >= 0; i--)
        {
            GameObject old = unlockedContainer.GetChild(i).gameObject;
            old.SetActive(false);
            Destroy(old);
        }
        unlockedSlots.Clear();

        List<string> unlocked = SaveManager.instance.saveData.unlockedPlants;
        PlantData[] plants = plantRegistry.plants;
        LevelConfig config = ProceduralLevel.CurrentConfig;
        int lastSection = config != null ? Mathf.Max(0, (config.levelNumber - 1) / LevelsPerSection) : 0;
        for (int i = 0; i < plants.Length; i++)
            if (plants[i] != null && unlocked.Contains(plants[i].plantName))
                lastSection = Mathf.Max(lastSection, i / LevelsPerSection);

        // the first plants at the bottom left, as the old grid had them (Tommy): the earliest section is
        // the bottom row, later sections stack above it, each left to right. no level labels (Tommy)
        for (int section = lastSection; section >= 0; section--)
        {
            int first = section * LevelsPerSection;
            if (first >= plants.Length) continue;
            RectTransform grid = AddSectionGrid(section);
            for (int i = first; i < first + LevelsPerSection && i < plants.Length; i++)
                if (plants[i] != null) AddPlantCard(grid, plants[i], unlocked.Contains(plants[i].plantName));
        }
    }

    private RectTransform AddSectionGrid(int section)
    {
        GameObject go = new GameObject("Levels " + (section * LevelsPerSection + 1) + " to " + (section * LevelsPerSection + LevelsPerSection),
            typeof(RectTransform), typeof(GridLayoutGroup));
        go.layer = panel.layer;
        go.transform.SetParent(unlockedContainer, false);
        GridLayoutGroup grid = go.GetComponent<GridLayoutGroup>();
        grid.cellSize = CardCell;
        grid.spacing = Vector2.zero;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = LevelsPerSection;
        grid.startCorner = GridLayoutGroup.Corner.LowerLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        return (RectTransform)go.transform;
    }

    // a plant not unlocked yet: its sprite as a solid black silhouette, half transparent pixels too
    // (UI/Silhouette through Resources/UISilhouette.mat). without the material it's only tinted black
    private static Material silhouetteMaterial;
    private static bool silhouetteLooked;

    private static Material SilhouetteMaterial
    {
        get
        {
            if (!silhouetteLooked)
            {
                silhouetteLooked = true;
                silhouetteMaterial = Resources.Load<Material>("UISilhouette");
            }
            return silhouetteMaterial;
        }
    }

    // one plant's card: the plant (or its silhouette) in its window, its sun cost in its slot
    private void AddPlantCard(RectTransform grid, PlantData data, bool unlocked)
    {
        GameObject card = new GameObject(data.displayName + " Card", typeof(RectTransform), typeof(CanvasGroup));
        card.layer = panel.layer;
        card.transform.SetParent(grid, false);
        RectTransform cardRect = (RectTransform)card.transform;

        LoadoutSlotSquare square = LoadoutSlotSquare.Create(cardRect, CardScale, null, null);
        PlantCardLook.Fill(square.Rect);

        if (unlocked)
        {
            LoadoutSlot slot = Instantiate(slotPrefab, square.transform);
            slot.Initialize(data, OnUnlockedSlotClicked, ShowTooltip, HideTooltip);
            square.ShowPlant(slot, data);
            AddRecommendedBadge(slot, data);
            unlockedSlots.Add(slot);
            // a plant already in the loadout is greyed out here
            if (selectedLoadout.Contains(data.plantName)) card.GetComponent<CanvasGroup>().alpha = 0.35f;
        }
        else
        {
            // its sprite, all dark, in the card's window over the trim
            GameObject silhouette = new GameObject("Silhouette", typeof(RectTransform), typeof(Image));
            silhouette.layer = card.layer;
            RectTransform r = (RectTransform)silhouette.transform;
            r.SetParent(square.transform, false);
            PlantCardLook.InWindow(r);
            Image image = silhouette.GetComponent<Image>();
            image.sprite = data.icon;
            image.preserveAspect = true;
            image.material = SilhouetteMaterial;
            image.color = Color.black;
            image.raycastTarget = false;

            // the card in its plain bright wood, no padlock: the black silhouette says it's not unlocked yet
            // (Tommy, 2026-11-25)
            square.ShowOpen();
        }

        // the sun cost (the one placing it charges, the skill tree's reduction included) in the card's slot
        square.SetCost(unlocked ? SkillTreeManager.GetEffectiveSunCost(data).ToString() : "?", CardCostColor);
    }

    // a card's cost: the sun's gold (PlantInfoPanelStyle's sun glyph colour)
    private static Color CardCostColor => PlantInfoPanelStyle.Instance.normal.sun;

    // the Current Loadout grid's cells the card's size, touching, LoadoutColumns a row, centred (the
    // scene's grid was made for the old squares)
    private void UseCardCells()
    {
        GridLayoutGroup grid = selectedContainer != null ? selectedContainer.GetComponent<GridLayoutGroup>() : null;
        if (grid == null) return;
        grid.cellSize = CardCell;
        grid.spacing = Vector2.zero;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = LoadoutColumns;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
    }


    private void OnUnlockedSlotClicked(string plantName)
    {
        if (selectedLoadout.Contains(plantName) || selectedLoadout.Count >= SaveManager.instance.saveData.MaxLoadoutSize) return;
        CompleteFlights();
        Rect? from = SlotRect(AvailableSlotOf(plantName));
        selectedLoadout.Add(plantName);
        RefreshUI();
        // it glides from its card to its new loadout slot (its card stays greyed, as before)
        LoadoutSlot to = LoadoutSlotOf(plantName);
        if (from.HasValue && to != null) Fly(plantName, from.Value, to, null);

        if (plantName == "AcornKnight")
            TutorialManager.instance?.NotifyEvent(TutorialEvents.SelectAcornKnightSlot);
    }

    private void OnSelectedSlotClicked(string plantName)
    {
        CompleteFlights();
        Dictionary<string, Rect> before = LoadoutRects();
        selectedLoadout.Remove(plantName);
        RefreshUI();
        // it glides back to its card, which stays greyed until it lands; the plants after it slide one slot left
        LoadoutSlot card = AvailableSlotOf(plantName);
        if (card != null && before.TryGetValue(plantName, out Rect from))
        {
            CanvasGroup cardGroup = card.transform.parent != null && card.transform.parent.parent != null
                ? card.transform.parent.parent.GetComponent<CanvasGroup>() : null;
            Fly(plantName, from, card, cardGroup);
        }
        foreach (KeyValuePair<string, Rect> moved in LoadoutRects())
            if (before.TryGetValue(moved.Key, out Rect old) && Vector2.Distance(old.center, moved.Value.center) > 1f)
                Fly(moved.Key, old, LoadoutSlotOf(moved.Key), null);
    }

    // ---------- plants gliding between the two sections (Tommy, 2026-11-24, like Plants vs. Zombies) ----------
    // a click doesn't snap a plant from one section to the other: a copy of its portrait glides there
    // (FlyTime, quick then settling, unscaled since the level is paused), the real one shows once it lands.
    // a plant going back to its card keeps the card greyed until then. a new click lands every glide first
    private const float FlyTime = 0.28f;
    private class Flight
    {
        public RectTransform flyer;
        public Rect from, to;
        public LoadoutSlot target;
        public CanvasGroup greyedCard;
        public float t;
    }
    private readonly List<Flight> flights = new List<Flight>();
    private Coroutine flying;

    private void Fly(string plantName, Rect from, LoadoutSlot target, CanvasGroup greyedCard)
    {
        PlantData data = GetPlantData(plantName);
        if (data == null || target == null) return;
        Rect? to = SlotRect(target);
        if (!to.HasValue) return;

        // the sliding sound, once for everything one click sends gliding (Tommy: kept for the plant clicks)
        UISounds.Slide();
        GameObject go = new GameObject("Flying " + plantName, typeof(RectTransform), typeof(Image));
        go.layer = panel.layer;
        RectTransform flyer = (RectTransform)go.transform;
        flyer.SetParent(panel.transform, false);
        flyer.SetAsLastSibling();
        flyer.anchorMin = flyer.anchorMax = flyer.pivot = new Vector2(0.5f, 0.5f);
        Image image = go.GetComponent<Image>();
        image.sprite = data.icon;
        image.preserveAspect = true;
        image.raycastTarget = false;

        // the real one waits out of sight (a card going back only stays greyed)
        if (greyedCard == null) SlotGroup(target).alpha = 0f;
        else greyedCard.alpha = 0.35f;
        Flight flight = new Flight { flyer = flyer, from = from, to = to.Value, target = target, greyedCard = greyedCard };
        PlaceFlyer(flight, 0f);
        flights.Add(flight);
        if (flying == null) flying = StartCoroutine(Flights());
    }

    private System.Collections.IEnumerator Flights()
    {
        while (flights.Count > 0)
        {
            for (int i = flights.Count - 1; i >= 0; i--)
            {
                Flight f = flights[i];
                f.t += Time.unscaledDeltaTime / FlyTime;
                if (f.t >= 1f) { Land(f); flights.RemoveAt(i); }
                else PlaceFlyer(f, 1f - Mathf.Pow(1f - f.t, 3f));
            }
            yield return null;
        }
        flying = null;
    }

    private void PlaceFlyer(Flight f, float k)
    {
        if (f.flyer == null) return;
        f.flyer.anchoredPosition = Vector2.Lerp(f.from.center, f.to.center, k);
        f.flyer.sizeDelta = Vector2.Lerp(f.from.size, f.to.size, k);
    }

    private void Land(Flight f)
    {
        if (f.target != null)
        {
            if (f.greyedCard == null) SlotGroup(f.target).alpha = 1f;
            else if (f.greyedCard != null) f.greyedCard.alpha = 1f;
        }
        if (f.flyer != null) Destroy(f.flyer.gameObject);
    }

    // every glide lands at once (a new click, the screen closing or changing)
    private void CompleteFlights()
    {
        foreach (Flight f in flights) Land(f);
        flights.Clear();
        if (flying != null) { StopCoroutine(flying); flying = null; }
    }

    private static CanvasGroup SlotGroup(LoadoutSlot slot)
    {
        CanvasGroup group = slot.GetComponent<CanvasGroup>();
        return group != null ? group : slot.gameObject.AddComponent<CanvasGroup>();
    }

    private LoadoutSlot AvailableSlotOf(string plantName)
    {
        foreach (LoadoutSlot slot in unlockedSlots)
            if (slot != null && slot.PlantName == plantName) return slot;
        return null;
    }

    private LoadoutSlot LoadoutSlotOf(string plantName)
    {
        foreach (LoadoutSlot slot in selectedContainer.GetComponentsInChildren<LoadoutSlot>())
            if (slot.PlantName == plantName) return slot;
        return null;
    }

    // where each loadout plant sits now, in the loadout panel's own space
    private Dictionary<string, Rect> LoadoutRects()
    {
        var rects = new Dictionary<string, Rect>();
        foreach (LoadoutSlot slot in selectedContainer.GetComponentsInChildren<LoadoutSlot>())
        {
            Rect? r = SlotRect(slot);
            if (r.HasValue) rects[slot.PlantName] = r.Value;
        }
        return rects;
    }

    // a slot's rect in the loadout panel's own space, after the layout has placed it
    private Rect? SlotRect(LoadoutSlot slot)
    {
        if (slot == null) return null;
        Canvas.ForceUpdateCanvases();
        RectTransform space = (RectTransform)panel.transform;
        Vector3[] corners = new Vector3[4];
        ((RectTransform)slot.transform).GetWorldCorners(corners);
        Vector2 min = space.InverseTransformPoint(corners[0]);
        Vector2 max = space.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    public void Confirm()
    {
        TutorialManager.instance?.NotifyEvent(TutorialEvents.ClickConfirmButton);
        StopIntro();
        CompleteFlights();
        SetMapShown(false);
        SaveManager.instance.selectedLoadout = new List<string>(selectedLoadout);
        panel.SetActive(false);
        HideTooltip();
        HideSlotHint();
        showcase?.HideTips();
        GameManager.instance?.OnLoadoutConfirmed();
    }

    private void ShowTooltip(PlantData data)
    {
        if (data.plantName == "AcornKnight")
            TutorialManager.instance?.NotifyEvent(TutorialEvents.HoverAcornKnightSlot);

        if (tooltipPanel == null) return;
        tooltipPanel.SetActive(true);
        SkinTooltip(data);
        Plant prefab = data.plantPrefab;
        // the texts the plant shows in the level, with the numbers of a just placed plant: every path
        // at level 0 (Tommy, 2026-11-24; the short PlantData versions are gone)
        PlantPreview.Texts texts = PlantPreview.For(data);
        if (tooltipName != null)
            tooltipName.text = !string.IsNullOrEmpty(texts.name) ? texts.name : data.displayName;
        if (tooltipIcon != null)
            tooltipIcon.sprite = data.icon;
        if (tooltipDescription != null)
            tooltipDescription.text = texts.description ?? "";
        if (tooltipAttackTitle != null)
            tooltipAttackTitle.text = "Attack";
        if (tooltipAttackDescription != null)
            tooltipAttackDescription.text = texts.attack ?? "";
        if (tooltipPassiveTitle != null)
            tooltipPassiveTitle.text = "Passive";
        if (tooltipPassiveDescription != null)
            tooltipPassiveDescription.text = texts.passive ?? "";
        if (tooltipSkillTitle != null)
            tooltipSkillTitle.text = "Skill";
        if (tooltipSkillDescription != null)
            tooltipSkillDescription.text = texts.skill ?? "";
        if (tooltipElementalType != null)
            tooltipElementalType.text = ColoredElemental(data.elementalType);
        if (tooltipElementalBonus != null)
            tooltipElementalBonus.text = prefab != null ? prefab.GetElementDescription() : "";
        if (tooltipDamageType != null)
            tooltipDamageType.text = ColoredDamage(data.damageType);
        if (tooltipStats != null && data.plantPrefab != null)
        {
            PlantBaseStats s = data.plantPrefab.GetBaseStats();
            string stats = $"ATK DMG: <color=green><b>{s.attackDamage}</b></color>     ATK SPD: <color=green><b>{s.attackSpeed}</b></color>     RANGE: <color=green><b>{s.attackRange}</b></color>\n" +
                           $"SKILL CD: <color=green><b>{s.skillCooldown}s</b></color>";
            if (s.passiveCooldown > 0)
                stats += $"     PASSIVE CD: <color=green><b>{s.passiveCooldown}s</b></color>";
            if (s.piercing > 0)
                stats += $"     PIERCE: <color=green><b>{s.piercing}</b></color>";
            tooltipStats.text = stats;
        }
        FitTooltip();
    }

    // the plant tooltip wears the same kit panel as the insect tooltip (panel_base and its shine, Tommy,
    // 2026-11-24), tinted with the plant's element colour like the level's plant info panel
    private Image tooltipBody;

    private void SkinTooltip(PlantData data)
    {
        InsectShowcaseStyle s = InsectShowcaseStyle.Instance;
        if (tooltipBody == null)
        {
            tooltipBody = tooltipPanel.GetComponent<Image>();
            if (tooltipBody == null) return;
            tooltipBody.sprite = s.panelBase;
            tooltipBody.type = Image.Type.Sliced;
            tooltipBody.pixelsPerUnitMultiplier = 1f;

            // the shine over the whole panel, kept out of its layout group (switched off again when it
            // wears the in game right panel's look, below)
            GameObject shine = new GameObject("Shine", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            shine.layer = tooltipPanel.layer;
            RectTransform r = (RectTransform)shine.transform;
            r.SetParent(tooltipPanel.transform, false);
            r.SetAsFirstSibling();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            shine.GetComponent<LayoutElement>().ignoreLayout = true;
            Image shineImage = shine.GetComponent<Image>();
            shineImage.sprite = s.panelShine;
            shineImage.type = Image.Type.Sliced;
            shineImage.pixelsPerUnitMultiplier = 1f;
            shineImage.raycastTarget = false;

            LayoutLikeInsectTooltip(s);
            // the in game right panel's wood and gold trim (Tommy, 2026-11-25), its contents clear of the frame
            if (TallPanelLook.Available)
            {
                TallPanelLook.Apply(tooltipBody);
                VerticalLayoutGroup column = tooltipPanel.GetComponent<VerticalLayoutGroup>();
                int edge = (TallPanelLook.Inset + 2) * 2;
                if (column != null) column.padding = new RectOffset(edge, edge, edge, edge);
            }
            BuildNamePlate();
        }
        // the panel and its section boxes in the plant's element colour, like the level's plant info panel
        // (Tommy, 2026-11-26)
        Color panelColor = PlantData.ElementPanelColor(data.elementalType);
        tooltipBody.color = panelColor;
        if (namePlate != null) namePlate.color = panelColor;
        if (portraitBackdrop != null) portraitBackdrop.color = PlantData.ElementBackdropColor(data.elementalType);
        if (portraitBand != null) portraitBand.color = PlantData.ElementColor(data.elementalType);
        foreach (Image box in tooltipBoxes)
            if (box != null)
            {
                box.color = panelColor;
                SectionFrameLook.Recolor(box);
            }
    }

    // the plant's name on a plate (Tommy, 2026-11-26): the in game upgrade buttons' small button
    // (PlantInfoPanelStyle.upgradeButton, its wood tinted with the plant's element colour, its gold and
    // leaf trim over it), 2 units a pixel, as wide as the name needs. it stretches only at the art's plain
    // columns (sprite borders 21 and 33), so both leafy ends keep their pixels. the name moves into it
    private Image namePlate;

    // Tommy's portrait frame round the tooltip's portrait (2026-11-26, PortraitFrameLook), as on the level's
    // plant info panel: in the icon row in the plant's place, 80 by 80 art px at 2 units a pixel (160 square);
    // the backdrop in its 128 square opening (tinted with the element's backdrop colour), the plant on it at
    // exactly its own pixel size, the band over them (tinted with the element's colour) with its gold brackets
    private Image portraitBackdrop, portraitBand;
    // the row the portrait sits in (the icon's parent until the frame took it in)
    private Transform tooltipIconRow;

    private void UsePortraitFrame()
    {
        if (tooltipIcon == null || !PortraitFrameLook.Available || portraitBand != null) return;
        Transform row = tooltipIcon.transform.parent;
        if (row == null) return;
        tooltipIconRow = row;
        GameObject holder = new GameObject("PortraitFrame", typeof(RectTransform), typeof(LayoutElement));
        holder.layer = tooltipIcon.gameObject.layer;
        RectTransform h = (RectTransform)holder.transform;
        h.SetParent(row, false);
        h.SetSiblingIndex(tooltipIcon.transform.GetSiblingIndex());
        LayoutElement size = holder.GetComponent<LayoutElement>();
        size.minWidth = size.preferredWidth = PortraitFrameLook.Size;
        size.minHeight = size.preferredHeight = PortraitFrameLook.Size;
        size.flexibleWidth = size.flexibleHeight = 0f;

        Rect opening = PortraitFrameLook.Opening;
        portraitBackdrop = NewPortraitLayer("Backdrop", h);
        PortraitFrameLook.TopLeft(portraitBackdrop.rectTransform, opening.x, opening.y, opening.width, opening.height);
        PortraitFrameLook.MakeBackdrop(portraitBackdrop);
        tooltipIcon.transform.SetParent(portraitBackdrop.transform, false);
        RectTransform icon = tooltipIcon.rectTransform;
        icon.anchorMin = Vector2.zero;
        icon.anchorMax = Vector2.one;
        icon.offsetMin = icon.offsetMax = Vector2.zero;
        tooltipIcon.preserveAspect = true;
        LayoutElement iconSize = tooltipIcon.GetComponent<LayoutElement>();
        if (iconSize != null) iconSize.ignoreLayout = true;
        portraitBand = NewPortraitLayer("Frame", h);
        PortraitFrameLook.MakeFrame(portraitBand);
    }

    private static Image NewPortraitLayer(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }
    // the plate drawn at 3 canvas units an art pixel (Tommy, 2026-11-26: bigger, the name overlapped the 2
    // unit one), so its wood (art rows 3 to 15) is 39 units tall around the name's 21 unit capitals.
    // canvas units: its height (the art's 19 px) and the room between the name and the leafy ends (13 px)
    private const float NamePlateScale = 3f;
    private const float NamePlateHeight = 19f * NamePlateScale;
    private const int NamePlateSidePad = (int)(13f * NamePlateScale);
    // the portrait beside the name: a fixed square, 2 units a pixel of the 64 px plant art
    private const float PortraitSize = 128f;

    private void BuildNamePlate()
    {
        PlantInfoPanelStyle look = PlantInfoPanelStyle.Instance;
        if (namePlate != null || tooltipName == null || look.upgradeButton == null) return;
        Transform column = tooltipName.transform.parent;
        if (column == null) return;

        GameObject go = new GameObject("NamePlate", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        go.layer = tooltipName.gameObject.layer;
        RectTransform plate = (RectTransform)go.transform;
        plate.SetParent(column, false);
        plate.SetSiblingIndex(tooltipName.transform.GetSiblingIndex());
        namePlate = go.GetComponent<Image>();
        namePlate.sprite = look.upgradeButton;
        namePlate.type = Image.Type.Sliced;
        // the sprites are 2 units a pixel at 1; less than 1 draws their slices bigger
        namePlate.pixelsPerUnitMultiplier = 2f / NamePlateScale;
        namePlate.raycastTarget = false;
        LayoutElement size = go.GetComponent<LayoutElement>();
        size.minHeight = size.preferredHeight = NamePlateHeight;

        if (look.upgradeButtonTrim != null)
        {
            GameObject trimGO = new GameObject("Trim", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            trimGO.layer = go.layer;
            RectTransform tr = (RectTransform)trimGO.transform;
            tr.SetParent(plate, false);
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            trimGO.GetComponent<LayoutElement>().ignoreLayout = true;
            Image trim = trimGO.GetComponent<Image>();
            trim.sprite = look.upgradeButtonTrim;
            trim.type = Image.Type.Sliced;
            trim.pixelsPerUnitMultiplier = 2f / NamePlateScale;
            trim.raycastTarget = false;
        }

        // the portrait a fixed square, its sprite never stretched, in the middle of the name column's height
        if (tooltipIcon != null)
        {
            tooltipIcon.preserveAspect = true;
            LayoutElement icon = tooltipIcon.GetComponent<LayoutElement>();
            if (icon == null) icon = tooltipIcon.gameObject.AddComponent<LayoutElement>();
            icon.minWidth = icon.preferredWidth = PortraitSize;
            icon.minHeight = icon.preferredHeight = PortraitSize;
            icon.flexibleWidth = icon.flexibleHeight = 0f;
            HorizontalLayoutGroup iconRow = tooltipIcon.transform.parent != null ? tooltipIcon.transform.parent.GetComponent<HorizontalLayoutGroup>() : null;
            if (iconRow != null)
            {
                iconRow.childForceExpandHeight = false;
                iconRow.childAlignment = TextAnchor.MiddleLeft;
            }
            UsePortraitFrame();
        }

        // the name inside it, in the middle of the wood, the plate as wide as the name and its ends need
        tooltipName.transform.SetParent(plate, false);
        tooltipName.alignment = TextAlignmentOptions.Center;
        tooltipName.textWrappingMode = TextWrappingModes.NoWrap;
        HorizontalLayoutGroup row = go.GetComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(NamePlateSidePad, NamePlateSidePad, 0, 0);
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        // the name column gives each child its own width, so the plate hugs the name
        VerticalLayoutGroup names = column.GetComponent<VerticalLayoutGroup>();
        if (names != null)
        {
            names.childControlWidth = true;
            names.childForceExpandWidth = false;
        }
    }

    // the dark boxes of the tooltip's sections, tinted with the plant's element colour on every hover
    private readonly List<Image> tooltipBoxes = new List<Image>();

    // the plant tooltip laid out like the insect tooltip (Tommy, 2026-11-24): the whole screen's height
    // on the right; the name, a seam, the plant's description; then Attack, Passive, Skill and the
    // element with its bonus, each in its own dark box with its title in the kit's label colour
    private void LayoutLikeInsectTooltip(InsectShowcaseStyle s)
    {
        RectTransform tip = (RectTransform)tooltipPanel.transform;
        float width = tip.rect.width;
        float margin = s.margin * 2f;
        tip.anchorMin = new Vector2(1f, 0f);
        tip.anchorMax = new Vector2(1f, 1f);
        tip.pivot = new Vector2(1f, 0.5f);
        tip.anchoredPosition = new Vector2(-margin, 0f);
        tip.sizeDelta = new Vector2(width, -2f * margin);
        ContentSizeFitter fitter = tooltipPanel.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;
        VerticalLayoutGroup column = tooltipPanel.GetComponent<VerticalLayoutGroup>();
        if (column != null)
        {
            column.childAlignment = TextAnchor.UpperCenter;
            column.childForceExpandHeight = false;
            column.childControlHeight = true;
            column.spacing = 12f;
        }

        // the seam under the name, like the insect tooltip's
        Transform header = ChildOf(tooltipPanel.transform, tooltipName != null ? tooltipName.transform : null);
        Transform nameRow = header != null ? ChildOf(header, tooltipName.transform) : null;
        if (header != null && nameRow != null)
        {
            GameObject seam = new GameObject("Seam", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            seam.layer = tooltipPanel.layer;
            seam.transform.SetParent(header, false);
            seam.transform.SetSiblingIndex(nameRow.GetSiblingIndex() + 1);
            Image seamImage = seam.GetComponent<Image>();
            seamImage.sprite = s.seam;
            seamImage.raycastTarget = false;
            LayoutElement seamSize = seam.GetComponent<LayoutElement>();
            seamSize.minHeight = seamSize.preferredHeight = 4f;
            seamSize.flexibleWidth = 1f;
        }

        // Attack, Passive, Skill, then the element: each section a dark inset box in Tommy's section frame
        // (SectionFrameLook, 2026-11-26), its text clear of the frame
        TMP_Text[] titles = { tooltipAttackTitle, tooltipPassiveTitle, tooltipSkillTitle, tooltipElementalType };
        foreach (TMP_Text title in titles)
        {
            Transform section = title != null ? ChildOf(tooltipPanel.transform, title.transform) : null;
            if (section == null || section == header) continue;
            Image box = section.GetComponent<Image>();
            if (box == null) box = section.gameObject.AddComponent<Image>();
            box.sprite = s.insetBox;
            box.type = Image.Type.Sliced;
            box.pixelsPerUnitMultiplier = 1f;
            box.raycastTarget = false;
            tooltipBoxes.Add(box);
            VerticalLayoutGroup inner = section.GetComponent<VerticalLayoutGroup>();
            bool framed = SectionFrameLook.Add(box) != null;
            if (inner != null) inner.padding = framed ? new RectOffset(20, 20, 16, 18) : new RectOffset(14, 14, 10, 12);
            if (title != tooltipElementalType) title.color = s.label;
            section.SetAsLastSibling();
        }
    }

    // the child of parent that holds t
    private static Transform ChildOf(Transform parent, Transform t)
    {
        while (t != null && t.parent != parent) t = t.parent;
        return t;
    }

    // the plant tooltip is the screen's height; its sections sit at the top. when they're too long for
    // it, every text under the line gets the same smaller share of its own font size, the biggest that
    // fits (Tommy, 2026-11-24, as the level's tooltip does: PlantUpgradeUI.LayoutTooltip); the portrait,
    // name and damage type above the line never shrink (Tommy, 2026-11-26)
    private TMP_Text[] tooltipTexts;
    private float[] tooltipBaseSizes;
    private const float TooltipMinScale = 0.35f;

    private void FitTooltip()
    {
        if (tooltipPanel == null) return;
        RectTransform tip = (RectTransform)tooltipPanel.transform;
        if (tooltipTexts == null)
        {
            // everything above the line (the portrait's row: the name and the damage type) keeps its size
            // (Tommy, 2026-11-26); only the texts under it shrink to fit
            Transform fixedRow = tooltipIconRow != null ? tooltipIconRow : tooltipIcon != null ? tooltipIcon.transform.parent : null;
            var scaled = new List<TMP_Text>();
            foreach (TMP_Text text in tooltipPanel.GetComponentsInChildren<TMP_Text>(true))
                if (fixedRow == null || !text.transform.IsChildOf(fixedRow)) scaled.Add(text);
            tooltipTexts = scaled.ToArray();
            tooltipBaseSizes = new float[tooltipTexts.Length];
            for (int i = 0; i < tooltipTexts.Length; i++) tooltipBaseSizes[i] = tooltipTexts[i].fontSize;
        }
        Canvas.ForceUpdateCanvases();
        float maxHeight = tip.rect.height;

        if (TooltipHeightAt(tip, 1f) <= maxHeight) return;
        // halving the range between the smallest share and the full size
        float fits = TooltipMinScale, tooBig = 1f;
        for (int i = 0; i < 8; i++)
        {
            float scale = (fits + tooBig) * 0.5f;
            if (TooltipHeightAt(tip, scale) <= maxHeight) fits = scale;
            else tooBig = scale;
        }
        TooltipHeightAt(tip, fits);
    }

    // sets every tooltip text to scale times its own size, lays the panel out and returns the height its
    // sections need
    private float TooltipHeightAt(RectTransform tip, float scale)
    {
        for (int i = 0; i < tooltipTexts.Length; i++)
            if (tooltipTexts[i] != null) tooltipTexts[i].fontSize = tooltipBaseSizes[i] * scale;
        LayoutRebuilder.ForceRebuildLayoutImmediate(tip);
        return LayoutUtility.GetPreferredHeight(tip);
    }

    private static string ColoredElemental(ElementalType type)
    {
        return type switch
        {
            ElementalType.Fire    => "<color=orange>Fire</color>",
            ElementalType.Water   => "<color=#4FC3F7>Water</color>",
            ElementalType.Ice     => "<color=#00FFFF>Ice</color>",
            ElementalType.Wind    => "<color=#B2EBF2>Wind</color>",
            ElementalType.Grass  => "<color=green>Grass</color>",
            ElementalType.Poison  => "<color=purple>Poison</color>",
            _                     => "<color=white>Neutral</color>"
        };
    }

    private static string ColoredDamage(DamageType type)
    {
        return type switch
        {
            DamageType.Physical => "<color=#A0522D>Physical</color>",
            DamageType.Magic    => "<color=#FFB6C1>Magic</color>",
            _                   => type.ToString()
        };
    }

    public void ResetUnlockedScroll()
    {
        ((RectTransform)unlockedContainer).anchoredPosition = Vector2.zero;
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    // the unlock hint just above a hovered locked square
    private void ShowSlotHint(LoadoutSlotSquare square, string hint)
    {
        if (slotHint == null) BuildSlotHint();
        if (slotHint == null || square == null) return;
        slotHintText.text = hint;
        slotHint.gameObject.SetActive(true);
        slotHint.SetAsLastSibling();
        RectTransform rect = square.Rect;
        float top = rect.rect.yMax + SlotHintGap;
        slotHint.position = rect.TransformPoint(new Vector3(rect.rect.center.x, top, 0f));
        LayoutRebuilder.ForceRebuildLayoutImmediate(slotHint);
    }

    private void HideSlotHint()
    {
        if (slotHint != null) slotHint.gameObject.SetActive(false);
    }

    // a dark box sized to its text, in the font the screen's own labels use. neither part takes the
    // pointer, so the square under it keeps its hover
    private void BuildSlotHint()
    {
        if (panel == null) return;
        GameObject box = new GameObject("SlotUnlockHint", typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        box.layer = panel.layer;
        slotHint = (RectTransform)box.transform;
        slotHint.SetParent(panel.transform, false);
        slotHint.anchorMin = slotHint.anchorMax = new Vector2(0.5f, 0.5f);
        slotHint.pivot = new Vector2(0.5f, 0f);
        Image background = box.GetComponent<Image>();
        background.color = new Color(0.08f, 0.06f, 0.05f, 0.92f);
        background.raycastTarget = false;
        VerticalLayoutGroup layout = box.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        // Tommy's flower frame (2026-11-26), the text clear of it
        if (TooltipFrameLook.Available)
        {
            TooltipFrameLook.Apply(background);
            int inset = (int)TooltipFrameLook.Inset;
            layout.padding = new RectOffset(inset, inset, inset, inset);
        }
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = box.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.layer = panel.layer;
        label.transform.SetParent(slotHint, false);
        slotHintText = label.GetComponent<TextMeshProUGUI>();
        TMP_Text fontSource = panel.GetComponentInChildren<TMP_Text>(true);
        if (fontSource != null && fontSource.font != null) slotHintText.font = fontSource.font;
        slotHintText.fontSize = 22f;
        slotHintText.color = Color.white;
        slotHintText.alignment = TextAlignmentOptions.Center;
        slotHintText.textWrappingMode = TextWrappingModes.NoWrap;
        slotHintText.raycastTarget = false;
        box.SetActive(false);
    }

    private PlantData GetPlantData(string plantName)
    {
        foreach (var data in plantRegistry.plants)
        {
            if (data == null) continue;
            if (data.plantName == plantName) return data;
        }

        return null;
    }
}
