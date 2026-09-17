// a mid-level fertilizer built at runtime (not a hand-authored FertilizerData asset) - its stats
// are rolled from whatever the current loadout's dynamic pool allows (see FertilizerStatRules),
// each carrying its own applicability scope since different stats in the same bundle can come
// from different unlock reasons (one generic, one elemental, one family, etc.)
public class GeneratedFertilizer
{
    public FertilizerTier tier;
    public GeneratedFertilizerStat[] stats;
}

public struct GeneratedFertilizerStat
{
    public StatType statType;
    public float value;
    public FertilizerStatRules.StatScope scope;
}
