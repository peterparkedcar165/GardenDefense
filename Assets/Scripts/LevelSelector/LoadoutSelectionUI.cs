using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LoadoutSelectionUI : MonoBehaviour
{
    public static LoadoutSelectionUI instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private PlantData[] allPlantData;
    [SerializeField] private Transform unlockedContainer;
    [SerializeField] private Transform selectedContainer;
    [SerializeField] private LoadoutSlot slotPrefab;
    [SerializeField] private Button confirmButton;

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
    [SerializeField] private TMP_Text tooltipDamageType;
    [SerializeField] private TMP_Text tooltipStats;

    private int pendingLevel;
    private List<string> selectedLoadout = new List<string>();
    private List<LoadoutSlot> unlockedSlots = new List<LoadoutSlot>();
    private List<LoadoutSlot> selectedSlots = new List<LoadoutSlot>();

    public bool IsOpen => panel.activeSelf;

    void Awake()
    {
        instance = this;
        panel.SetActive(false);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (panel.activeSelf && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            panel.SetActive(false);
            selectedLoadout.Clear();
            HideTooltip();
        }
    }

    public void Show(int level)
    {
        pendingLevel = level;
        List<string> unlocked = SaveManager.instance.saveData.unlockedPlants;

        // if less than 4 unlocked, skip the screen
        if (unlocked.Count < 1)
        {
            SaveManager.instance.selectedLoadout = new List<string>(unlocked);
            LoadLevel();
            return;
        }

        panel.SetActive(true);
        selectedLoadout.Clear();
        RefreshUI();
        
    }

    private void RefreshUI()
    {
        Debug.Log("RefreshUI called. Unlocked plants: " + SaveManager.instance.saveData.unlockedPlants.Count);
        foreach (string p in SaveManager.instance.saveData.unlockedPlants)
            Debug.Log("Plant: " + p);

        foreach (var s in unlockedSlots){
            Destroy(s.gameObject);
        }
        unlockedSlots.Clear();

        List<string> unlocked = SaveManager.instance.saveData.unlockedPlants;
        foreach (PlantData data in allPlantData)
        {
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
        if (selectedLoadout.Contains(plantName) || selectedLoadout.Count >= 4) return;
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
        LoadLevel();
    }

    private void LoadLevel()
    {
        SceneTransition transition = FindAnyObjectByType<SceneTransition>();
        transition.StartCoroutine(transition.FadeToScene("Level"+pendingLevel));
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

    private void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    private PlantData GetPlantData(string plantName)
    {
        foreach (var data in allPlantData)
        {
            Debug.Log($"Comparing '{plantName}' to '{data.plantName}'");
            if (data.plantName == plantName) return data;
        }

        return null;
    }
}
