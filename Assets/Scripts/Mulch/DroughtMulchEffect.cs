using System.Collections.Generic;
using UnityEngine;

// two phases on its plant's own sun production (Plant.mulchSunProductionBonus, read by
// Plant.GenerateSun): none during the drought, then the after multiplier for the rest of the level.
// each plant keeps its own drought timer (Tommy, 2026-11-28; it was one timer for the whole level,
// 2026-11-22): it starts when the plant's first drought is equipped and runs once, in game time while
// the plant is alive (MulchManager.Update ticks it); a drought equipped or merged on that plant after its
// timer ran out starts straight at its after multiplier. the timer belongs to the plant's droughts, so a
// plant revived with its mulches carries it on. only does anything on a plant that makes sun (sun sap counts)
public class DroughtMulchEffect : MulchEffect
{
    private static readonly List<DroughtMulchEffect> active = new List<DroughtMulchEffect>();
    private static readonly HashSet<Clock> ticked = new HashSet<Clock>();

    // a plant's drought timer, shared by every drought on that plant
    private class Clock
    {
        public float elapsed;
        public float length;
        public bool over;
    }

    private readonly DroughtMulchData typedData;
    private Clock clock;
    private float applied;
    // the sun its after bonus added once the drought was over
    private float extra;

    public DroughtMulchEffect(DroughtMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    // a new level: no drought has run yet (MulchManager.ResetForLevel)
    public static void ResetClock() => active.Clear();

    // MulchManager.Update, every frame: each plant's timer once, while that plant is alive
    public static void TickClock(float deltaTime)
    {
        if (active.Count == 0) return;
        ticked.Clear();
        List<DroughtMulchEffect> ended = null;
        foreach (DroughtMulchEffect effect in active)
        {
            Clock c = effect.clock;
            if (c == null || c.over || !effect.OwnerAlive || !ticked.Add(c)) continue;
            c.elapsed += deltaTime;
            if (c.elapsed < c.length) continue;
            c.over = true;
            if (ended == null) ended = new List<DroughtMulchEffect>();
            foreach (DroughtMulchEffect other in active)
                if (other.clock == c) ended.Add(other);
        }
        if (ended == null) return;
        foreach (DroughtMulchEffect effect in ended)
            effect.SetMultiplier(effect.V(effect.typedData.afterSunMultiplier));
    }

    public override void Activate()
    {
        if (!active.Contains(this)) active.Add(this);
        // the plant's timer: the one its other droughts already run, or a new one
        if (clock == null && Owner != null)
            foreach (MulchEffect owned in Owner.mulches)
                if (owned is DroughtMulchEffect other && other != this && other.clock != null) { clock = other.clock; break; }
        if (clock == null)
        {
            clock = new Clock { length = typedData.haltDuration };
            clock.over = clock.length <= 0f;
        }
        SetMultiplier(clock.over ? V(typedData.afterSunMultiplier) : typedData.droughtSunMultiplier);
    }

    public override void Deactivate()
    {
        active.Remove(this);
        SetBonus(0f);
    }

    public override string GetStatus()
    {
        if (clock == null) return null;
        if (clock.over) return $"Drought over: {V(typedData.afterSunMultiplier) * 100f:0}% Sun\nExtra Sun made: {extra:0}";
        return $"Drought: {Mathf.Max(0f, clock.length - clock.elapsed):0}s left";
    }

    // GenerateSun banks amount times 1 + every production bonus on the plant; this copy's part is
    // applied over that
    public override void OnSunGenerated(int amount, bool fromKill)
    {
        if (clock == null || !clock.over || Owner == null || amount <= 0 || applied <= 0f) return;
        float factor = 1f + Owner.mulchSunProductionBonus;
        if (factor > 0f) extra += amount * applied / factor;
    }

    // stored as what it adds on top of 1, so it adds up with anything else on the same plant
    private void SetMultiplier(float multiplier) => SetBonus(multiplier - 1f);

    private void SetBonus(float value)
    {
        if (Owner != null) Owner.mulchSunProductionBonus += value - applied;
        applied = Owner != null ? value : 0f;
    }
}
