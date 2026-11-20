using UnityEngine;
using System.Collections.Generic;
using System.Text;

// a mulch of random stats, from the old fertilizer stat pool (FertilizerStatRules: stats every
// plant can use, plus ones the loadout's elements and damage types unlock). rolls its stats when
// it drops; any two wild mulches of the same rarity merge into one of the next rarity with fresh
// rolls (Tommy, 2026-11-20: the old stat fertilizers became this)
[CreateAssetMenu(fileName = "WildMulch", menuName = "Scriptable Objects/Mulch/Wild Mulch")]
public class WildMulchData : MulchData
{
    [Tooltip("how many different stats it rolls")]
    public TierValue statCount = new TierValue(1f, 2f, 3f);
    [Tooltip("each stat's value: the old fertilizer range for that stat, times this, times the rarity multiplier below (proposal)")]
    public float valueScale = 3f;
    public TierValue rarityMultiplier = new TierValue(1f, 2f, 3f);

    public override MulchEffect CreateEffect(MulchItem item) => new WildMulchEffect(this, item);

    public override void OnItemCreated(MulchItem item)
    {
        item.stats.Clear();
        List<StatType> pool = new List<StatType>(FertilizerStatRules.GetAvailableStatsWithScope(FertilizerManager.GetCurrentLoadout()).Keys);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        int count = Mathf.Min(Mathf.Max(1, statCount.GetInt(item.rarity)), pool.Count);
        float multiplier = valueScale * rarityMultiplier.Get(item.rarity);
        for (int i = 0; i < count; i++)
        {
            StatType stat = pool[i];
            (float min, float max) = FertilizerStatRules.BaseRange(stat);
            float value = Random.Range(min, max) * multiplier;
            if (FertilizerStatRules.IsIntegerStat(stat)) value = Mathf.Round(value);
            item.stats.Add(new WildStat { statType = stat, value = value });
        }
    }

    // any two wild mulches of the same rarity, below Epic
    public override bool CanMerge(MulchItem a, MulchItem b) =>
        a != null && b != null && a.data == this && b.data == this && a.rarity == b.rarity && a.rarity < MulchTier.Epic;

    public override string GetDescription(MulchItem item)
    {
        StringBuilder sb = new StringBuilder();
        StatType[] types = new StatType[item.stats.Count];
        for (int i = 0; i < types.Length; i++) types[i] = item.stats[i].statType;
        foreach (int i in FertilizerFormat.SortedIndicesByName(types))
        {
            WildStat stat = item.stats[i];
            bool good = stat.value >= 0f;
            if (FertilizerFormat.IsInvertedStat(stat.statType)) good = !good;
            sb.AppendLine($"<color={FertilizerFormat.GetStatColor(stat.statType)}><b>{FertilizerFormat.FormatStatName(stat.statType)}</b></color>: " +
                $"<color={(good ? "green" : "red")}><b>{FertilizerFormat.FormatValue(stat.statType, stat.value)}</b></color>");
        }
        sb.Append("<size=85%><color=#AAAAAA>Merges with any Wild Mulch of the same rarity, rolling new stats.</color></size>");
        return sb.ToString();
    }
}
