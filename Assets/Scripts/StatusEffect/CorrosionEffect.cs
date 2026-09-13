using UnityEngine;

// Fire+Poison primer combo reaction: corrosive armor decay. every second, armor is reduced by
// 1 * (1 + elementalAffinity), for as long as the effect is active (base duration 12s).
//
// deliberately a single instance per target, not source-stackable and not IElementalAffinityEffect
// (that interface would let the higher-affinity instance always win and discard a newer, weaker
// one - the opposite of what this needs): any reapplication, regardless of affinity, always takes
// over as the active instance and keeps ticking at ITS OWN rate going forward. the accumulated
// total is carried across via OnReapply so the existing armor reduction is preserved rather than
// reset - the old instance's OnExpire reverts its own share, then the new instance's OnApply
// immediately re-applies the inherited total before continuing to tick from there. tracked as a
// double (not the armorAdder float directly) so many small fractional ticks over a long duration
// don't drift/round away, and the exact accumulated amount can always be restored precisely
public class CorrosionEffect : StatusEffect
{
    private const float TickInterval = 1f;
    private float tickTimer;
    private double totalArmorReduction;
    private float cachedelementalAffinity;

    public CorrosionEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        cachedelementalAffinity = source?.elementalAffinity ?? 0f;
        effectType = Type.negative;
        elementalType = ElementalType.Fire;
    }

    private float RatePerSecond => 1f * (1f + cachedelementalAffinity);

    public override string GetName() => "<color=#B22222>Corrosion</color>";
    public override string GetDescription() =>
        $"Reduces Armor by <color=green><b>{RatePerSecond:F1}</b></color> every second.";

    public override void OnReapply(StatusEffect previous)
    {
        if (previous is CorrosionEffect prev)
        {
            totalArmorReduction = prev.totalArmorReduction;
            tickTimer = prev.tickTimer;
        }
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Corrosion", new Color(0.7f, 0.3f, 0.1f));

        // restores whatever total was inherited from a previous instance (see OnReapply) - the old
        // instance's own OnExpire already reverted its share right before this ran
        if (totalArmorReduction != 0d)
            target.armorAdder -= (float)totalArmorReduction;
    }

    public override void OnTick(float deltaTime)
    {
        tickTimer += deltaTime;
        if (tickTimer < TickInterval) return;
        tickTimer -= TickInterval;

        double increment = RatePerSecond;
        totalArmorReduction += increment;
        target.armorAdder -= (float)increment;
    }

    public override void OnExpire()
    {
        target.armorAdder += (float)totalArmorReduction;
    }
}
