using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlantsPanelUI : MonoBehaviour
{
    [SerializeField] private PlantEntry entryPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private PlantRegistry plantRegistry;

    [Header("Detail Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailSunCost;
    [SerializeField] private TMP_Text detailDamageType;
    [SerializeField] private TMP_Text detailElementalType;
    [SerializeField] private TMP_Text detailElementalTypeDescription;
    [SerializeField] private TMP_Text detailAttackTitle;
    [SerializeField] private TMP_Text detailAttackDescription;
    [SerializeField] private TMP_Text detailPassiveTitle;
    [SerializeField] private TMP_Text detailPassiveDescription;
    [SerializeField] private TMP_Text detailSkillTitle;
    [SerializeField] private TMP_Text detailSkillDescription;
    [SerializeField] private TMP_Text detailStatsTitle;
    [SerializeField] private TMP_Text detailStatsDescription;

    private PlantData pinnedData;

    void Update()
    {
        if (pinnedData == null) return;
        if (Mouse.current.rightButton.wasPressedThisFrame)
            TryUnpin();
    }

    public bool TryUnpin()
    {
        if (pinnedData == null) return false;
        pinnedData = null;
        HideDetail();
        return true;
    }

    void Start()
    {
        foreach (PlantData data in plantRegistry.plants)
        {
            PlantEntry entry = Instantiate(entryPrefab, content);
            entry.Initialize(data, OnEntryHover, OnEntryHoverExit, OnEntryClicked);
        }
        if (detailPanel) detailPanel.SetActive(false);
    }

    private void OnEntryHover(PlantData data)
    {
        if (pinnedData != null) return;
        ShowDetail(data);
    }

    private void OnEntryHoverExit()
    {
        if (pinnedData != null) return;
        HideDetail();
    }

    private void OnEntryClicked(PlantData data)
    {
        if (pinnedData == data)
        {
            pinnedData = null;
            HideDetail();
        }
        else
        {
            pinnedData = data;
            ShowDetail(data);
        }
    }

    private void ShowDetail(PlantData data)
    {
        if (detailPanel == null) return;
        detailPanel.SetActive(true);

        Plant prefab = data.plantPrefab;
        if (detailName)                    detailName.text                    = $"<color={ElementalColor(data.elementalType)}>{data.displayName}</color>";
        if (detailSunCost)                 detailSunCost.text                 = $"<color=yellow>{data.sunCost} Sun</color>";
        if (detailDamageType)              detailDamageType.text              = ColoredDamage(data.damageType);
        if (detailElementalType)           detailElementalType.text           = $"Elemental Type: {ColoredElemental(data.elementalType)}";
        if (detailElementalTypeDescription) detailElementalTypeDescription.text = prefab != null ? prefab.GetElementDescription() : "";
        if (detailAttackTitle)             detailAttackTitle.text             = "Attack";
        if (detailAttackDescription)       detailAttackDescription.text       = data.GetAttackDescription();
        if (detailPassiveTitle)            detailPassiveTitle.text            = "Passive";
        if (detailPassiveDescription)      detailPassiveDescription.text      = data.GetPassiveDescription();
        if (detailSkillTitle)              detailSkillTitle.text              = "Skill";
        if (detailSkillDescription)        detailSkillDescription.text        = data.GetSkillDescription();
        if (detailStatsTitle)              detailStatsTitle.text              = "Stats";
        if (detailStatsDescription)
        {
            PlantBaseStats s = data.plantPrefab != null ? data.plantPrefab.GetBaseStats() : default;
            detailStatsDescription.text =
                $"HEALTH: <color=green><b>{data.baseMaxHealth}</b></color>\n" +
                $"ATTACK DAMAGE: <color=green><b>{s.attackDamage}</b></color>\n" +
                $"ATTACK SPEED: <color=green><b>{s.attackSpeed}</b></color>\n" +
                $"ATTACK RANGE: <color=green><b>{s.attackRange}</b></color>\n" +
                $"SKILL COOLDOWN: <color=green><b>{s.skillCooldown}s</b></color>";
        }
    }

    private void HideDetail()
    {
        if (detailPanel) detailPanel.SetActive(false);
    }

    private static string ElementalColor(ElementalType type) => type switch
    {
        ElementalType.Fire   => "orange",
        ElementalType.Water  => "#4FC3F7",
        ElementalType.Ice    => "#00FFFF",
        ElementalType.Wind   => "#B2EBF2",
        ElementalType.Nature => "green",
        ElementalType.Poison => "purple",
        _                    => "white"
    };

    private static string ColoredElemental(ElementalType type)
    {
        string color = ElementalColor(type);
        string name  = type == ElementalType.Neutral ? "Neutral" : type.ToString();
        return $"<color={color}>{name}</color>";
    }

    private static string ColoredDamage(DamageType type) => type switch
    {
        DamageType.Physical => "<color=#A0522D>Physical Damage</color>",
        DamageType.Magic    => "<color=#FFB6C1>Magic Damage</color>",
        _                   => type.ToString()
    };
}
