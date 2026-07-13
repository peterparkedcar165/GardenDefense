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
    [SerializeField] private Transform insectContainer;
    [SerializeField] private InsectPreviewSlot insectSlotPrefab;
    [SerializeField] private GameObject insectTooltipPanel;
    [SerializeField] private TMP_Text insectTooltipDescription;
    [SerializeField] private TMP_Text insectTooltipPassiveDescription;

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
    private List<LoadoutSlot> selectedSlots = new List<LoadoutSlot>();
    private readonly List<InsectPreviewSlot> insectSlots = new List<InsectPreviewSlot>();

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
        if (!IsOpen || SceneTransition.IsTransitioning) return;
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

    private void PopulateInsectPanel()
    {
        if (insectContainer == null || insectSlotPrefab == null) return;

        foreach (var slot in insectSlots)
            if (slot != null) Destroy(slot.gameObject);
        insectSlots.Clear();

        SpawnManager spawner = FindAnyObjectByType<SpawnManager>();
        if (spawner == null) return;

        var seen = new HashSet<InsectData>();
        foreach (GameObject prefab in spawner.GetInsectPrefabs())
        {
            if (prefab == null) continue;
            Insect insect = prefab.GetComponent<Insect>();
            if (insect == null || insect.data == null) continue;
            if (!seen.Add(insect.data)) continue;

            InsectPreviewSlot slot = Instantiate(insectSlotPrefab, insectContainer);
            slot.Initialize(insect.data, ShowInsectTooltip, HideInsectTooltip);
            insectSlots.Add(slot);
        }

    }

    private void ShowInsectTooltip(InsectData data)
    {
        if (insectTooltipPanel == null) return;
        insectTooltipPanel.SetActive(true);

        if (insectTooltipDescription != null)
            insectTooltipDescription.text = data.description;

        bool hasPassive = !string.IsNullOrWhiteSpace(data.passiveDescription);
        if (insectTooltipPassiveDescription != null)
        {
            insectTooltipPassiveDescription.gameObject.SetActive(hasPassive);
            insectTooltipPassiveDescription.text = data.passiveDescription;
        }
    }

    private void HideInsectTooltip()
    {
        if (insectTooltipPanel != null) insectTooltipPanel.SetActive(false);
    }

    public void RefreshUI()
    {
        Debug.Log("RefreshUI called. Unlocked plants: " + SaveManager.instance.saveData.unlockedPlants.Count);
        foreach (string p in SaveManager.instance.saveData.unlockedPlants)
            Debug.Log("Plant: " + p);

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
            unlockedSlots.Add(slot);
        }

        foreach (var s in selectedSlots) Destroy(s.gameObject);
        selectedSlots.Clear();

        foreach (string plantName in selectedLoadout)
        {
            PlantData data = GetPlantData(plantName);
            if (data == null) continue;
            LoadoutSlot slot = Instantiate(slotPrefab, selectedContainer);
            slot.Initialize(data, OnSelectedSlotClicked, ShowTooltip, HideTooltip);
            selectedSlots.Add(slot);
        }

        confirmButton.interactable = selectedLoadout.Count > 0;
    }

    private void OnUnlockedSlotClicked(string plantName)
    {
        if (selectedLoadout.Contains(plantName) || selectedLoadout.Count >= SaveManager.instance.saveData.MaxLoadoutSize) return;
        selectedLoadout.Add(plantName);
        RefreshUI();
    }

    private void OnSelectedSlotClicked(string plantName)
    {
        selectedLoadout.Remove(plantName);
        RefreshUI();
    }

    public void Confirm()
    {
        SaveManager.instance.selectedLoadout = new List<string>(selectedLoadout);
        panel.SetActive(false);
        HideTooltip();
        GameManager.instance?.OnLoadoutConfirmed();
    }

    private void ShowTooltip(PlantData data)
    {
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
            ElementalType.Nature  => "<color=green>Nature</color>",
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

    private PlantData GetPlantData(string plantName)
    {
        foreach (var data in plantRegistry.plants)
        {
            if (data == null) continue;
            Debug.Log($"Comparing '{plantName}' to '{data.plantName}'");
            if (data.plantName == plantName) return data;
        }

        return null;
    }
}
