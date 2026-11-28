using UnityEngine;

// shared base for continuous, radius based plant buffs (Hellebore's Protection, Begonia's
// Blessing, Zinnia's Warmth, Calendula's Light). the source plant
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

    // a light's aura (Calendula's and Tansy's): it ends where Collider cells or cliffs block the light (Tommy, 2026-11-28)
    protected virtual bool IsLight => false;

    // subclasses that need extra per-tick work override this instead of OnTick
    protected virtual void OnAuraTick(float deltaTime) { }

    public override void OnTick(float deltaTime)
    {
        if (sourcePlant == null || !sourcePlant.IsAlive)
        {
            duration = 0f;
            return;
        }
        Vector3 center = centerOverride != null ? centerOverride.position : sourcePlant.transform.position;
        // squared distances: the same test without a square root every frame
        if ((target.transform.position - center).sqrMagnitude > range * range)
        {
            duration = 0f;
            return;
        }
        if (IsLight)
        {
            Plant lightFrom = centerOverride != null ? centerOverride.GetComponent<Plant>() : sourcePlant;
            if (lightFrom != null && !lightFrom.LightReaches(target.transform.position))
            {
                duration = 0f;
                return;
            }
        }
        OnAuraTick(deltaTime);
    }
}
