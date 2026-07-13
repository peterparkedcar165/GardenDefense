using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// scene controller for the skill tree screen
// builds a plant icon row from unlocked plants and renders the selected plants chain
public class SkillTreeUI : MonoBehaviour
{
    public static SkillTreeUI instance;

    [Header("Data")]
    [SerializeField] private PlantRegistry plantRegistry;

    [Header("Plant Selection")]
    [SerializeField] private Transform plantIconContainer;
    [SerializeField] private Button plantIconPrefab;

    [Header("Tree")]
    [SerializeField] private Transform chainContainer;
    [SerializeField] private Transform stepColumnPrefab;
    [SerializeField] private SkillNodeButton nodeButtonPrefab;

    [Header("Info")]
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text plantNameText;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    private PlantData selectedPlant;
    private readonly List<SkillNodeButton> nodeButtons = new List<SkillNodeButton>();

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        BuildPlantIcons();
        HideTooltip();
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            OnBack();
    }

    private void BuildPlantIcons()
    {
        if (plantRegistry == null || SaveManager.instance == null) return;
        List<string> unlocked = SaveManager.instance.saveData.unlockedPlants;
        PlantData first = null;
        foreach (PlantData data in plantRegistry.plants)
        {
            if (data == null || data.skillTree == null) continue;
            if (!unlocked.Contains(data.plantName)) continue;
            PlantData captured = data;
            Button icon = Instantiate(plantIconPrefab, plantIconContainer);
            Image img = icon.GetComponent<Image>();
            if (img != null && data.icon != null) img.sprite = data.icon;
            icon.onClick.AddListener(() => SelectPlant(captured));
            if (first == null) first = data;
        }
        if (first != null) SelectPlant(first);
    }

    public void SelectPlant(PlantData data)
    {
        selectedPlant = data;
        BuildTree();
    }

    private void BuildTree()
    {
        foreach (Transform child in chainContainer) Destroy(child.gameObject);
        nodeButtons.Clear();
        if (selectedPlant == null || selectedPlant.skillTree == null) return;

        if (plantNameText != null)
            plantNameText.text = string.IsNullOrEmpty(selectedPlant.displayName) ? selectedPlant.plantName : selectedPlant.displayName;

        SkillTreeData tree = selectedPlant.skillTree;
        for (int i = 0; i < tree.steps.Count; i++)
        {
            Transform column = Instantiate(stepColumnPrefab, chainContainer);
            foreach (SkillTreeNode node in tree.steps[i].nodes)
            {
                SkillNodeButton button = Instantiate(nodeButtonPrefab, column);
                button.Init(this, tree, selectedPlant.plantName, i, node);
                nodeButtons.Add(button);
            }
        }
        RefreshAll();
    }

    public void RefreshAll()
    {
        if (pointsText != null && SaveManager.instance != null)
            pointsText.text = $"Skill Points: <b><color=green>{SaveManager.instance.saveData.skillPoints}</color></b>";
        foreach (SkillNodeButton button in nodeButtons)
            button.Refresh();
    }

    public void ShowTooltip(string text)
    {
        if (tooltipPanel == null) return;
        tooltipPanel.SetActive(true);
        if (tooltipText != null) tooltipText.text = text;
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    public void OnBack()
    {
        if (SceneTransition.IsTransitioning) return;
        SceneTransition t = FindAnyObjectByType<SceneTransition>();
        if (t != null) t.StartCoroutine(t.FadeToScene("MainMenu"));
    }
}
