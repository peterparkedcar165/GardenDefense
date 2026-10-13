using UnityEngine;

// Snow Ward: granted by Holly's passive (the spiraling cold wind around herself) and by her
// Blizzard skill (at 1.5x power) to any plant caught in either. Holly's own job is only ever to
// apply/refresh this effect on nearby plants (see Holly.ApplySnowWard) - everything the ward
// actually does lives here: the shield ramps up over time rather than being granted instantly,
// cools the plant down out of Hot weather, and makes it immune to Sandstorm's penalty and damage
// (see SandstormExposedEffect, which just checks for this effect's presence) for as long as it
// holds.
//
// exclusivity ("only the holly with the largest shield wins, other hollies can't interfere or
// stack") is handled entirely by the caller (Holly.ApplySnowWard / Blizzard), never by this
// class or by Entity.ApplyEffect's generic same-type shield comparison: that generic check
// compares momentary ShieldEffect.amount, which is the wrong basis here since amount deliberately
// starts low and ramps up - whichever holly got to a plant first would always "win" regardless of
// how much stronger a later holly's cap actually is. instead, the caller resolves exclusivity by
// cap (Power) up front and either refreshes this exact instance (same source) or removes it and
// applies a fresh one (a stronger different source taking over) - this class just does the
// ramping/cooling/immunity once it's the one holding the plant
public class SnowWardEffect : ShieldEffect
{
    public float rampPerSecond;
    public float shieldMax;
    public float coolingPerSecond;
    public float tickInterval;
    // Holly's Path2 max level bonus (flat Magic Resistance, e.g. 0.15 = 15%) - applied/removed via
    // OnApply/OnExpire through the dedicated magicResistanceAdder knob (Magic Resistance is
    // otherwise purely derived from Magic Armor, so it has no multiplier/other adder of its own),
    // and adjusted live through RefreshPresence if it changes (e.g. Path2 gets maxed mid-hold)
    public float magicResistanceBonus;

    // the stat used to decide which holly's ward owns a plant when two overlap - the cap/potential
    // of the shield, not its current ramped amount
    public float Power => shieldMax;

    private float _tickTimer;

    public SnowWardEffect(Entity target, float duration, int level, Entity source,
        float rampPerSecond, float shieldMax, float coolingPerSecond, float tickInterval, float magicResistanceBonus)
        : base(target, duration, level, source, 0f)
    {
        this.rampPerSecond = rampPerSecond;
        this.shieldMax = shieldMax;
        this.coolingPerSecond = coolingPerSecond;
        this.tickInterval = Mathf.Max(0.05f, tickInterval);
        this.magicResistanceBonus = magicResistanceBonus;
        elementalType = ElementalType.Ice;
    }

    public override void OnApply()
    {
        base.OnApply();
        target.magicResistanceAdder += magicResistanceBonus;
    }

    public override void OnExpire()
    {
        target.magicResistanceAdder -= magicResistanceBonus;
        base.OnExpire();
    }

    // called by the owning source (same Holly's passive tick, or her own Blizzard) instead of
    // going through ApplyEffect again - keeps this exact instance alive (resetting its duration)
    // and updates its strength to whichever call is currently driving it, without ever resetting
    // the ramped amount back down
    public void RefreshPresence(float newDuration, float newRampPerSecond, float newShieldMax, float newCoolingPerSecond, float newTickInterval, float newMagicResistanceBonus)
    {
        duration = newDuration;
        rampPerSecond = newRampPerSecond;
        shieldMax = newShieldMax;
        coolingPerSecond = newCoolingPerSecond;
        tickInterval = Mathf.Max(0.05f, newTickInterval);
        if (newMagicResistanceBonus != magicResistanceBonus)
        {
            target.magicResistanceAdder += newMagicResistanceBonus - magicResistanceBonus;
            magicResistanceBonus = newMagicResistanceBonus;
        }
    }

    protected override float ShieldCap => shieldMax;

    public override void OnTick(float deltaTime)
    {
        base.OnTick(deltaTime);

        if (target is Plant plant)
            plant.temperature = Mathf.Max(plant.temperature - coolingPerSecond * deltaTime, 10f);

        _tickTimer += deltaTime;
        if (_tickTimer < tickInterval) return;
        _tickTimer -= tickInterval;

        amount = Mathf.Min(amount + rampPerSecond * tickInterval, shieldMax);
    }

    protected override string GetShieldDetails() =>
        $"Shielded, cooled by <color=#00FFFF><b>{coolingPerSecond:F1}</b></color>/s, and immune to <color=#E8D9A8><b>Sandstorm</b></color>." +
        (magicResistanceBonus > 0f ? $" Grants <color=#FF69B4><b>+{magicResistanceBonus * 100f:F0}% Magic Resistance</b></color>." : "");

    public override string GetName() => "<color=#00FFFF><b>Snow Ward</b></color>";
}
