// waterlily's stacking soak: a movement slow built on SlowEffect (so the Hinder mulch's global
// slow bonus still applies) with its own stronger per stack values, plus it grounds flying
// insects for as long as it lasts. grounding goes through GroundedEffect so it shares the one
// flight authority every other grounding source already uses, and never shortens a longer
// ground that is already running
public class SoakedEffect : SlowEffect
{
    // 18% at 1 stack, +10% per stack after that
    public const float baseSoakSlowness = 0.18f;
    public const float soakSlownessPerLevel = 0.10f;

    public SoakedEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        slowness = baseSoakSlowness + soakSlownessPerLevel * (level - 1) + MulchManager.GlobalSlowBonus;
    }

    public override string GetName() => "<color=#4FC3F7>Soaked</color>";
    public override string GetDescription() =>
        $"Reduce <color=green><b>Movement Speed</b></color> by <color=green><b>{slowness * 100:F0}%</b></color>. Grounds flying insects.";

    public override void OnApply()
    {
        base.OnApply();

        if (target is not FlyingInsect flyer) return;
        GroundedEffect existing = flyer.GetEffect<GroundedEffect>();
        if (existing != null && existing.duration >= duration) return;

        GroundedEffect grounded = new GroundedEffect(flyer, duration, 1, source);
        flyer.ApplyEffect(grounded);
        // ApplyEffect scales durations by debuff given/received again, keep the ground matched
        // to this soak's own already scaled duration instead
        grounded.duration = duration;
    }
}
