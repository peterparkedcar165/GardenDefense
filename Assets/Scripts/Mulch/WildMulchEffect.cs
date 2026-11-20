// the rolled stats onto its plant through PlantStatApplier, the same switch skill trees use, and
// back off again when it stops
public class WildMulchEffect : MulchEffect
{
    private bool applied;

    public WildMulchEffect(WildMulchData data, MulchItem item) : base(item) {}

    public override void Activate()
    {
        if (Owner == null || applied) return;
        applied = true;
        foreach (WildStat stat in Item.stats)
            PlantStatApplier.Apply(Owner, stat.statType, stat.value);
    }

    public override void Deactivate()
    {
        if (Owner == null || !applied) return;
        applied = false;
        foreach (WildStat stat in Item.stats)
            PlantStatApplier.Apply(Owner, stat.statType, -stat.value);
    }
}
