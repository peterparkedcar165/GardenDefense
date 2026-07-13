using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Text;

// one node in the chain, shows name and rank, buys a rank on click, tooltip on hover
public class SkillNodeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text rankText;

    [Header("State Colors")]
    [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color maxedColor = new Color(1f, 0.85f, 0.3f);

    private SkillTreeUI ui;
    private SkillTreeData tree;
    private string plantName;
    private int stepIndex;
    private SkillTreeNode node;
    private bool hovering;

    public void Init(SkillTreeUI ui, SkillTreeData tree, string plantName, int stepIndex, SkillTreeNode node)
    {
        this.ui = ui;
        this.tree = tree;
        this.plantName = plantName;
        this.stepIndex = stepIndex;
        this.node = node;
        if (nameText != null) nameText.text = node.nodeName;
        if (button != null) button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (!SkillTreeManager.TryPurchase(tree, plantName, stepIndex, node)) return;
        ui.RefreshAll();
        if (hovering) ui.ShowTooltip(BuildTooltip());
    }

    public void Refresh()
    {
        int rank = SkillTreeManager.GetRank(plantName, node.id);
        bool maxed = rank >= node.maxRank;
        bool locked = !SkillTreeManager.IsStepUnlocked(tree, plantName, stepIndex)
                   || SkillTreeManager.IsExclusiveLocked(tree.steps[stepIndex], plantName, node);

        if (rankText != null) rankText.text = $"{rank}/{node.maxRank}";
        if (background != null) background.color = locked ? lockedColor : maxed ? maxedColor : availableColor;
        if (button != null) button.interactable = !locked && !maxed;
    }

    private string BuildTooltip()
    {
        int rank = SkillTreeManager.GetRank(plantName, node.id);
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<b><color=#FFD700>{node.nodeName}</color></b>  [{rank}/{node.maxRank}]");
        if (!string.IsNullOrEmpty(node.description))
            sb.AppendLine(node.description);
        sb.AppendLine();

        foreach (SkillNodeEffect effect in node.effects)
        {
            string perRank = FertilizerFormat.FormatValue(effect.statType, effect.valuePerRank);
            string current = FertilizerFormat.FormatValue(effect.statType, effect.valuePerRank * rank);
            sb.AppendLine($"{FertilizerFormat.FormatStatName(effect.statType)}: <b><color=green>{perRank}</color></b> per rank [<b><color=green>{current}</color></b>]");
        }

        if (SkillTreeManager.IsExclusiveLocked(tree.steps[stepIndex], plantName, node))
            sb.AppendLine("<color=red>Locked: the other path was chosen</color>");
        else if (!SkillTreeManager.IsStepUnlocked(tree, plantName, stepIndex))
            sb.AppendLine("<color=red>Locked: invest in the previous node first</color>");
        else if (rank >= node.maxRank)
            sb.AppendLine("<color=#FFD700>MAX</color>");
        else
            sb.AppendLine($"Cost: <b><color=green>{node.costPerRank}</color></b> skill point{(node.costPerRank == 1 ? "" : "s")}");

        return sb.ToString().TrimEnd();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        ui.ShowTooltip(BuildTooltip());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        ui.HideTooltip();
    }
}
