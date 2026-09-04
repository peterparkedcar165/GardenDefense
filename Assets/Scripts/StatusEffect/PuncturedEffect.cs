using UnityEngine;

// the actual extra-damage-on-Physical-hit mechanic lives in Entity.Damage() (sourced overload),
// gated on HasEffect<PuncturedEffect>() - this class is just the stack/duration container.
// stacking is additive: reapplying (from any source) adds onto the existing level rather than
// replacing it, capped at 100 - callers just do ApplyEffect(new PuncturedEffect(target, dur, N, src))
public class PuncturedEffect : StatusEffect
{
    private const int MaxStacks = 100;

    public PuncturedEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override void OnReapply(StatusEffect previous)
    {
        if (previous is PuncturedEffect old)
            level = Mathf.Min(level + old.level, MaxStacks);
    }

    public override void OnApply() { }
    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }

    public override string GetName() => "<color=#A0522D>Punctured</color>";
    public override string GetDescription() =>
        $"Taking Physical damage deals an extra <color=green><b>{level}</b></color> Grass damage.";
}
