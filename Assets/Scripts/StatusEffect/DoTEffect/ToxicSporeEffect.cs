// PoisonShroom's attack: deals no direct damage on hit, instead applying this DoT.
// per-tick damage is Attack Damage scaled by the tick interval, so total damage per second is
// tickInterval-independent (extending the duration is a straight buff, never a nerf, and the
// tick rate can change without changing overall damage output). source-stackable, so multiple
// PoisonShrooms can each have their own instance active on the same target at once. not tagged
// ElementalDebuff, so its own ticks can still roll to inflict Poisoned like any other Poison damage
public class ToxicSporeEffect : DoTEffect
{
    private static readonly DamageTag[] tickTags = { DamageTag.DoT };

    private readonly float damagePerTick;
    private bool isContinuation;
    private const float internalCooldownReductionPerTick = 1f;
    private readonly bool toxicCatalystActive;
    // fraction of the target's CURRENT health dealt as bonus damage per second while poisoned
    private readonly float percentHealthDPS;

    public ToxicSporeEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source, 1f)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Poison;
        sourceStackable = true;
        damagePerTick = (source?.attackDamage ?? 0f) * ticks.baseInterval;
        toxicCatalystActive = source is PoisonShroom ps && SkillTreeManager.HasUnlock(ps, PoisonShroom.ToxicCatalystUnlock);
        percentHealthDPS = (source as PoisonShroom)?.PercentHealthDPS ?? 0f;
    }

    // called (on this new instance) only when the same source already had one running on this
    // target, right before the old instance expires and this one takes over
    public override void OnReapply(StatusEffect previous)
    {
        base.OnReapply(previous);
        isContinuation = true;
    }

    public override void OnApply()
    {
        base.OnApply();
        if (!isContinuation) DealTick(1f);
    }

    // one tick, or the share of one left at a natural expiry (the shared DoT timing, OverTimeTicker)
    protected override void DealTick(float share)
    {
        // Executioner's Bloom: bonus damage finishing off insects already below 33% health
        // (the health check first, so the unlock is only looked up for a target that low)
        bool execute = target.health / target.maxHealth < 0.33f
                       && source is PoisonShroom ps && SkillTreeManager.HasUnlock(ps, PoisonShroom.ExecutionersBloomUnlock);

        // percent-health damage reads the target's CURRENT health at each tick, so it naturally
        // tapers off as the target dies rather than staying fixed like the flat portion
        float percentDamage = target.health * percentHealthDPS * ticks.baseInterval;
        float baseDamage = damagePerTick + percentDamage;
        float finalDamage = (execute ? baseDamage * 1.66f : baseDamage) * share;

        if (source != null)
            target.Damage(finalDamage, DamageType.Magic, ElementalType.Poison, source, source.DotCanCrit || source.ElementalReactionCanCrit, tickTags);
        else
            target.Damage(finalDamage, DamageType.Magic, ElementalType.Poison, tickTags);

        // Toxic Catalyst: keeps Poison elemental reactions coming faster on this target
        if (toxicCatalystActive)
        {
            target.poisonInternalCooldown -= internalCooldownReductionPerTick * share;
            if (target.poisonInternalCooldown < 0f) target.poisonInternalCooldown = 0f;
        }
    }

    public override string GetName() => "<color=purple>Toxic Spore</color>";
    public override string GetDescription() =>
        $"Deals <color=green><b>{damagePerTick:F0}</b></color> <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage, plus <color=green><b>{percentHealthDPS * 100f:F1}%</b></color> of current health, on application and every <color=green><b>{TickInterval:F1}s</b></color> after.";
}
