using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int highestLevelUnlocked = 0;
    public List<string> unlockedPlants = new List<string>();
    public int currency = 0;

    // 0 = locked, 1 = unlocked (25 sun), 2 = 20 sun, 3 = 15 sun, 4 = 10 sun
    public int flowerPotLevel = 0;
    public int waterPotLevel  = 0;

    // plant slots unlock through level progression, not purchase: +1 slot (past the base 4) for
    // every 5 levels unlocked - level 6 unlocked (highestLevelUnlocked >= 5) grants slot 5, level
    // 11 (>= 10) grants slot 6, level 16 (>= 15) grants slot 7, level 21 (>= 20) grants slot 8,
    // capped there for now
    public int MaxLoadoutSize => 4 + Mathf.Clamp(highestLevelUnlocked / 5, 0, 4);

    // skill tree meta progression - skill points are earned per plant (see PlantExpRecord),
    // not from a global pool, so using a plant more is what lets its own tree get built out
    public List<SkillNodePurchase> skillPurchases = new List<SkillNodePurchase>();

    public int GetSkillRank(string plantName, string nodeId)
    {
        foreach (SkillNodePurchase p in skillPurchases)
            if (p.plantName == plantName && p.nodeId == nodeId) return p.rank;
        return 0;
    }

    public void SetSkillRank(string plantName, string nodeId, int rank)
    {
        foreach (SkillNodePurchase p in skillPurchases)
            if (p.plantName == plantName && p.nodeId == nodeId) { p.rank = rank; return; }
        skillPurchases.Add(new SkillNodePurchase { plantName = plantName, nodeId = nodeId, rank = rank });
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
