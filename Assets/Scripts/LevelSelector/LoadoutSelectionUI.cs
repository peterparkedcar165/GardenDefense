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
        PopulateInsectPanel();
        RefreshUI();
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
        if (tooltipName != null)
            tooltipName.text = prefab != null ? prefab.GetName() : data.displayName;
        if (tooltipIcon != null)
            tooltipIcon.sprite = data.icon;
        if (tooltipDescription != null)
            tooltipDescription.text = prefab != null ? prefab.GetDescription() : "";
        if (tooltipAttackTitle != null)
            tooltipAttackTitle.text = "Attack";
        if (tooltipAttackDescription != null)
            tooltipAttackDescription.text = data.GetAttackDescription();
        if (tooltipPassiveTitle != null)
            tooltipPassiveTitle.text = "Passive";
        if (tooltipPassiveDescription != null)
            tooltipPassiveDescription.text = data.GetPassiveDescription();
        if (tooltipSkillTitle != null)
            tooltipSkillTitle.text = "Skill";
        if (tooltipSkillDescription != null)
            tooltipSkillDescription.text = data.GetSkillDescription();
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
