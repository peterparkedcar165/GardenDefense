using UnityEngine;

// Heatstroke (the Sunflower's skill tree, Tommy, 2026-11-27): an insect inside her Sunray moves 25% slower. the beam
// puts it on again every tick, so it lasts while the insect is inside and a moment after (Sunray.HeatstrokeLinger).
// one at a time: a new one replaces the old, so overlapping beams never stack it
public class HeatstrokeEffect : StatusEffect
{
    public const float SlowAmount = 0.25f;
    private readonly float appliedAmount;

    public HeatstrokeEffect(Entity target, float duration, Entity source) : base(target, duration, 1, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Fire;
        appliedAmount = SlowAmount + MulchManager.SlowBonusFrom(source);
        // a while inside effect: its length is the beam's, not stretched or cut by duration stats
        durationIsFinal = true;
    }

    public override string GetName() => "<color=orange>Heatstroke</color>";
    public override string GetDescription() => $"Reduce <color=green><b>Movement Speed</b></color> by <color=green><b>{appliedAmount * 100f:F0}%</b></color>.";

    public override void OnApply()
    {
        if (target is Insect insect) insect.movementSpeedMultiplier -= appliedAmount;
    }

    public override void OnExpire()
    {
        if (target is Insect insect) insect.movementSpeedMultiplier += appliedAmount;
    }
}
