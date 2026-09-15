using System.Collections.Generic;

// staged-purchase layer for the Skill Tree screen. left-clicking a node stages it here (no
// save-data mutation, no points actually spent yet); right-clicking a still-staged node unstages
// it for a full refund. nothing is written to SaveData until Confirm is called - at that point
// staged picks become real via SkillTreeManager.TryPurchase, and can no longer be undone short of
// a full SaveManager.ResetSkillTrees(). SkillTreeManager itself is never given pending state, so
// gameplay's own SkillTreeManager.ApplyTo/HasUnlock calls (in actual level scenes) only ever see
// confirmed purchases, never anything mid-decision on this screen
public static class SkillTreeSession
{
    private struct Pending
    {
        public string plantName;
        public string nodeId;
        public int cost;
    }

    private static readonly List<Pending> pending = new List<Pending>();

    private static SaveData Data => SaveManager.instance != null ? SaveManager.instance.saveData : null;

    public static int PendingSpent
    {
        get
        {
            int total = 0;
            foreach (Pending p in pending) total += p.cost;
            return total;
        }
    }

    public static int DisplaySkillPoints => Data != null ? Data.skillPoints - PendingSpent : 0;

    // real (confirmed) rank plus 1 if currently staged - every node in this tree shape is
    // maxRank 1, so "staged or confirmed" is all display logic ever needs
    public static int GetDisplayRank(string plantName, string nodeId)
    {
        int savedRank = SkillTreeManager.GetRank(plantName, nodeId);
        if (savedRank > 0) return savedRank;
        return IsPending(plantName, nodeId) ? 1 : 0;
    }

    public static bool IsPending(string plantName, string nodeId)
    {
        foreach (Pending p in pending)
            if (p.plantName == plantName && p.nodeId == nodeId) return true;
        return false;
    }

    public static bool IsConfirmed(string plantName, string nodeId) =>
        SkillTreeManager.GetRank(plantName, nodeId) > 0;

    // same shape as SkillTreeManager.IsStepUnlocked/IsExclusiveLocked, but counting staged picks
    // too so a player can chain-buy several steps in one sitting before confirming
    public static bool IsStepUnlockedDisplay(SkillTreeData tree, string plantName, int stepIndex)
    {
        if (stepIndex <= 0) return true;
        SkillTreeStep previous = tree.steps[stepIndex - 1];
        int total = 0;
        foreach (SkillTreeNode node in previous.nodes)
            total += GetDisplayRank(plantName, node.id);
        return total >= previous.ranksToUnlockNext;
    }

    public static bool IsExclusiveLockedDisplay(SkillTreeStep step, string plantName, SkillTreeNode node)
    {
        if (step.nodes.Count < 2) return false;
        foreach (SkillTreeNode other in step.nodes)
            if (other != node && GetDisplayRank(plantName, other.id) > 0) return true;
        return false;
    }

    public static bool CanStage(SkillTreeData tree, string plantName, int stepIndex, SkillTreeNode node)
    {
        if (Data == null) return false;
        if (GetDisplayRank(plantName, node.id) >= node.maxRank) return false;
        if (!IsStepUnlockedDisplay(tree, plantName, stepIndex)) return false;
        if (IsExclusiveLockedDisplay(tree.steps[stepIndex], plantName, node)) return false;
        return DisplaySkillPoints >= node.costPerRank;
    }

    public static bool TryStage(SkillTreeData tree, string plantName, int stepIndex, SkillTreeNode node)
    {
        if (!CanStage(tree, plantName, stepIndex, node)) return false;
        pending.Add(new Pending { plantName = plantName, nodeId = node.id, cost = node.costPerRank });
        SkillTreeUI.instance?.RefreshAll();
        return true;
    }

    // only ever un-does a staged (not yet confirmed) pick - a confirmed purchase is permanent
    // short of a full tree reset, by design
    public static bool TryUnstage(string plantName, string nodeId)
    {
        for (int i = 0; i < pending.Count; i++)
        {
            if (pending[i].plantName != plantName || pending[i].nodeId != nodeId) continue;
            pending.RemoveAt(i);
            SkillTreeUI.instance?.RefreshAll();
            return true;
        }
        return false;
    }

    public static bool HasPending => pending.Count > 0;

    public static void ConfirmAll()
    {
        if (Data == null) return;
        foreach (Pending p in pending)
        {
            Data.skillPoints -= p.cost;
            Data.SetSkillRank(p.plantName, p.nodeId, 1);
        }
        pending.Clear();
        SaveManager.instance.Save();
        SkillTreeUI.instance?.RefreshAll();
    }

    // clears any staged picks too - a full reset invalidates in-progress decisions along with
    // everything already confirmed
    public static void ResetAll()
    {
        pending.Clear();
        SaveManager.instance?.ResetSkillTrees();
    }
}
