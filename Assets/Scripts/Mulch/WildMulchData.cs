using UnityEngine;
using System.Collections.Generic;
using System.Text;

// a mulch of random stats, from the old fertilizer stat pool (FertilizerStatRules: stats every
// plant can use, plus ones the loadout's elements and damage types unlock). rolls its stats when
// it drops (Tommy, 2026-11-20: the old stat fertilizers became this). any two wild mulches of the
// same rarity merge into one of the next rarity that keeps both stat sets, each upgraded to the
// new rarity (Tommy, 2026-11-21)
[CreateAssetMenu(fileName = "WildMulch", menuName = "Scriptable Objects/Mulch/Wild Mulch")]
public class WildMulchData : MulchData
{
    [Tooltip("how many different stats it rolls")]
    public TierValue statCount = new TierValue(1f, 2f, 4f);
    [Tooltip("each stat's value: the old fertilizer range for that stat, times this, times the rarity multiplier below (Tommy, 2026-11-21: halved from 3)")]
    public float valueScale = 1.5f;
    public TierValue rarityMultiplier = new TierValue(1f, 2f, 4f);

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

    // both copies' stats together, each scaled from its rarity's multiplier to the next one's, so a
    // stat ends up as strong as if it had been rolled at the new rarity. a stat both copies have is
    // kept once, the stronger of the two, then scaled (proposal)
    public override MulchItem Merge(MulchItem a, MulchItem b)
    {
        MulchTier next = NextRarity(a.rarity);
        float from = rarityMultiplier.Get(a.rarity);
        float ratio = from > 0f ? rarityMultiplier.Get(next) / from : 1f;

        List<WildStat> merged = new List<WildStat>();
        foreach (MulchItem copy in new[] { a, b })
            foreach (WildStat stat in copy.stats)
            {
                int at = merged.FindIndex(s => s.statType == stat.statType);
                if (at < 0) merged.Add(stat);
                else if (Mathf.Abs(stat.value) > Mathf.Abs(merged[at].value)) merged[at] = stat;
            }
        for (int i = 0; i < merged.Count; i++)
        {
            WildStat stat = merged[i];
            stat.value *= ratio;
            if (FertilizerStatRules.IsIntegerStat(stat.statType)) stat.value = Mathf.Round(stat.value);
            merged[i] = stat;
        }
        return new MulchItem(this, next, merged);
    }

    // one plain line per stat: "Increase STAT by X." (Tommy, 2026-11-21)
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
            string verb = stat.value >= 0f ? "Increase" : "Reduce";
            string amount = FertilizerFormat.FormatValue(stat.statType, Mathf.Abs(stat.value)).TrimStart('+');
            if (sb.Length > 0) sb.Append('\n');
            sb.Append($"{verb} {Kw(FertilizerFormat.FormatStatName(stat.statType), FertilizerFormat.GetStatColor(stat.statType))} by " +
                $"{(good ? Green(amount) : Red(amount))}.");
        }
        return sb.ToString();
    }
}
