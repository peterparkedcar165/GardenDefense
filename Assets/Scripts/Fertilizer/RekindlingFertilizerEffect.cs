// shrinks every burn a plant applies through FertilizerManager.BurnDurationMultiplier (read by
// BurnEffect's constructor). separately, a direct grass hit from a plant on a burning insect feeds
// that burn's remaining duration back up. damage over time ticks (the burn's own included) and
// reaction procs don't count, since they aren't a plant's own direct hit
public class RekindlingFertilizerEffect : FertilizerEffect
{
    private readonly RekindlingFertilizerData typedData;

    public RekindlingFertilizerEffect(RekindlingFertilizerData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        FertilizerManager.BurnDurationMultiplier *= (1f - typedData.durationReductionPercent);
        Entity.OnEntityHit += HandleHit;
    }

    public override void Deactivate()
    {
        FertilizerManager.BurnDurationMultiplier /= (1f - typedData.durationReductionPercent);
        Entity.OnEntityHit -= HandleHit;
    }

    private void HandleHit(EntityEventData hit)
    {
        if (hit.source is not Plant) return;
        if (hit.elementalType != ElementalType.Grass) return;
        if (Entity.HasTag(hit.tags, DamageTag.DoT) || Entity.HasTag(hit.tags, DamageTag.ElementalDebuff)) return;

        BurnEffect burn = hit.target?.GetEffect<BurnEffect>();
        if (burn == null) return;
        burn.duration += typedData.extendPerHit;
    }
}
