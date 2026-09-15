// static logic for skill tree purchases, unlock rules, and applying bonuses to plants
public static class SkillTreeManager
{
    private static SaveData Data => SaveManager.instance != null ? SaveManager.instance.saveData : null;

    public static int GetRank(string plantName, string nodeId)
    {
        return Data != null ? Data.GetSkillRank(plantName, nodeId) : 0;
    }

    // a step opens once the previous step has enough total ranks bought
    public static bool IsStepUnlocked(SkillTreeData tree, string plantName, int stepIndex)
    {
        if (stepIndex <= 0) return true;
        SkillTreeStep previous = tree.steps[stepIndex - 1];
        int total = 0;
        foreach (SkillTreeNode node in previous.nodes)
            total += GetRank(plantName, node.id);
        return total >= previous.ranksToUnlockNext;
    }

    // a fork node is locked forever once its rival has any ranks
    public static bool IsExclusiveLocked(SkillTreeStep step, string plantName, SkillTreeNode node)
    {
        if (step.nodes.Count < 2) return false;
        foreach (SkillTreeNode other in step.nodes)
            if (other != node && GetRank(plantName, other.id) > 0) return true;
        return false;
    }

    public static bool CanPurchase(SkillTreeData tree, string plantName, int stepIndex, SkillTreeNode node)
    {
        if (Data == null) return false;
        if (!IsStepUnlocked(tree, plantName, stepIndex)) return false;
        if (IsExclusiveLocked(tree.steps[stepIndex], plantName, node)) return false;
        if (GetRank(plantName, node.id) >= node.maxRank) return false;
        return Data.skillPoints >= node.costPerRank;
    }

    public static bool TryPurchase(SkillTreeData tree, string plantName, int stepIndex, SkillTreeNode node)
    {
        if (!CanPurchase(tree, plantName, stepIndex, node)) return false;
        Data.skillPoints -= node.costPerRank;
        Data.SetSkillRank(plantName, node.id, GetRank(plantName, node.id) + 1);
        SaveManager.instance.Save();
        return true;
    }

    // pushes purchased bonuses into the plants stat fields, called from Plant.LoadData before UpdateStats
    public static void ApplyTo(Plant plant)
    {
        if (plant == null || plant.data == null || plant.data.skillTree == null) return;
        if (Data == null) return;
        string plantName = plant.data.plantName;
        foreach (SkillTreeStep step in plant.data.skillTree.steps)
            foreach (SkillTreeNode node in step.nodes)
            {
                int rank = GetRank(plantName, node.id);
                if (rank <= 0) continue;
                foreach (SkillNodeEffect effect in node.effects)
                    PlantStatApplier.Apply(plant, effect.statType, effect.valuePerRank * rank);
            }
    }

    // behavior unlock query for bespoke code, true if a purchased node carries this unlock id
    public static bool HasUnlock(Plant plant, string unlockId)
    {
        if (plant == null || plant.data == null || plant.data.skillTree == null) return false;
        return HasUnlock(plant.data.plantName, plant.data.skillTree, unlockId);
    }

    public static bool HasUnlock(string plantName, SkillTreeData tree, string unlockId)
    {
        if (tree == null) return false;
        foreach (SkillTreeStep step in tree.steps)
            foreach (SkillTreeNode node in step.nodes)
                if (node.unlockId == unlockId && GetRank(plantName, node.id) > 0) return true;
        return false;
    }

    // sums confirmed StatType.SunCostReduction effects for this plant - queried at Tile
    // placement time, before any Plant instance exists, since sun is spent off the prefab's own
    // PlantData.sunCost right before Instantiate
    public static int GetSunCostReduction(string plantName, SkillTreeData tree)
    {
        if (tree == null) return 0;
        int total = 0;
        foreach (SkillTreeStep step in tree.steps)
            foreach (SkillTreeNode node in step.nodes)
            {
                int rank = GetRank(plantName, node.id);
                if (rank <= 0) continue;
                foreach (SkillNodeEffect effect in node.effects)
                    if (effect.statType == StatType.SunCostReduction)
                        total += UnityEngine.Mathf.RoundToInt(effect.valuePerRank * rank);
            }
        return total;
    }

    // the actual sun cost to charge/display for placing a fresh plant of this species right
    // now - only the skill tree's reduction applies here, since a not-yet-placed plant can't
    // have been fertilized. shared by Tile (placement charge) and the shop/loadout UI
    // (PlantButton, PlantSlotButton) so the displayed and charged cost always agree
    public static int GetEffectiveSunCost(PlantData data)
    {
        if (data == null) return 0;
        return UnityEngine.Mathf.Max(0, data.sunCost - GetSunCostReduction(data.plantName, data.skillTree));
    }
}
