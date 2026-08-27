using UnityEngine;

// shared base for continuous, radius based plant buffs (Hellebore's Protection, Begonia's
// Blessing, Zinnia's Warmth, Calendula's Light, Snowdrop's Cooling). the source plant
// reapplies these every tick to whatever is currently in range; a short duration that races
// against that reapply interval visibly flickers whenever multiple sources' independent
// timers fall out of sync (each stacked instance expiring and reapplying on its own cadence),
// so instead the buff is permanent and removes itself the instant its source plant is gone
// or out of range, checked every single frame rather than only on the reapply interval.
public abstract class PlantAuraBuffEffect : StatusEffect
{
    protected readonly Plant sourcePlant;
    private readonly float range;

    protected PlantAuraBuffEffect(Entity target, int level, Plant source, float range)
        : base(target, float.MaxValue, level, source)
    {
        sourcePlant = source;
        this.range = range;
    }

    // subclasses that need extra per-tick work (e.g. Snowdrop's Cooling) override this instead of OnTick
    protected virtual void OnAuraTick(float deltaTime) { }

    public override void OnTick(float deltaTime)
    {
        if (sourcePlant == null || !sourcePlant.IsAlive ||
            Vector3.Distance(target.transform.position, sourcePlant.transform.position) > range)
        {
            duration = 0f;
            return;
        }
        OnAuraTick(deltaTime);
    }
}
