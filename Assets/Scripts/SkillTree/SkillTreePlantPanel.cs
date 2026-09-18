using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// self-contained panel for one plant's skill tree: sprite, name, exp, level, and its node chain.
// the nodes are NOT hand-placed - this builds them at runtime from plantData.skillTree, spawning
// nodeButtonPrefab once per node, so the whole node chain (button look, spacing, fork layout)
// only ever needs editing in ONE place (this script's constants and the shared prefab) rather
// than being redone per plant. duplicating a plant's panel is then just swapping plantData
//
// the panel hides itself entirely unless the plant is both unlocked (in SaveData.unlockedPlants)
// and has an authored SkillTreeData assigned, so newly added plants simply appear once both are
// true, with no manual scene work needed per plant beyond duplicating this panel
public class SkillTreePlantPanel : MonoBehaviour
{
    [SerializeField] private PlantData plantData;

    [Header("Refs")]
    [SerializeField] private Image plantSprite;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text pointsText;

    [Header("Node Building")]
    [SerializeField] private Transform nodesContainer;
    [SerializeField] private SkillNodeButton nodeButtonPrefab;
    [SerializeField] private float stepSpacing = 24f;
    [SerializeField] private float forkNodeSpacing = 12f;

    private readonly List<SkillNodeButton> nodeButtons = new List<SkillNodeButton>();

    // called by SkillTreeUI right after Instantiate, before this panel's own Start runs - lets a
    // spawner assign the plant instead of hand-setting it in the Inspector per duplicated panel
    public void Bind(PlantData data) => plantData = data;

    private void Start()
    {
        if (!ShouldShow())
        {
            gameObject.SetActive(false);
            return;
        }

        SkillTreeUI.instance?.RegisterPanel(this);

        if (plantSprite != null) plantSprite.sprite = plantData.icon;
        if (nameText != null) nameText.text = string.IsNullOrEmpty(plantData.displayName) ? plantData.plantName : plantData.displayName;

        BuildNodes();
        Refresh();
    }

    private bool ShouldShow()
    {
        if (plantData == null || plantData.skillTree == null) return false;
        if (nodeButtonPrefab == null || nodesContainer == null) return false;
        if (SaveManager.instance == null) return false;
        return SaveManager.instance.saveData.unlockedPlants.Contains(plantData.plantName);
    }

    // one column per step (a plain RectTransform with a VerticalLayoutGroup, so a fork's two
    // nodes stack instead of sitting side by side), each column holding that step's node
    // button(s). this is the only place any plant's node buttons get created
    private void BuildNodes()
    {
        nodeButtons.Clear();
        SkillTreeData tree = plantData.skillTree;

        HorizontalLayoutGroup rowLayout = nodesContainer.GetComponent<HorizontalLayoutGroup>();
        if (rowLayout == null) rowLayout = nodesContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = stepSpacing;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        // grows the row to fit the tallest column (a fork step) rather than clipping it or
        // leaving mismatched columns awkwardly off-center within a too-small fixed rect
        ContentSizeFitter rowFitter = nodesContainer.GetComponent<ContentSizeFitter>();
        if (rowFitter == null) rowFitter = nodesContainer.gameObject.AddComponent<ContentSizeFitter>();
        rowFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int stepIndex = 0; stepIndex < tree.steps.Count; stepIndex++)
        {
            SkillTreeStep step = tree.steps[stepIndex];

            GameObject column = new GameObject($"Step{stepIndex + 1}", typeof(RectTransform));
            column.transform.SetParent(nodesContainer, false);
            VerticalLayoutGroup colLayout = column.AddComponent<VerticalLayoutGroup>();
            colLayout.spacing = forkNodeSpacing;
            colLayout.childAlignment = TextAnchor.MiddleCenter;
            colLayout.childControlWidth = false;
            colLayout.childControlHeight = false;
            colLayout.childForceExpandWidth = false;
            colLayout.childForceExpandHeight = false;

            // without this the column keeps its default 100x100 RectTransform regardless of
            // whether it holds 1 or 2 nodes, so a fork's pair (which needs more height than a
            // single node) gets centered inside a box sized for one - throwing off the symmetry
            // between the top node's gap to the midline and the bottom node's gap to it
            ContentSizeFitter colFitter = column.AddComponent<ContentSizeFitter>();
            colFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            colFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int nodeIndex = 0; nodeIndex < step.nodes.Count; nodeIndex++)
            {
                SkillNodeButton button = Instantiate(nodeButtonPrefab, column.transform);
                button.Init(SkillTreeUI.instance, tree, plantData.plantName, stepIndex, nodeIndex);
                nodeButtons.Add(button);
            }
        }
    }

    public void Refresh()
    {
        if (plantData == null) return;

        if (expText != null && SaveManager.instance != null)
        {
            int level = SaveManager.instance.saveData.GetPlantLevel(plantData.plantName);
            int exp = SaveManager.instance.saveData.GetPlantExp(plantData.plantName);
            string expValue = level >= PlantLevelCurve.MaxLevel ? $"{exp}" : $"{exp}/{PlantLevelCurve.ExpForNextLevel(level)}";
            expText.text = $"Exp: <b><color=green>{expValue}</color></b>";
        }

        if (levelText != null && SaveManager.instance != null)
            levelText.text = $"Level: <b><color=green>{SaveManager.instance.saveData.GetPlantLevel(plantData.plantName)}/{PlantLevelCurve.MaxLevel}</color></b>";

        if (pointsText != null)
            pointsText.text = $"Skill Points: <b><color=green>{SkillTreeSession.DisplaySkillPoints(plantData.plantName)}</color></b>";

        foreach (SkillNodeButton button in nodeButtons)
            button.Refresh();
    }
}
