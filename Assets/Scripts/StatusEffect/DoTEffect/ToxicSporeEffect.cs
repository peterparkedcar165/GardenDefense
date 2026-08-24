// PoisonShroom's attack: deals no direct damage on hit, instead applying this DoT.
// each tick deals the source's Attack Damage at the time of application (so extending the
// duration is a straight buff, never a nerf). source-stackable, so multiple PoisonShrooms
// can each have their own instance active on the same target at once. not tagged
// ElementalDebuff, so its own ticks can still roll to inflict Poisoned like any other Poison damage
public class ToxicSporeEffect : DoTEffect
{
    private static readonly DamageTag[] tickTags = { DamageTag.DoT };

    private readonly float damagePerTick;
    private bool isContinuation;

    public ToxicSporeEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Poison;
        tickInterval = 1f;
        sourceStackable = true;
        damagePerTick = source?.attackDamage ?? 0f;
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
        if (!isContinuation) DealTick();
    }

    public override void OnTick(float deltaTime)
    {
        tickTimer += deltaTime;
        if (tickTimer < tickInterval) return;
        tickTimer -= tickInterval;
        DealTick();
    }

    private void DealTick()
    {
        if (source != null)
            target.Damage(damagePerTick, DamageType.Magic, ElementalType.Poison, source, source.DotCanCrit || source.ElementalReactionCanCrit, tickTags);
        else
            target.Damage(damagePerTick, DamageType.Magic, ElementalType.Poison, tickTags);
    }

    public override string GetName() => "<color=purple>Toxic Spore</color>";
    public override string GetDescription() =>
        $"Deals <color=green><b>{damagePerTick:F0}</b></color> <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage on application and per second after.";
}
