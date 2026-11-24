using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int highestLevelUnlocked = 0;
    public List<string> unlockedPlants = new List<string>();
    public int currency = 0;

    // bumped by SaveManager.MigrateExpCurve whenever PlantLevelCurve's growth rate changes, so an
    // already-saved plant's (level, totalExp) can be reconstructed and re-leveled under the new
    // curve exactly once - 0 means still on the original GrowthRate 1.5 curve
    public int expCurveVersion = 0;

    // 0 = locked, 1 = unlocked (25 sun), 2 = 20 sun, 3 = 15 sun, 4 = 10 sun.
    // unlike waterPotLevel (purchased from level 0 in the Shop), flowerPotLevel's 0->1 step is
    // free and automatic the moment Level 11 is unlocked (see SaveManager.RepairFlowerPotFromLevel)
    // - only the 1->2->3->4 sun-cost upgrades beyond that are purchased
    public int flowerPotLevel = 0;
    public int waterPotLevel  = 0;

    // Level 11 unlocked == highestLevelUnlocked >= 10 (same off-by-one convention as
    // LevelSelector/MaxLoadoutSize above)
    public const int FlowerPotRequiredHighestLevelUnlocked = 10;

    // tutorial popup ids already shown to this save - each one only ever shows once. ids follow a
    // "level{N}_{moment}" convention (e.g. "level1_plantselection") so they stay easy to tell
    // apart as more get added - see TutorialIds
    public List<string> seenTutorials = new List<string>();

    // plant slots unlock through level progression, not purchase: +1 slot (past the base 4) for
    // every 5 levels unlocked - level 6 unlocked (highestLevelUnlocked >= 5) grants slot 5, level
    // 11 (>= 10) grants slot 6, level 16 (>= 15) grants slot 7, level 21 (>= 20) grants slot 8,
    // capped there for now
    public int MaxLoadoutSize => 4 + Mathf.Clamp(highestLevelUnlocked / 5, 0, 4);

    // every plant slot there is, unlocked or not: the loadout screen and the plant bar draw this many
    // squares (Tommy, 2026-11-22)
    public const int MaxLoadoutSlots = 8;

    // the level to beat to open a slot (slot counted from 0): beating level 5 unlocks level 6, which
    // opens slot 5, and so on every 5 levels. 0 for the 4 slots every save starts with
    public static int LevelToBeatForSlot(int slot) => slot < 4 ? 0 : (slot - 3) * 5;

    public static string SlotUnlockHint(int slot) =>
        $"<b>Locked plant slot</b>\nBeat Level {LevelToBeatForSlot(slot)} to unlock it.";

    // the plant bar's hover on an open slot no plant was picked for this level
    public const string EmptySlotHint = "<b>Empty plant slot</b>\nNo plant was picked for it this level.";

    // skill tree meta progression - skill points are earned per plant (see PlantExpRecord),
    // not from a global pool, so using a plant more is what lets its own tree get built out
    public List<SkillNodePurchase> skillPurchases = new List<SkillNodePurchase>();

    // runtime lookup over skillPurchases (plant name, then node id, to the first matching entry),
    // so a rank lookup is not a walk with string compares every time. not saved. built again
    // whenever the list's count no longer matches, which catches entries added or removed
    // straight on the list (SaveManager's tree reset removes them that way)
    [System.NonSerialized] private Dictionary<string, Dictionary<string, SkillNodePurchase>> purchaseLookup;
    [System.NonSerialized] private List<SkillNodePurchase> purchaseLookupList;
    [System.NonSerialized] private int purchaseLookupCount = -1;

    private Dictionary<string, Dictionary<string, SkillNodePurchase>> PurchaseLookup
    {
        get
        {
            if (purchaseLookup == null || purchaseLookupList != skillPurchases || purchaseLookupCount != skillPurchases.Count)
            {
                if (purchaseLookup == null) purchaseLookup = new Dictionary<string, Dictionary<string, SkillNodePurchase>>();
                else purchaseLookup.Clear();
                foreach (SkillNodePurchase p in skillPurchases)
                {
                    if (p == null || p.plantName == null || p.nodeId == null) continue;
                    if (!purchaseLookup.TryGetValue(p.plantName, out var nodes))
                        purchaseLookup[p.plantName] = nodes = new Dictionary<string, SkillNodePurchase>();
                    // the first matching entry wins, same as the old walk
                    if (!nodes.ContainsKey(p.nodeId)) nodes[p.nodeId] = p;
                }
                purchaseLookupList = skillPurchases;
                purchaseLookupCount = skillPurchases.Count;
            }
            return purchaseLookup;
        }
    }

    private SkillNodePurchase FindPurchase(string plantName, string nodeId)
    {
        if (skillPurchases == null || plantName == null || nodeId == null) return null;
        return PurchaseLookup.TryGetValue(plantName, out var nodes) && nodes.TryGetValue(nodeId, out SkillNodePurchase p) ? p : null;
    }

    public int GetSkillRank(string plantName, string nodeId)
    {
        SkillNodePurchase p = FindPurchase(plantName, nodeId);
        return p != null ? p.rank : 0;
    }

    public void SetSkillRank(string plantName, string nodeId, int rank)
    {
        SkillNodePurchase existing = FindPurchase(plantName, nodeId);
        if (existing != null) { existing.rank = rank; return; }
        skillPurchases.Add(new SkillNodePurchase { plantName = plantName, nodeId = nodeId, rank = rank });
        // the count check above builds the lookup again with this entry on the next call
    }

    // persistent per-species exp/level/skill points, banked from a plant's own (session-scoped)
    // exp whenever it dies, is uprooted, or survives to a level's completion - shown on the
    // Skill Tree screen. level and skillPoints are this plant species's OWN meta progression -
    // there is no global skill point pool, so a plant only grows its tree by actually being used
    public List<PlantExpRecord> plantExp = new List<PlantExpRecord>();

    private PlantExpRecord GetOrCreatePlantRecord(string plantName)
    {
        foreach (PlantExpRecord r in plantExp)
            if (r.plantName == plantName) return r;
        PlantExpRecord created = new PlantExpRecord { plantName = plantName };
        plantExp.Add(created);
        return created;
    }

    public int GetPlantExp(string plantName)
    {
        foreach (PlantExpRecord r in plantExp)
            if (r.plantName == plantName) return r.totalExp;
        return 0;
    }

    public void AddPlantExp(string plantName, int amount)
    {
        if (amount <= 0) return;
        GetOrCreatePlantRecord(plantName).totalExp += amount;
    }

    public int GetPlantLevel(string plantName)
    {
        foreach (PlantExpRecord r in plantExp)
            if (r.plantName == plantName) return r.level;
        return 1;
    }

    public int GetPlantSkillPoints(string plantName)
    {
        foreach (PlantExpRecord r in plantExp)
            if (r.plantName == plantName) return r.skillPoints;
        return 0;
    }

    // amount may be negative (spending); never lets a plant's balance go below 0
    public void AddPlantSkillPoints(string plantName, int amount)
    {
        PlantExpRecord record = GetOrCreatePlantRecord(plantName);
        record.skillPoints = Mathf.Max(0, record.skillPoints + amount);
    }

}

[System.Serializable]
public class SkillNodePurchase
{
    public string plantName;
    public string nodeId;
    public int rank;
}

[System.Serializable]
public class PlantExpRecord
{
    public string plantName;
    public int totalExp;
    public int level = 1;
    public int skillPoints = 0;
}
