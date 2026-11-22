using System.Collections;
using UnityEngine;

// two phases on its plant's own sun production (Plant.mulchSunProductionBonus, read by
// Plant.GenerateSun): none for haltDuration seconds of game time, then the after multiplier for the
// rest of the level. the clock only runs while its plant is alive and keeps its progress through a
// merge. only does anything on a plant that makes sun (sun sap counts)
public class DroughtMulchEffect : MulchEffect
{
    private readonly DroughtMulchData typedData;
    private Coroutine routine;
    private float elapsed;
    private float applied;
    private bool over;
    // the sun its after bonus added once the drought was over
    private float extra;

    public DroughtMulchEffect(DroughtMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
        over = data.haltDuration <= 0f;
    }

    public override void Activate()
    {
        SetMultiplier(over ? V(typedData.afterSunMultiplier) : typedData.droughtSunMultiplier);
        StopOnOwner(ref routine);
        if (!over) routine = StartOnOwner(Run());
    }

    public override void Deactivate()
    {
        StopOnOwner(ref routine);
        SetBonus(0f);
    }

    public override string GetStatus()
    {
        if (over) return $"Drought over: {V(typedData.afterSunMultiplier) * 100f:0}% Sun\nExtra Sun made: {extra:0}";
        return $"Drought: {Mathf.Max(0f, typedData.haltDuration - elapsed):0}s left";
    }

    // GenerateSun banks amount times 1 + every production bonus on the plant; this copy's part is
    // applied over that
    public override void OnSunGenerated(int amount, bool fromKill)
    {
        if (!over || Owner == null || amount <= 0 || applied <= 0f) return;
        float factor = 1f + Owner.mulchSunProductionBonus;
        if (factor > 0f) extra += amount * applied / factor;
    }

    private IEnumerator Run()
    {
        while (!over)
        {
            if (OwnerAlive)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= typedData.haltDuration)
                {
                    over = true;
                    SetMultiplier(V(typedData.afterSunMultiplier));
                }
            }
            yield return null;
        }
        routine = null;
    }

    // stored as what it adds on top of 1, so it adds up with anything else on the same plant
    private void SetMultiplier(float multiplier) => SetBonus(multiplier - 1f);

    private void SetBonus(float value)
    {
        if (Owner != null) Owner.mulchSunProductionBonus += value - applied;
        applied = Owner != null ? value : 0f;
    }
}
