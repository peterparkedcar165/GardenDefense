using System.Collections.Generic;
using UnityEngine;

// two phases on its plant's own sun production (Plant.mulchSunProductionBonus, read by
// Plant.GenerateSun): none during the drought, then the after multiplier for the rest of the level.
// one drought timer for the whole level (Tommy, 2026-11-22): it starts when the first drought is
// equipped and runs once, in game time while any plant carrying a drought is alive
// (MulchManager.Update ticks it); every drought equipped, merged or revived after it ran out starts
// straight at its after multiplier. only does anything on a plant that makes sun (sun sap counts)
public class DroughtMulchEffect : MulchEffect
{
    private static readonly List<DroughtMulchEffect> active = new List<DroughtMulchEffect>();
    private static bool clockStarted;
    private static bool clockOver;
    private static float clockElapsed;
    private static float clockLength;

    private readonly DroughtMulchData typedData;
    private float applied;
    // the sun its after bonus added once the drought was over
    private float extra;

    public DroughtMulchEffect(DroughtMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    // a new level: no drought has run yet (MulchManager.ResetForLevel)
    public static void ResetClock()
    {
        active.Clear();
        clockStarted = clockOver = false;
        clockElapsed = clockLength = 0f;
    }

    // MulchManager.Update, every frame
    public static void TickClock(float deltaTime)
    {
        if (!clockStarted || clockOver) return;
        bool anyAlive = false;
        foreach (DroughtMulchEffect effect in active)
            if (effect.OwnerAlive) { anyAlive = true; break; }
        if (!anyAlive) return;
        clockElapsed += deltaTime;
        if (clockElapsed < clockLength) return;
        clockOver = true;
        foreach (DroughtMulchEffect effect in new List<DroughtMulchEffect>(active))
            effect.SetMultiplier(effect.V(effect.typedData.afterSunMultiplier));
    }

    public override void Activate()
    {
        if (!active.Contains(this)) active.Add(this);
        if (!clockStarted)
        {
            clockStarted = true;
            clockLength = typedData.haltDuration;
            clockOver = clockLength <= 0f;
        }
        SetMultiplier(clockOver ? V(typedData.afterSunMultiplier) : typedData.droughtSunMultiplier);
    }

    public override void Deactivate()
    {
        active.Remove(this);
        SetBonus(0f);
    }

    public override string GetStatus()
    {
        if (clockOver) return $"Drought over: {V(typedData.afterSunMultiplier) * 100f:0}% Sun\nExtra Sun made: {extra:0}";
        return $"Drought: {Mathf.Max(0f, clockLength - clockElapsed):0}s left";
    }

    // GenerateSun banks amount times 1 + every production bonus on the plant; this copy's part is
    // applied over that
    public override void OnSunGenerated(int amount, bool fromKill)
    {
        if (!clockOver || Owner == null || amount <= 0 || applied <= 0f) return;
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
