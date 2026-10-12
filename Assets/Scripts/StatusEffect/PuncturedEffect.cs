using UnityEngine;

// reduces the target's own Attack Damage by 1 per stack (via attackDamageAdder) for as long as
// it holds. stacking is additive: reapplying (from any source) adds onto the existing level
// rather than replacing it, capped at 100 - callers just do
// ApplyEffect(new PuncturedEffect(target, dur, N, src)). the adder math relies on
// Entity.ApplyEffect's generic replace sequence (new.OnReapply -> old.OnExpire -> new.OnApply),
// so OnReapply only needs to combine the stack count - OnExpire/OnApply handle the actual delta
public class PuncturedEffect : StatusEffect
{
    private const int MaxStacks = 10;

    public PuncturedEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Poison;
    }

    public override void OnReapply(StatusEffect previous)
    {
        if (previous is PuncturedEffect old)
            level = Mathf.Min(level + old.level, MaxStacks);
    }

    public override void OnApply() => target.attackDamageAdder -= level;
    public override void OnTick(float deltaTime) { }
    public override void OnExpire() => target.attackDamageAdder += level;

    public override string GetName() => "<color=purple>Punctured</color>";
    public override string GetDescription() =>
        $"Attack Damage reduced by <color=red><b>{level}</b></color>.";
}
