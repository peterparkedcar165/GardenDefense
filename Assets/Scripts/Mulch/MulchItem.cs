using System.Collections.Generic;

// one copy of a mulch at a rarity: a ground drop, an inventory slot and an equipped mulch all hold
// one. a wild mulch's copy also carries the stats it rolled
public class MulchItem
{
    public readonly MulchData data;
    public readonly MulchTier rarity;
    // wild mulch only: the stats this copy rolled (WildMulchData.OnItemCreated)
    public readonly List<WildStat> stats = new List<WildStat>();

    public MulchItem(MulchData data, MulchTier rarity)
    {
        this.data = data;
        this.rarity = rarity;
        data?.OnItemCreated(this);
    }

    // a copy that comes with its stats already set (a wild mulch merge), so nothing is rolled
    public MulchItem(MulchData data, MulchTier rarity, List<WildStat> presetStats)
    {
        this.data = data;
        this.rarity = rarity;
        if (presetStats != null) stats.AddRange(presetStats);
    }

    public string Name => data != null ? data.mulchName : "?";

    public bool CanMergeWith(MulchItem other) => data != null && other != null && data.CanMerge(this, other);

    // the copy this and another mergeable copy become (MulchData.Merge)
    public MulchItem Merged(MulchItem other) => data.Merge(this, other);
}

// one stat a wild mulch rolled
public struct WildStat
{
    public StatType statType;
    public float value;
}
