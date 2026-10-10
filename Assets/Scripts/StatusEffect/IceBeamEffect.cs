using UnityEngine;

// purely a visible marker so players can see Snowdrop's Ice Beam is active - the actual
// mechanic (fixed Attack Speed, piercing without falloff) is driven by Snowdrop's own
// _iceBeamTimer/IceBeamActive, not by this effect. self-applied, same duration as the timer
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
    public override void OnExpire() { }
}
