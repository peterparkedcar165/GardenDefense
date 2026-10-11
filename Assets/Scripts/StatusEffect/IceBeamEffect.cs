using UnityEngine;

// the authoritative source of truth for "is Ice Beam active" (Snowdrop.IceBeamActive just reads
// HasEffect<IceBeamEffect>() off herself) - the generic effect system ages and expires this on
// its own, so the fixed Attack Speed override needs to be un-done the instant that happens,
// not just whenever Snowdrop next happens to check a timer
public class IceBeamEffect : StatusEffect
{
    public IceBeamEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.positive;
        elementalType = ElementalType.Ice;
    }

    public override string GetName() => "<color=#00FFFF>Ice Beam</color>";
    public override string GetDescription() =>
        "Attacks become a piercing Ice Beam with a fixed Attack Speed that cannot be increased or reduced.";

    public override void OnApply() { }
    public override void OnExpire() => (target as Plant)?.UpdateStats();
}
