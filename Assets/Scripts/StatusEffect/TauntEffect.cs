public class TauntEffect : StatusEffect
{
    public IAttackable taunter;
    public int tauntStrength = 1;

    public TauntEffect(Entity target, float duration, int level, Entity source, IAttackable taunter)
        : base(target, duration, level, source)
    {
        this.taunter = taunter;
        effectType = Type.negative;
    }

    // a weaker (or equal) taunt attempt from a DIFFERENT source is refused rather than overwriting
    // it outright - otherwise two plants periodically re-taunting the same insect on their own
    // independent timers (e.g. two Cactuses) would flip its target back and forth every tick,
    // since Taunt isn't source-stackable. a STRONGER incoming taunt still overrides. the SAME
    // source refreshing its own taunt is always let through regardless of strength
    public override bool TryBlockNegativeEffect(StatusEffect incoming) =>
        incoming is TauntEffect taunt && incoming.source != source && taunt.tauntStrength <= tauntStrength;

    public override string GetName() => "Taunted";
    public override string GetDescription() => "Forced to attack a target.";
}
