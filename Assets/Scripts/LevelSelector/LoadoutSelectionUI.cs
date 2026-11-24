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
    // backdrop included, so the whole map shows; pressed again, the screen comes back as it was
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
    // the game's own panel. once the fade is done and IntroDelay has passed, the insect panel, the plant
    // panels and the buttons rise from below the screen into their places, a little one after another,
    // and the dark backdrop fades in with them. the level is paused, so it all runs on unscaled time
    private const float IntroDelay = 0.5f;
    private const float IntroRise = 0.45f;
    private const float IntroStagger = 0.08f;
    private Coroutine intro;
    private readonly List<(RectTransform rect, Vector2 home, float delay)> introMovers = new List<(RectTransform, Vector2, float)>();
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

        // what rises: every shown part of the screen (the tooltips stay hidden), the insect panel
        // first, then the plant panels, then the buttons. all rise the same distance, enough to put
        // the highest one fully below the screen, so parts that belong together stay together
        RectTransform panelRect = (RectTransform)panel.transform;
        Canvas.ForceUpdateCanvases();
        Transform loadoutSection = SectionOf(selectedContainer), plantsSection = SectionOf(unlockedContainer);
        introMovers.Clear();
        float distance = 0f;
        Vector3[] corners = new Vector3[4];
        foreach (Transform child in panel.transform)
        {
            if (!child.gameObject.activeSelf || !(child is RectTransform rt)) continue;
            float delay = showcase != null && child == showcase.transform ? 0f
                : child == loadoutSection || child == plantsSection || child.name == "PlantsPanel" ? IntroStagger
                : 2f * IntroStagger;
            introMovers.Add((rt, rt.anchoredPosition, delay));
            rt.GetWorldCorners(corners);
            float top = panelRect.InverseTransformPoint(corners[1]).y;
            distance = Mathf.Max(distance, top - panelRect.rect.yMin + 40f);
        }
        foreach (var m in introMovers) m.rect.anchoredPosition = m.home - new Vector2(0f, distance);
        SetBackdropAlpha(0f);
        // the backdrop still takes the pointer, so nothing under the screen can be clicked meanwhile
        panelGroup.interactable = false;
        intro = StartCoroutine(Intro(distance));
    }

    private System.Collections.IEnumerator Intro(float distance)
    {
        while (SceneTransition.IsTransitioning) yield return null;
        yield return new WaitForSecondsRealtime(IntroDelay);
        float maxDelay = 0f;
        foreach (var m in introMovers) maxDelay = Mathf.Max(maxDelay, m.delay);
        for (float t = 0f; t < IntroRise + maxDelay; t += Time.unscaledDeltaTime)
        {
            foreach (var m in introMovers)
            {
                if (m.rect == null) continue;
                float k = Mathf.Clamp01((t - m.delay) / IntroRise);
                float eased = 1f - Mathf.Pow(1f - k, 3f);   // quick at first, settling into place
                m.rect.anchoredPosition = m.home - new Vector2(0f, distance * (1f - eased));
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
            BuildPlantPanels();
        }
        if (mapButton == null)
        {
            backdrop = panel.GetComponent<Image>();
            if (backdrop != null && backdropAlpha < 0f) backdropAlpha = backdrop.color.a;
            RectTransform panelRect = (RectTransform)panel.transform;
            mapButton = PlankButton.Create(panelRect, "Show Map", GameControlsStyle.Instance.speed,
                InsectShowcaseStyle.Instance.font, () => SetMapShown(!mapShown, false));
            // just right of Confirm, level with it
            RectTransform r = mapButton.Rect;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            Vector3[] corners = new Vector3[4];
            ((RectTransform)confirmButton.transform).GetWorldCorners(corners);
            Vector2 rightMiddle = panelRect.InverseTransformPoint((corners[2] + corners[3]) * 0.5f);
            r.anchoredPosition = new Vector2(Mathf.Round(rightMiddle.x + 24f), Mathf.Round(rightMiddle.y));
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
        LiftTitle(loadoutSection, selectedContainer as RectTransform);
        LiftTitle(plantsSection, BoxIn(plantsSection));

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
        AddPlantPanel(panelRect, loadoutSection, upper, width, pad, between);
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
                    box.color = s.plantPanel;
                }
                // the title gets the panel's 1 pixel shadow
                TMP_Text title = child.GetComponent<TMP_Text>();
                if (title != null && title.GetComponent<PixelTextEffect>() == null)
                    title.gameObject.AddComponent<PixelTextEffect>().Color = s.plantTitleShadow;
            }
    }

    private void AddPlantPanel(RectTransform panelRect, Transform section, Rect bounds, float width, float padTop, float padBottom)
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

    // moves a section's title up until its drawn text ends TitleGap pixels above the box
    private static void LiftTitle(Transform section, RectTransform box)
    {
        if (box == null) return;
        TMP_Text title = null;
        foreach (Transform child in section)
            if ((title = child.GetComponent<TMP_Text>()) != null) break;
        if (title == null) return;
        title.ForceMeshUpdate();
        Bounds drawn = title.textBounds;
        if (drawn.size.y <= 0f) return;
        float textBottom = section.InverseTransformPoint(title.rectTransform.TransformPoint(new Vector3(drawn.center.x, drawn.min.y, 0f))).y;
        Vector3[] corners = new Vector3[4];
        box.GetWorldCorners(corners);
        float boxTop = section.InverseTransformPoint(corners[1]).y;
        float lift = boxTop + TitleGap * 2f - textBottom;
        if (lift > 0f) title.rectTransform.anchoredPosition += new Vector2(0f, Mathf.Ceil(lift));
    }

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
    // the button fades the screen out and back (Tommy, 2026-11-24: a lerp, not a snap); opening,
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
            if (mapButton != null) mapButton.SetLabel(shown ? "Hide Map" : "Show Map");
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
        BuildAvailablePlants();

        // hidden first, so the grid lays out only the new squares this frame
        foreach (var s in selectedSquares)
            if (s != null) { s.gameObject.SetActive(false); Destroy(s.gameObject); }
        selectedSquares.Clear();
        HideSlotHint();

        for (int i = 0; i < SaveData.MaxLoadoutSlots; i++)
            selectedSquares.Add(LoadoutSlotSquare.Create(selectedContainer, ShowSlotHint, HideSlotHint));

        int filled = 0;
        foreach (string plantName in selectedLoadout)
        {
            PlantData data = GetPlantData(plantName);
            if (data == null || filled >= selectedSquares.Count) continue;
            LoadoutSlotSquare square = selectedSquares[filled++];
            LoadoutSlot slot = Instantiate(slotPrefab, square.transform);
            slot.Initialize(data, OnSelectedSlotClicked, ShowTooltip, HideTooltip);
            square.ShowPlant(slot, data);
            AddRecommendedBadge(slot, data);
        }

        int open = SaveManager.instance.saveData.MaxLoadoutSize;
        for (int i = filled; i < selectedSquares.Count; i++)
        {
            if (i < open) selectedSquares[i].ShowOpen();
            else selectedSquares[i].ShowLocked(SaveData.SlotUnlockHint(i));
        }

        confirmButton.interactable = selectedLoadout.Count > 0;
    }

    // ---------- Available Plants (Tommy, 2026-11-24) ----------
    // the plants in sections of five levels (Levels 1-5, Levels 6-10, ...). the registry's slot N is the
    // plant beating level N unlocks (slot 0 the one every save starts with), so a section holds the plants
    // that become available within its five levels. each plant is a card: its square (the plant bar's
    // bevel frame on its backdrop, tinted by element), its name and its sun cost under it. a plant not
    // unlocked yet keeps its place as a dark silhouette named ??? that costs ?. shown: the section of the
    // level being played and every section holding an unlocked plant
    private const int LevelsPerSection = 5;
    private const float CardSquare = 100f;
    private const float CardNameHeight = 20f;
    private const float CardCostHeight = 18f;
    private const float CardHeight = CardSquare + 3f + CardNameHeight + 1f + CardCostHeight;
    private static readonly Color SilhouetteColor = new Color(0.06f, 0.06f, 0.07f, 1f);
    private bool availablePrepared;

    // the container's grid becomes a column of sections, each with its own grid
    private void PrepareAvailableContainer()
    {
        if (availablePrepared) return;
        availablePrepared = true;
        RectOffset padding = new RectOffset(20, 20, 20, 30);
        GridLayoutGroup oldGrid = unlockedContainer.GetComponent<GridLayoutGroup>();
        if (oldGrid != null)
        {
            padding = new RectOffset(oldGrid.padding.left, oldGrid.padding.right, oldGrid.padding.top, oldGrid.padding.bottom);
            DestroyImmediate(oldGrid);   // one layout group per object
        }
        VerticalLayoutGroup column = unlockedContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        column.padding = padding;
        column.spacing = 8f;
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

        for (int section = 0; section <= lastSection; section++)
        {
            int first = section * LevelsPerSection;
            if (first >= plants.Length) break;
            AddSectionHeader($"Levels {first + 1}-{first + LevelsPerSection}");
            RectTransform grid = AddSectionGrid();
            for (int i = first; i < first + LevelsPerSection && i < plants.Length; i++)
                if (plants[i] != null) AddPlantCard(grid, plants[i], unlocked.Contains(plants[i].plantName));
        }
    }

    // a section's title, in a box of its own so the text's shadow copies stay out of the column's layout
    private void AddSectionHeader(string title)
    {
        InsectShowcaseStyle s = InsectShowcaseStyle.Instance;
        GameObject box = new GameObject("Section " + title, typeof(RectTransform), typeof(LayoutElement));
        box.layer = panel.layer;
        box.transform.SetParent(unlockedContainer, false);
        box.GetComponent<LayoutElement>().preferredHeight = 24f;
        TextMeshProUGUI text = NewText(box.transform, title, 20f, TextAlignmentOptions.Left);
        RectTransform r = text.rectTransform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        text.gameObject.AddComponent<PixelTextEffect>().Color = s.plantTitleShadow;
    }

    private RectTransform AddSectionGrid()
    {
        GameObject go = new GameObject("Plants", typeof(RectTransform), typeof(GridLayoutGroup));
        go.layer = panel.layer;
        go.transform.SetParent(unlockedContainer, false);
        GridLayoutGroup grid = go.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(CardSquare, CardHeight);
        grid.spacing = new Vector2(7f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = LevelsPerSection;
        grid.childAlignment = TextAnchor.UpperCenter;
        return (RectTransform)go.transform;
    }

    // one plant's card: its square with the plant (or its silhouette), its name, its sun cost
    private void AddPlantCard(RectTransform grid, PlantData data, bool unlocked)
    {
        GameObject card = new GameObject(data.displayName + " Card", typeof(RectTransform), typeof(CanvasGroup));
        card.layer = panel.layer;
        card.transform.SetParent(grid, false);
        RectTransform cardRect = (RectTransform)card.transform;

        LoadoutSlotSquare square = LoadoutSlotSquare.Create(cardRect, null, null);
        RectTransform sq = square.Rect;
        sq.anchorMin = sq.anchorMax = new Vector2(0.5f, 1f);
        sq.pivot = new Vector2(0.5f, 1f);
        sq.anchoredPosition = Vector2.zero;
        sq.sizeDelta = new Vector2(CardSquare, CardSquare);

        if (unlocked)
        {
            LoadoutSlot slot = Instantiate(slotPrefab, square.transform);
            slot.Initialize(data, OnUnlockedSlotClicked, ShowTooltip, HideTooltip);
            square.ShowPlant(slot, data);
            // the name sits under the square now
            foreach (TMP_Text own in slot.GetComponentsInChildren<TMP_Text>(true)) own.gameObject.SetActive(false);
            AddRecommendedBadge(slot, data);
            unlockedSlots.Add(slot);
            // a plant already in the loadout is greyed out here
            if (selectedLoadout.Contains(data.plantName)) card.GetComponent<CanvasGroup>().alpha = 0.35f;
        }
        else
        {
            square.ShowOpen();
            // its sprite, all dark, between the backdrop and the frame
            GameObject silhouette = new GameObject("Silhouette", typeof(RectTransform), typeof(Image));
            silhouette.layer = card.layer;
            RectTransform r = (RectTransform)silhouette.transform;
            r.SetParent(square.transform, false);
            r.SetSiblingIndex(Mathf.Max(0, square.transform.childCount - 2));
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(CardSquare * 0.9f, CardSquare * 0.9f);
            Image image = silhouette.GetComponent<Image>();
            image.sprite = data.icon;
            image.preserveAspect = true;
            image.color = SilhouetteColor;
            image.raycastTarget = false;
        }

        // the name, then the sun cost (the one placing it charges, the skill tree's reduction included)
        TextMeshProUGUI name = NewText(cardRect, unlocked ? data.displayName : "???", 16f, TextAlignmentOptions.Top);
        // a long name gets a smaller size so it fits the card (set by hand: its shadow copies the size)
        float nameWidth = name.GetPreferredValues(name.text).x;
        float roomWidth = CardSquare + 4f;
        if (nameWidth > roomWidth) name.fontSize = Mathf.Max(10f, Mathf.Floor(16f * roomWidth / nameWidth));
        PlaceUnder(name.rectTransform, CardSquare + 3f, CardSquare + 6f, CardNameHeight);
        name.gameObject.AddComponent<PixelTextEffect>().Color = InsectShowcaseStyle.Instance.plantTitleShadow;
        AddCost(cardRect, unlocked ? SkillTreeManager.GetEffectiveSunCost(data).ToString() : "?", CardSquare + 3f + CardNameHeight + 1f);
    }

    // the cost under a card's name: the sun glyph, then the number, centred together
    private void AddCost(RectTransform card, string cost, float top)
    {
        PlantInfoPanelStyle look = PlantInfoPanelStyle.Instance;
        TextMeshProUGUI text = NewText(card, cost, 16f, TextAlignmentOptions.TopLeft);
        float textWidth = Mathf.Ceil(text.GetPreferredValues(cost).x);
        const float SunSize = 14f, Gap = 4f;
        float total = SunSize + Gap + textWidth;

        GameObject sunGO = new GameObject("Sun", typeof(RectTransform), typeof(Image));
        sunGO.layer = card.gameObject.layer;
        RectTransform sun = (RectTransform)sunGO.transform;
        sun.SetParent(card, false);
        sun.anchorMin = sun.anchorMax = new Vector2(0.5f, 1f);
        sun.pivot = new Vector2(0f, 1f);
        sun.anchoredPosition = new Vector2(Mathf.Round(-total / 2f), -(top + 2f));
        sun.sizeDelta = new Vector2(SunSize, SunSize);
        Image sunImage = sunGO.GetComponent<Image>();
        sunImage.sprite = look.sunGlyph;
        sunImage.color = look.normal.sun;
        sunImage.raycastTarget = false;

        RectTransform r = text.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(Mathf.Round(-total / 2f + SunSize + Gap), -top);
        r.sizeDelta = new Vector2(textWidth + 2f, CardCostHeight);
        text.gameObject.AddComponent<PixelTextEffect>().Color = InsectShowcaseStyle.Instance.plantTitleShadow;
    }

    private TextMeshProUGUI NewText(Transform parent, string value, float size, TextAlignmentOptions align)
    {
        InsectShowcaseStyle s = InsectShowcaseStyle.Instance;
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.layer = panel.layer;
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        if (s.font != null) text.font = s.font;
        text.fontSize = size;
        text.color = s.text;
        text.alignment = align;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.margin = Vector4.zero;
        text.text = value;
        return text;
    }

    // a rect centred under the top of its card, top units down, width wide and height tall
    private static void PlaceUnder(RectTransform r, float top, float width, float height)
    {
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -top);
        r.sizeDelta = new Vector2(width, height);
    }

    private void OnUnlockedSlotClicked(string plantName)
    {
        if (selectedLoadout.Contains(plantName) || selectedLoadout.Count >= SaveManager.instance.saveData.MaxLoadoutSize) return;
        selectedLoadout.Add(plantName);
        RefreshUI();

        if (plantName == "AcornKnight")
            TutorialManager.instance?.NotifyEvent(TutorialEvents.SelectAcornKnightSlot);
    }

    private void OnSelectedSlotClicked(string plantName)
    {
        selectedLoadout.Remove(plantName);
        RefreshUI();
    }

    public void Confirm()
    {
        TutorialManager.instance?.NotifyEvent(TutorialEvents.ClickConfirmButton);
        StopIntro();
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

            // the shine over the whole panel, kept out of its layout group
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
        }
        Color panelColor = PlantData.ElementPanelColor(data.elementalType);
        tooltipBody.color = panelColor;
        foreach (Image box in tooltipBoxes)
            if (box != null) box.color = panelColor;
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

        // Attack, Passive, Skill, then the element: each section a dark inset box
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
            if (inner != null) inner.padding = new RectOffset(14, 14, 10, 12);
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
    // it, every text in it gets the same smaller share of its own font size, the biggest that fits
    // (Tommy, 2026-11-24, as the level's tooltip does: PlantUpgradeUI.LayoutTooltip)
    private TMP_Text[] tooltipTexts;
    private float[] tooltipBaseSizes;
    private const float TooltipMinScale = 0.35f;

    private void FitTooltip()
    {
        if (tooltipPanel == null) return;
        RectTransform tip = (RectTransform)tooltipPanel.transform;
        if (tooltipTexts == null)
        {
            tooltipTexts = tooltipPanel.GetComponentsInChildren<TMP_Text>(true);
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
