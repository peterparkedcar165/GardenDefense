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
    private readonly List<GameObject> hiddenForMap = new List<GameObject>();
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
            RectTransform panelRect = (RectTransform)panel.transform;
            mapButton = PlankButton.Create(panelRect, "Show Map", GameControlsStyle.Instance.speed,
                InsectShowcaseStyle.Instance.font, () => SetMapShown(!mapShown));
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

        Rect upper = BoundsIn(panelRect, loadoutSection);
        Rect lower = BoundsIn(panelRect, plantsSection);
        if (upper.yMin < lower.yMin) { Rect t = upper; upper = lower; lower = t; Transform tt = loadoutSection; loadoutSection = plantsSection; plantsSection = tt; }
        const float Px = 2f;
        float pad = s.plantPanelPad * Px;
        // the two panels never touch: when the sections are close the pads between them shrink
        float gap = upper.yMin - lower.yMax;
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

    // what a section's active children cover, in the loadout panel's own space
    private static Rect BoundsIn(RectTransform space, Transform section)
    {
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        Vector3[] corners = new Vector3[4];
        foreach (Transform child in section)
        {
            if (!child.gameObject.activeSelf || !(child is RectTransform rt)) continue;
            rt.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 p = space.InverseTransformPoint(corner);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
        }
        return min.x > max.x ? new Rect(0f, 0f, 0f, 0f) : Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // Show Map: everything on the loadout panel but its button hides, and the dark backdrop goes, so
    // the whole map shows. off again, exactly what was showing comes back
    private void SetMapShown(bool shown)
    {
        if (mapShown == shown) return;
        mapShown = shown;
        if (shown)
        {
            HideTooltip();
            HideSlotHint();
            showcase?.HideTips();
            hiddenForMap.Clear();
            foreach (Transform child in panel.transform)
            {
                if (mapButton != null && child == mapButton.transform) continue;
                if (!child.gameObject.activeSelf) continue;
                hiddenForMap.Add(child.gameObject);
                child.gameObject.SetActive(false);
            }
            if (backdrop != null) backdrop.enabled = false;
        }
        else
        {
            foreach (GameObject go in hiddenForMap)
                if (go != null) go.SetActive(true);
            hiddenForMap.Clear();
            if (backdrop != null) backdrop.enabled = true;
        }
        if (mapButton != null) mapButton.SetLabel(shown ? "Hide Map" : "Show Map");
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
        foreach (var s in unlockedSlots){
            Destroy(s.gameObject);
        }
        unlockedSlots.Clear();

        List<string> unlocked = SaveManager.instance.saveData.unlockedPlants;
        foreach (PlantData data in plantRegistry.plants)
        {
            if (data == null) continue;
            if (!unlocked.Contains(data.plantName)) continue;

            LoadoutSlot slot = Instantiate(slotPrefab, unlockedContainer);
            slot.Initialize(data, OnUnlockedSlotClicked, ShowTooltip, HideTooltip);
            slot.SetDimmed(selectedLoadout.Contains(data.plantName));
            AddRecommendedBadge(slot, data);
            unlockedSlots.Add(slot);
        }

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

    // the plant tooltip is as tall as its texts, at most the screen's height less a small margin. when
    // they're too long for that, every text in it gets the same smaller share of its own font size, the
    // biggest that fits (Tommy, 2026-11-24, as the level's tooltip does: PlantUpgradeUI.LayoutTooltip)
    private TMP_Text[] tooltipTexts;
    private float[] tooltipBaseSizes;
    private const float TooltipScreenMargin = 16f;
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
        Canvas canvas = tooltipPanel.GetComponentInParent<Canvas>();
        RectTransform root = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
        float maxHeight = (root != null ? root.rect.height : Screen.height) - 2f * TooltipScreenMargin;

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

    // sets every tooltip text to scale times its own size, lays the panel out and returns its height
    private float TooltipHeightAt(RectTransform tip, float scale)
    {
        for (int i = 0; i < tooltipTexts.Length; i++)
            if (tooltipTexts[i] != null) tooltipTexts[i].fontSize = tooltipBaseSizes[i] * scale;
        LayoutRebuilder.ForceRebuildLayoutImmediate(tip);
        return tip.rect.height;
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
