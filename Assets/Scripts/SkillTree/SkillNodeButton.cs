using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Text;

// one node in a plant's chain. spawned at runtime by SkillTreePlantPanel (one shared prefab for
// every plant), which passes in which slot of the tree this instance represents - so editing
// this prefab's look, or the panel's layout, changes every plant's tree at once instead of
// needing per-plant hand-duplicated copies
//
// left click stages a purchase (SkillTreeSession.TryStage); right click un-stages it if it
// hasn't been confirmed yet. a confirmed node no longer responds to right click at all
public class SkillNodeButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text rankText;

    [Header("State Colors")]
    [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color pendingColor = new Color(0.4f, 0.7f, 1f);
    [SerializeField] private Color confirmedColor = new Color(1f, 0.85f, 0.3f);

    private SkillTreeUI ui;
    private SkillTreeData tree;
    private string plantName;
    private int stepIndex;
    private SkillTreeNode node;

    // called by SkillTreePlantPanel right after Instantiate, with the exact slot this instance
    // represents in the plant's tree
    public void Init(SkillTreeUI ui, SkillTreeData tree, string plantName, int stepIndex, int nodeIndexInStep)
    {
        this.ui = ui;
        this.tree = tree;
        this.plantName = plantName;
        this.stepIndex = stepIndex;
        node = tree.steps[stepIndex].nodes[nodeIndexInStep];
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (node == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (!SkillTreeSession.TryStage(tree, plantName, stepIndex, node)) return;
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (!SkillTreeSession.TryUnstage(plantName, node.id)) return;
        }
        else return;

        Refresh();
        if (hovering) ui?.ShowTooltip(BuildTooltip());
    }

    public void Refresh()
    {
        if (node == null) return;

        bool confirmed = SkillTreeSession.IsConfirmed(plantName, node.id);
        bool pendingPick = !confirmed && SkillTreeSession.IsPending(plantName, node.id);
        bool locked = !confirmed && !pendingPick &&
                      (!SkillTreeSession.IsStepUnlockedDisplay(tree, plantName, stepIndex)
                    || SkillTreeSession.IsExclusiveLockedDisplay(tree.steps[stepIndex], plantName, node));

        int displayRank = SkillTreeSession.GetDisplayRank(plantName, node.id);
        if (rankText != null) rankText.text = $"{displayRank}/{node.maxRank}";

        if (background != null)
            background.color = confirmed ? confirmedColor
                              : pendingPick ? pendingColor
                              : locked ? lockedColor
                              : availableColor;
    }

    private bool hovering;

    // name, description, and a locked/unlocked line - matches the same three things the
    // in-level skill/passive tooltip shows, nothing more
    private string BuildTooltip()
    {
        bool confirmed = SkillTreeSession.IsConfirmed(plantName, node.id);
        bool pendingPick = !confirmed && SkillTreeSession.IsPending(plantName, node.id);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<b><color=#FFD700>{node.nodeName}</color></b>");
        if (!string.IsNullOrEmpty(node.description))
            sb.AppendLine(node.description);
        sb.AppendLine();

        if (confirmed)
            sb.AppendLine("<color=#FFD700>Unlocked (confirmed) - reset the tree to change this</color>");
        else if (pendingPick)
            sb.AppendLine("<color=#66B2FF>Unlocked (pending) - right click to undo</color>");
        else if (SkillTreeSession.IsExclusiveLockedDisplay(tree.steps[stepIndex], plantName, node))
            sb.AppendLine("<color=red>Locked: the other path was chosen</color>");
        else if (!SkillTreeSession.IsStepUnlockedDisplay(tree, plantName, stepIndex))
            sb.AppendLine("<color=red>Locked: invest in the previous node first</color>");
        else
            sb.AppendLine($"<color=grey>Locked - costs <b><color=green>{node.costPerRank}</color></b> skill point{(node.costPerRank == 1 ? "" : "s")} to unlock</color>");

        return sb.ToString().TrimEnd();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        ui?.ShowTooltip(BuildTooltip());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        ui?.HideTooltip();
    }
}
