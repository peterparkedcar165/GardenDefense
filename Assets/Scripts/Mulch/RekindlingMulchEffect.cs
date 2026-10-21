// shrinks every Burn's base duration board-wide via MulchManager.BurnDurationMultiplier (same
// static-multiplier convention as PrimerCooldownMulchEffect/SlowBonusMulchEffect - read directly
// by BurnEffect's own constructor, so it covers every Burn applied after this mulch is picked,
// no per-instance hook needed). separately, listens for direct Grass damage landing on an
// already-Burning target and feeds that Burn's remaining duration back up - DoT ticks (the Burn's
// own damage included) and reaction procs (Germinate, etc.) are both tagged and explicitly
// excluded, since those aren't a plant's own direct hit
public class RekindlingMulchEffect : MulchEffect
{
    private readonly RekindlingMulchData typedData;

    public RekindlingMulchEffect(RekindlingMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        MulchManager.BurnDurationMultiplier *= (1f - typedData.durationReductionPercent);
        Entity.OnEntityHit += HandleHit;
    }

    public override void Deactivate()
    {
        MulchManager.BurnDurationMultiplier /= (1f - typedData.durationReductionPercent);
        Entity.OnEntityHit -= HandleHit;
    }

    private void HandleHit(EntityEventData hit)
    {
        // plant-only, same as the duration shrink above - an insect dealing Grass damage (if
        // that's ever a thing) should never feed this back up either
        if (hit.source is not Plant) return;
        if (hit.elementalType != ElementalType.Grass) return;
        if (HasTag(hit.tags, DamageTag.DoT) || HasTag(hit.tags, DamageTag.ElementalDebuff)) return;

        BurnEffect burn = hit.target?.GetEffect<BurnEffect>();
        if (burn == null) return;

        burn.duration += typedData.extendPerHit;
    }

    private static bool HasTag(DamageTag[] tags, DamageTag tag)
    {
        if (tags == null) return false;
        foreach (DamageTag t in tags)
            if (t == tag) return true;
        return false;
    }
}
