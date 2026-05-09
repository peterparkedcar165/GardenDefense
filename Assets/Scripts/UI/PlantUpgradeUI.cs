using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class PlantUpgradeUI : MonoBehaviour
{
    public static PlantUpgradeUI instance;

    [Header("Panel")]

    [SerializeField] private GameObject panel;

    [Header("Header")]
    [SerializeField] private Image plantIcon;
    [SerializeField] private TMP_Text plantNameText;
    [SerializeField] private TMP_Text targetingModeText;
    [SerializeField] private Button targetingToggleButton;

    [Header("Stats")]
    [SerializeField] private TMP_Text attackDamageText;
    [SerializeField] private TMP_Text attackSpeedText;
    [SerializeField] private TMP_Text attackRangeText;

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

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;


    [Header("Pip Sprites")]
    [SerializeField] private Sprite[] pipSprites;
    [Header("Colors")]
    [SerializeField] private Color pipFilled = Color.yellow;
    [SerializeField] private Color pipEmpty = Color.black;

    private Plant selectedPlant;
    void Awake()
    {
      instance = this;
      panel.SetActive(false);
      if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }  
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame && selectedPlant != null)
        {
            HidePanel();
        }

        if (selectedPlant != null)
        {
            RefreshStats();
            RefreshSkillButton();

            if (Keyboard.current.qKey.wasPressedThisFrame && selectedPlant.SkillReady)
                selectedPlant.ActivateSkill();
        }
    }

    public void ShowPanel(Plant plant)
    {
        InsectInfoUI.instance.HidePanel();
        selectedPlant = plant;
        panel.SetActive(true);
        Refresh();
    }

    public void HidePanel()
    {
        selectedPlant = null;
        panel.SetActive(false);
        HideTooltip();
    }

    public Plant GetSelectedPlant()
    {
        return selectedPlant;
    }

    private void Refresh()
    {
        if(selectedPlant == null)
        {
            return;
        }

        plantNameText.text = selectedPlant.GetName();

        if(plantIcon != null && selectedPlant.data != null)
        {
            plantIcon.sprite = selectedPlant.data.icon;
        }

        if(selectedPlant is Shooter shooter)
        {
            targetingModeText.text = shooter.targeting.ToString();
            targetingToggleButton.gameObject.SetActive(true);
        }
        else
        {
            targetingToggleButton.gameObject.SetActive(false);
        }

        RefreshStats();
        RefreshPaths();
    }

    private void RefreshStats()
    {
        attackDamageText.text = $"ATK: {selectedPlant.attackDamage:F0}";
        attackSpeedText.text = $"SPD: {selectedPlant.attackSpeed:F2}";
        attackRangeText.text = $"RNG: {selectedPlant.attackRange:F1}";
        // dear claude, please explain what this method does
    }

    private void RefreshPaths()
    {
        // Path 1
        path1NameText.text = selectedPlant.GetPath1Name();
        RefreshPips(path1Pips, selectedPlant.path1Level);
        bool path1Maxed = selectedPlant.path1Level >= Plant.pathLevelCap;
        path1UpgradeButton.interactable = !path1Maxed;
        path1CostText.text = path1Maxed ? "MAX" : $"{selectedPlant.GetPath1Cost()} Sun";

        // Path 2
        path2NameText.text = selectedPlant.GetPath2Name();
        RefreshPips(path2Pips, selectedPlant.path2Level);
        bool path2Maxed = selectedPlant.path2Level >= Plant.pathLevelCap;
        path2UpgradeButton.interactable = !path2Maxed;
        path2CostText.text = path2Maxed ? "MAX" : $"{selectedPlant.GetPath2Cost()} Sun";

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
            path3CostText.text = path3Maxed ? "MAX" : $"{selectedPlant.GetPath3Cost()} Sun";
        }
    }

    private void RefreshPips(Image pips, int level)
    {
        pips.sprite = pipSprites[level];;
    }

    private void RefreshSkillButton()
    {
        if (skillButton == null) return;
        bool ready = selectedPlant.SkillReady;
        skillButton.interactable = ready;
        if (skillCooldownText != null)
            skillCooldownText.text = ready ? "Q - Use Skill" : $"{Mathf.CeilToInt(selectedPlant.skillCooldownTimer)}s";
    }

    // Button Callbacks - wiring within inspector
    public void OnPath1UpgradeClicked()
    {
        if (selectedPlant == null)
        {
            return;
        }
        else
        {
            selectedPlant.UpgradePath1();
            RefreshPaths();
        }
    }

    public void OnPath2UpgradeClicked()
    {
        if (selectedPlant == null)
        {
            return;
        }
        else
        {
            selectedPlant.UpgradePath2();
            RefreshPaths();
        }
    }

    public void OnPath3UpgradeClicked()
    {
        if (selectedPlant == null)
        {
            return;
        }
        else
        {
            if (!selectedPlant.path3Unlocked)
            {
                selectedPlant.UnlockPath3();
            } else
            {
                selectedPlant.UpgradePath3();
            }

            RefreshPaths();
        }
    }

    public void OnSkillButtonClicked()
    {
        if (selectedPlant == null || !selectedPlant.SkillReady) return;
        selectedPlant.ActivateSkill();
    }

    public void OnTargetingToggleClicked()
    {
        if (selectedPlant is Shooter shooter)
        {
            int count = System.Enum.GetValues(typeof(TARGETING)).Length;
            shooter.targeting = (TARGETING)(((int)shooter.targeting + 1) % count);
            targetingModeText.text = shooter.targeting.ToString();
        }
    }

    // TOOPTIPS

    public void ShowTooltip(string text)
    {
        if (tooltipPanel == null)
        {
            return;
        } 
        else
        {
            tooltipPanel.SetActive(true);
            tooltipText.text = text;
        }

    Canvas.ForceUpdateCanvases();
    RectTransform panelRect = tooltipPanel.GetComponent<RectTransform>();
    panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tooltipText.preferredHeight + 40f);
    }
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}
