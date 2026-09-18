using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Text;
using System.Text.RegularExpressions;

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

    // a multi-rank node can already have some ranks confirmed while further ranks sit staged
    // on top - it must still read as "pending", not "confirmed", until those extra ranks are
    // actually confirmed too, or the button falsely looks auto-saved
    private bool IsLocked(out bool confirmed, out bool pendingPick)
    {
        pendingPick = SkillTreeSession.IsPending(plantName, node.id);
        confirmed = !pendingPick && SkillTreeSession.IsConfirmed(plantName, node.id);
        return !confirmed && !pendingPick &&
               (!SkillTreeSession.IsStepUnlockedDisplay(tree, plantName, stepIndex)
             || SkillTreeSession.IsExclusiveLockedDisplay(tree.steps[stepIndex], plantName, node));
    }

    private static string StripRichText(string text) => Regex.Replace(text, "<.*?>", "");

    public void Refresh()
    {
        if (node == null) return;

        bool locked = IsLocked(out bool confirmed, out bool pendingPick);

        int displayRank = SkillTreeSession.GetDisplayRank(plantName, node.id);
        if (rankText != null) rankText.text = $"{displayRank}/{node.maxRank}";

        if (background != null)
            background.color = confirmed ? confirmedColor
                              : pendingPick ? pendingColor
                              : locked ? lockedColor
                              : availableColor;
    }

    private bool hovering;

    // name, description, and a cost line - matches the same things the in-level skill/passive
    // tooltip shows, nothing more
    private string BuildTooltip()
    {
        bool locked = IsLocked(out _, out _);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(locked
            ? $"<b><color=#6E6E6E>{StripRichText(node.nodeName)}</color></b>"
            : $"<b><color=#FFD700>{node.nodeName}</color></b>");
        if (!string.IsNullOrEmpty(node.description))
            sb.AppendLine(locked ? $"<color=#6E6E6E>{StripRichText(node.description)}</color>" : node.description);
        sb.AppendLine();

        bool canAfford = SkillTreeSession.DisplaySkillPoints(plantName) >= node.costPerRank;
        string cost = canAfford ? $"<color=green><b>{node.costPerRank}</b></color>" : $"<color=red>{node.costPerRank}</color>";
        sb.AppendLine($"[COST]: {cost} point{(node.costPerRank == 1 ? "" : "s")}.");

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
