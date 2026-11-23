using UnityEngine;

// the shared timing of every damage over time and healing over time effect (Tommy, 2026-11-23).
// one tick deals (or heals) the amount per second times the base interval, fixed when the effect
// lands; a part read from the target's health (Poisoned's missing health) stays live, tick by tick.
// a tick speed bonus shortens the interval to base / (1 + bonus): each tick is just as big, there
// are more of them in the same duration, so the total goes up by that factor (Inflammable). when
// the effect runs out on its own (StatusEffect.OnNaturalExpire), the part of a tick it was partway
// through is paid too, so a duration that isn't a whole number of ticks never loses its last bit.
// an effect cleansed, used up or replaced early pays nothing more. in all, the total is amount per
// second x final duration x (1 + bonus), and seconds added later (Sludge, a refresh) add their share
public class OverTimeTicker
{
    public readonly float baseInterval;
    public float tickSpeedBonus;
    // seconds since the last tick
    public float timer;

    public OverTimeTicker(float baseInterval)
    {
        this.baseInterval = Mathf.Max(0.01f, baseInterval);
    }

    public float Interval => baseInterval / Mathf.Max(0.01f, 1f + tickSpeedBonus);

    // from OnTick: how many whole ticks are due after deltaTime more seconds
    public int Advance(float deltaTime)
    {
        timer += deltaTime;
        float interval = Interval;
        int due = 0;
        while (timer >= interval)
        {
            timer -= interval;
            due++;
        }
        return due;
    }

    // from OnNaturalExpire: the share of one tick still owed. the frame it ran out in can take it
    // past its duration (it ends below zero), and that overshoot was never lived
    public float LeftoverShare(float durationLeft) =>
        Mathf.Clamp01((timer + Mathf.Min(0f, durationLeft)) / Interval);

    // a reapplied effect keeps the old one's place in the tick (OnReapply)
    public void CarryFrom(OverTimeTicker previous)
    {
        if (previous != null) timer = previous.timer;
    }
}
