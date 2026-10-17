using UnityEngine;

// a plain marker debuff with no ticking behavior of its own - applied by Snowdrop's attack
// when a target isn't already carrying one. its only purpose is to be detected: by Snowdrop's
// own next attack (which refreshes it instead of re-dealing the bonus damage) and by her bonded
// partner's attacks (which detonate the same bonus damage and consume it - see
// Snowdrop.HandleBoundPartnerHit). source is always the Snowdrop that applied it
public class SnowMarkEffect : StatusEffect
{
    private const float ChillingGraspSlowPercent = 0.25f;
    private bool _slowApplied;
    private float _appliedAmount;

    public SnowMarkEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Ice;
        sourceStackable = false;
    }

    public override string GetName() => "<color=#00FFFF>Snow Mark</color>";
    public override string GetDescription() =>
        "Can be detonated by Snowdrop's bonded partner for bonus <color=#00FFFF>Ice</color> Magic damage equal to a % of Max Health.";

    // Chilling Grasp (skill tree): checked once on apply rather than every tick, since the
    // unlock can't change mid-mark - re-checked fresh on every refresh/reapply too, since OnApply
    // only ever fires on a genuinely new instance (the old one's OnExpire runs first on refresh)
    public override void OnApply()
    {
        if (source is Snowdrop snowdrop && target is Insect insect && SkillTreeManager.HasUnlock(snowdrop, Snowdrop.ChillingGraspUnlock))
        {
            _slowApplied = true;
            _appliedAmount = ChillingGraspSlowPercent + MulchManager.GlobalSlowBonus;
            insect.movementSpeedMultiplier -= _appliedAmount;
        }
    }

    public override void OnExpire()
    {
        if (_slowApplied && target is Insect insect)
            insect.movementSpeedMultiplier += _appliedAmount;
    }
}
