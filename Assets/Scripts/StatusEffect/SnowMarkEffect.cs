using UnityEngine;

// a plain marker debuff with no ticking behavior of its own - applied by Snowdrop's attack
// when a target isn't already carrying one. its only purpose is to be detected: by Snowdrop's
// own next attack (which refreshes it instead of re-dealing the bonus damage) and by her bonded
// partner's attacks (which detonate the same bonus damage and consume it - see
// Snowdrop.HandleBoundPartnerHit). source is always the Snowdrop that applied it
public class SnowMarkEffect : StatusEffect
{
    public SnowMarkEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Ice;
        sourceStackable = false;
    }

    public override string GetName() => "<color=#00FFFF>Snow Mark</color>";
    public override string GetDescription() =>
        "Can be detonated by Snowdrop's bonded partner for bonus <color=#00FFFF>Ice</color> Magic damage equal to a % of Max Health.";

    public override void OnApply() { }
    public override void OnExpire() { }
}
