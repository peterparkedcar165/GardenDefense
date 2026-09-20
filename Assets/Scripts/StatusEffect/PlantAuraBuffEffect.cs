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
    // when set, range is measured from this transform instead of sourcePlant (e.g. Calendula's
    // Borrowed Light re-centers the aura on a Floral Glow target rather than Calendula herself)
    private readonly Transform centerOverride;

    protected PlantAuraBuffEffect(Entity target, int level, Plant source, float range, Transform centerOverride = null)
        : base(target, float.MaxValue, level, source)
    {
        sourcePlant = source;
        this.range = range;
        this.centerOverride = centerOverride;
    }

    // subclasses that need extra per-tick work (e.g. Snowdrop's Cooling) override this instead of OnTick
    protected virtual void OnAuraTick(float deltaTime) { }

    public override void OnTick(float deltaTime)
    {
        if (sourcePlant == null || !sourcePlant.IsAlive)
        {
            duration = 0f;
            return;
        }
        Vector3 center = centerOverride != null ? centerOverride.position : sourcePlant.transform.position;
        if (Vector3.Distance(target.transform.position, center) > range)
        {
            duration = 0f;
            return;
        }
        OnAuraTick(deltaTime);
    }
}
