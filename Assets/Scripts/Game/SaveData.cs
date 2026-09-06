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

    // skill tree meta progression, points earned from level clears
    public int skillPoints = 0;
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
}

[System.Serializable]
public class SkillNodePurchase
{
    public string plantName;
    public string nodeId;
    public int rank;
}
