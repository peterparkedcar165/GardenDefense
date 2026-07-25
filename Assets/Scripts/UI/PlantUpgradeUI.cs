using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

public class PlantUpgradeUI : EntityInfoPanel
{
    public static PlantUpgradeUI instance;

    [Header("Targeting")]
    [SerializeField] private TMP_Text targetingModeText;
    [SerializeField] private Button targetingToggleButton;

    [Header("Minions (Burgeon)")]
    [SerializeField] private Button relocateMinionsButton;   // visible only for plants that command minions
    [SerializeField] private float relocateIndicatorRadius = 0.5f;

    [Header("Flying Priority (Cattail)")]
    [SerializeField] private Button flyingToggleButton;      // visible only for plants with a flying-first toggle
    [SerializeField] private TMP_Text flyingToggleText;

    [Header("Health Bar")]
    [SerializeField] private GameObject healthBarRoot;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TMP_Text healthText;

    [Header("Temperature Bar")]
    [SerializeField] private GameObject tempBarRoot;
    [SerializeField] private Image tempBarBackground;
    [SerializeField] private TMP_Text tempText;
    [SerializeField] private TMP_Text tempStateText;
    [SerializeField] private RectTransform tempCurrentIndicator;
    [SerializeField] private RectTransform tempComfortMinIndicator;
    [SerializeField] private RectTransform tempComfortMaxIndicator;
    [SerializeField] private RectTransform tempMinIndicator;
    [SerializeField] private RectTransform tempMaxIndicator;

    [Header("Stats (shared)")]
    [SerializeField] private TMP_Text stat1Text;
    [SerializeField] private TMP_Text stat2Text;
    [SerializeField] private TMP_Text stat3Text;
    [SerializeField] private TMP_Text stat4Text;

    [Header("Upgrades Section")]
    [SerializeField] private GameObject upgradesSection;

    [Header("Path 1")]
    [SerializeField] private TMP_Text path1NameText;
    [SerializeField] private TMP_Text path1CostText;
    [SerializeField] private Image path1Pips;
    [SerializeField] private Button path1UpgradeButton;

    [Header("Path 2")]
    [SerializeField] private TMP_Text path2NameText;
    [SerializeField] private TMP_Text path2CostText;
    [SerializeField] private Image path2Pips;
    [SerializeField] private Button path2UpgradeButton;

    [Header("Path 3")]
    [SerializeField] private TMP_Text path3NameText;
    [SerializeField] private TMP_Text path3CostText;
    [SerializeField] private Image path3Pips;
    [SerializeField] private Button path3Button;
    [SerializeField] private GameObject path3LockOverlay;

    [Header("Skill")]
    [SerializeField] private Button skillButton;
    [SerializeField] private TMP_Text skillCooldownText;

    [Header("Status Effects")]
    [SerializeField] private StatusEffectPanel statusEffectPanel;

    [Header("Stats Panels")]
    [SerializeField] private GameObject statsPanels;

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    [Header("Pip Sprites")]
    [SerializeField] private Sprite[] pipSprites;
    [Header("Colors")]
    [SerializeField] private Color pipFilled = Color.yellow;
    [SerializeField] private Color pipEmpty = Color.black;

    [System.Serializable]
    private struct ElementalPanelTheme
    {
        public ElementalType elementalType;
        public Sprite backgroundSprite;
    }

    [Header("Elemental Theme")]
    [SerializeField] private Image panelBackground;
    [SerializeField] private ElementalPanelTheme[] elementalThemes;

    private void ApplyElementalTheme(ElementalType type)
    {
        if (panelBackground == null)
        {
            Debug.LogWarning("PlantUpgradeUI: Panel Background is not assigned, cannot apply elemental theme.");
            return;
        }
        foreach (var theme in elementalThemes)
        {
            if (theme.elementalType == type)
            {
                panelBackground.sprite = theme.backgroundSprite;
                return;
            }
        }
        Debug.LogWarning($"PlantUpgradeUI: no elemental theme set for {type}.");
    }

    private Plant selectedPlant;
    private Insect selectedInsect;
    private SpriteRenderer cachedInsectRenderer;

    protected override void Awake()
    {
        if (instance != null) return;
        instance = this;
        base.Awake();
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    protected override void Update()
    {
        base.Update();

        if (tooltipPanel != null && tooltipPanel.activeSelf && selectedInsect != null && tooltipText != null)
        {
            string desc = selectedInsect.GetDescription();
            if (!string.IsNullOrEmpty(desc))
                tooltipText.text = $"{selectedInsect.GetName()}\n\n{desc}";
        }

        if (selectedInsect != null && Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
        {
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit == null || hit.GetComponentInParent<Insect>() == null)
                HidePanel();
        }

        if (selectedPlant != null)
        {
            RefreshSkillButton();
            RefreshPaths();
            if (Keyboard.current.qKey.wasPressedThisFrame && selectedPlant.SkillReady)
                selectedPlant.ActivateSkill();
        }
    }

    protected override bool ShouldBlockRightClickClose()
    {
        return SkillTargetingManager.instance.IsTargeting || SkillTargetingManager.instance.WasCancelledThisFrame;
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

        if (upgradesSection != null) upgradesSection.SetActive(true);
        if (healthBarRoot != null)   healthBarRoot.SetActive(true);
        if (stat4Text != null)       stat4Text.gameObject.SetActive(false);
        if (targetingToggleButton != null)
            targetingToggleButton.gameObject.SetActive(plant.UsesTargeting);
        if (targetingModeText != null)
            targetingModeText.text = plant.targeting.ToString();
        if (relocateMinionsButton != null)
            relocateMinionsButton.gameObject.SetActive(plant.CommandsMinions);
        if (flyingToggleButton != null)
            flyingToggleButton.gameObject.SetActive(plant.UsesFlyingToggle);
        if (flyingToggleText != null)
            flyingToggleText.text = plant.prioritizeFlying ? "Flying First" : "Default";

        panel.SetActive(true);
        ApplyElementalTheme(plant.elementalType);

        entityNameText.text = plant.GetName();
        if (entityIcon != null && plant.data != null)
            entityIcon.sprite = plant.data.icon;

        statusEffectPanel?.Show(plant);
        RefreshStats();
        RefreshPaths();
    }

    public void ShowPanel(Insect insect)
    {
        if (selectedPlant != null) selectedPlant.Deselect();
        selectedPlant = null;

        selectedInsect = insect;
        insect.ShowHealthBar();
        cachedInsectRenderer = insect.GetComponentInChildren<SpriteRenderer>();

        if (upgradesSection != null) upgradesSection.SetActive(false);
        if (tempBarRoot != null)     tempBarRoot.SetActive(false);
        if (targetingToggleButton != null)
            targetingToggleButton.gameObject.SetActive(false);
        if (relocateMinionsButton != null)
            relocateMinionsButton.gameObject.SetActive(false);
        if (flyingToggleButton != null)
            flyingToggleButton.gameObject.SetActive(false);
        if (stat4Text != null)       stat4Text.gameObject.SetActive(false);

        panel.SetActive(true);
        ApplyElementalTheme(ElementalType.Neutral);

        string desc = insect.GetDescription();
        entityNameText.text = string.IsNullOrEmpty(desc)
            ? insect.GetName()
            : $"{insect.GetName()}\n<size=70%><color=grey>{desc}</color></size>";
        if (entityIcon != null && cachedInsectRenderer != null)
            entityIcon.sprite = cachedInsectRenderer.sprite;

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
        base.HidePanel();
        HideTooltip();
    }

    public Plant  GetSelectedPlant()  => selectedPlant;
    public Insect GetSelectedInsect() => selectedInsect;

    // --- Stats ---

    protected override void RefreshStats()
    {
        statusEffectPanel?.Refresh();

        if (selectedPlant != null)
        {
            stat1Text.text = $"ATK: {selectedPlant.attackDamage:F0}";
            stat2Text.text = $"SPD: {selectedPlant.attackSpeed:F2}";
            stat3Text.text = $"RNG: {selectedPlant.attackRange:F1}";
            RefreshHealthBar();
            RefreshTemperatureBar(selectedPlant);
        }
        else if (selectedInsect != null)
        {
            string desc = selectedInsect.GetDescription();
            entityNameText.text = string.IsNullOrEmpty(desc)
                ? selectedInsect.GetName()
                : $"{selectedInsect.GetName()}\n<size=70%><color=grey>{desc}</color></size>";
            stat1Text.text = $"ATK: {selectedInsect.attackDamage:F0}";
            stat2Text.text = $"SPD: {selectedInsect.movementSpeed:F2}";
            stat3Text.text = $"SUN: {selectedInsect.sunDrop}";
            RefreshHealthBar();
        }
    }

    private void RefreshHealthBar()
    {
        float health, maxHealth, shield = 0f;

        if (selectedPlant != null)
        {
            health    = selectedPlant.health;
            maxHealth = selectedPlant.maxHealth;
            shield    = selectedPlant.TotalShield;
        }
        else if (selectedInsect != null)
        {
            health    = selectedInsect.health;
            maxHealth = selectedInsect.maxHealth;
            shield    = selectedInsect.TotalShield;
        }
        else return;

        if (healthBarFill != null)
        {
            float pct = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;
            healthBarFill.fillAmount = pct;
            healthBarFill.color = pct <= 0.25f ? Color.red
                                : pct <= 0.50f ? Color.yellow
                                : Color.green;
        }
        if (healthText != null)
        {
            string shieldSuffix = shield > 0f ? $" <b><color=#888888>(+{shield:F0})</color></b>" : "";
            healthText.text = $"{health:F0}/{maxHealth:F0}{shieldSuffix}";
        }
    }

    // --- Paths ---

    private void RefreshPaths()
    {
        // Path 1
        path1NameText.text = selectedPlant.GetPath1Name();
        RefreshPips(path1Pips, selectedPlant.path1Level);
        bool path1Maxed = selectedPlant.path1Level >= Plant.pathLevelCap;
        path1UpgradeButton.interactable = !path1Maxed;
        path1CostText.text = path1Maxed ? CapLabel(selectedPlant.path1Level) : $"{selectedPlant.GetPath1Cost()} Sun";

        // Path 2
        path2NameText.text = selectedPlant.GetPath2Name();
        RefreshPips(path2Pips, selectedPlant.path2Level);
        bool path2Maxed = selectedPlant.path2Level >= Plant.pathLevelCap;
        path2UpgradeButton.interactable = !path2Maxed;
        path2CostText.text = path2Maxed ? CapLabel(selectedPlant.path2Level) : $"{selectedPlant.GetPath2Cost()} Sun";

        // Path 3
        path3NameText.text = selectedPlant.GetPath3Name();
        bool unlocked = selectedPlant.path3Unlocked;
        path3LockOverlay.SetActive(!unlocked);

        if (!unlocked)
        {
            path3CostText.text = $"{selectedPlant.GetPath3Cost()} Sun to unlock.";
            path3Button.interactable = true;
            RefreshPips(path3Pips, selectedPlant.path3Level);
        }
        else
        {
            RefreshPips(path3Pips, selectedPlant.path3Level);
            bool path3Maxed = selectedPlant.path3Level >= Plant.pathLevelCap;
            path3Button.interactable = !path3Maxed;
            path3CostText.text = path3Maxed ? CapLabel(selectedPlant.path3Level) : $"{selectedPlant.GetPath3Cost()} Sun";
        }
    }

    // maxed at the true cap shows max, capped early by the level shows locked
    private static string CapLabel(int pathLevel) =>
        pathLevel >= Plant.absoluteLevelCap ? "MAX" : "LOCKED";

    private void RefreshPips(Image pips, int level)
    {
        pips.sprite = pipSprites[level];
    }

    private void RefreshSkillButton()
    {
        if (skillButton == null) return;
        bool ready = selectedPlant.SkillReady;
        skillButton.interactable = ready;
        if (skillCooldownText != null)
        {
            if (selectedPlant.IsSilenced)
                skillCooldownText.text = "SILENCED";
            else
                skillCooldownText.text = ready ? "Skill (Q)" : $"{Mathf.CeilToInt(selectedPlant.skillCooldownTimer)}s";
        }
    }

    // Button callbacks , wired in Inspector
    public void OnPath1UpgradeClicked()
    {
        if (selectedPlant == null) return;
        selectedPlant.UpgradePath1();
        RefreshPaths();
    }

    public void OnPath2UpgradeClicked()
    {
        if (selectedPlant == null) return;
        selectedPlant.UpgradePath2();
        RefreshPaths();
    }

    public void OnPath3UpgradeClicked()
    {
        if (selectedPlant == null) return;
        if (!selectedPlant.path3Unlocked)
            selectedPlant.UnlockPath3();
        else
            selectedPlant.UpgradePath3();
        RefreshPaths();
    }

    public void OnSkillButtonClicked()
    {
        if (selectedPlant == null || !selectedPlant.SkillReady) return;
        selectedPlant.ActivateSkill();
    }

    public void OnTargetingToggleClicked()
    {
        if (selectedPlant == null || !selectedPlant.UsesTargeting) return;
        int count = System.Enum.GetValues(typeof(TARGETING)).Length;
        selectedPlant.targeting = (TARGETING)(((int)selectedPlant.targeting + 1) % count);
        targetingModeText.text = selectedPlant.targeting.ToString();
    }

    // toggle the selected plant between prioritizing flyers and normal targeting
    public void OnFlyingToggleClicked()
    {
        if (selectedPlant == null || !selectedPlant.UsesFlyingToggle) return;
        selectedPlant.prioritizeFlying = !selectedPlant.prioritizeFlying;
        if (flyingToggleText != null)
            flyingToggleText.text = selectedPlant.prioritizeFlying ? "Flying First" : "Default";
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

    // --- Temperature Bar ---

    private void RefreshTemperatureBar(Plant plant)
    {
        bool isTempLevel = WeatherManager.instance != null &&
            (WeatherManager.instance.temperature == TemperatureType.Hot ||
             WeatherManager.instance.temperature == TemperatureType.Cold);

        if (tempBarRoot != null)
            tempBarRoot.SetActive(isTempLevel);

        if (!isTempLevel) return;

        Color tempColor = GetTempColor(plant);

        if (tempBarBackground != null)
            tempBarBackground.color = tempColor;

        if (tempText != null)
        {
            tempText.text  = $"{plant.temperature:F1}°";
            tempText.color = tempColor;
        }

        if (tempStateText != null)
        {
            if (plant.temperature < plant.comfortMin)
                tempStateText.text = "Cold";
            else if (plant.temperature > plant.comfortMax)
                tempStateText.text = "Hot";
            else
                tempStateText.text = "Comfort";
        }

        float min = plant.temperatureMin, max = plant.temperatureMax;
        SetTempIndicator(tempCurrentIndicator,    plant.temperature, min, max);
        SetTempIndicator(tempComfortMinIndicator, plant.comfortMin,  min, max);
        SetTempIndicator(tempComfortMaxIndicator, plant.comfortMax,  min, max);
        SetTempIndicator(tempMinIndicator,        min,               min, max);
        SetTempIndicator(tempMaxIndicator,        max,               min, max);
    }

    private static void SetTempIndicator(RectTransform indicator, float value, float min, float max)
    {
        if (indicator == null) return;
        float t = max > min ? Mathf.Clamp01((value - min) / (max - min)) : 0f;
        indicator.anchorMin = new Vector2(t, indicator.anchorMin.y);
        indicator.anchorMax = new Vector2(t, indicator.anchorMax.y);
        indicator.anchoredPosition = Vector2.zero;
    }

    private static Color GetTempColor(Plant plant)
    {
        float temp = plant.temperature;
        if (temp < plant.comfortMin)  return Color.cyan;
        if (temp > plant.comfortMax)  return new Color(1f, 0.5f, 0f);
        return Color.green;
    }

    // --- Tooltips ---

    public void ShowTooltip(string text)
    {
        if (tooltipPanel == null) return;
        tooltipPanel.SetActive(true);
        tooltipText.text = text;
        Canvas.ForceUpdateCanvases();
        RectTransform panelRect = tooltipPanel.GetComponent<RectTransform>();

        float maxHeight = Screen.height * 0.975f;
        float height = Mathf.Min(tooltipText.preferredHeight + 40f, maxHeight);
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        Canvas.ForceUpdateCanvases();
        Vector3[] corners = new Vector3[4];
        panelRect.GetWorldCorners(corners);
        float topY    = corners[1].y;
        float bottomY = corners[0].y;
        if (topY > Screen.height)
            panelRect.anchoredPosition += Vector2.down * (topY - Screen.height);
        else if (bottomY < 0f)
            panelRect.anchoredPosition += Vector2.up * -bottomY;
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
}
