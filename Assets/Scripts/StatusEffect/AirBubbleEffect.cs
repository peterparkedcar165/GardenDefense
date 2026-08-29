using UnityEngine;

// granted by Kelp's skill projectile. instantly tops off Oxygen on apply, then implements
// IRespirationEffect so Plant.UpdateAir() regenerates Oxygen at regenPerSecond for as long as
// this is active (only actually matters while the target is also Submerged, same as any other
// Respiration source). at Kelp's Path3 max, it also grants the target full bonus Sun Yield.
public class AirBubbleEffect : StatusEffect, IRespirationEffect
{
    private static GameObject _bubbleVisualPrefab;
    private GameObject _bubbleVisual;

    private readonly float initialOxygen;
    private readonly float regenPerSecond;
    private readonly bool grantsFullSunYield;

    public float RespirationRegenPerSecond => regenPerSecond;

    public AirBubbleEffect(Entity target, float duration, Entity source, float initialOxygen, float regenPerSecond, bool grantsFullSunYield)
        : base(target, duration, 1, source)
    {
        this.initialOxygen      = initialOxygen;
        this.regenPerSecond     = regenPerSecond;
        this.grantsFullSunYield = grantsFullSunYield;
        effectType    = Type.positive;
        elementalType = ElementalType.Water;
    }

    // credits the Kelp that fired this bubble for Oxygen it grants, same as her attack passive,
    // so the skill also feeds her cumulative-Oxygen Sun tracker
    public void OnOxygenGranted(float amount)
    {
        if (source is Kelp kelp) kelp.AccumulateOxygen(amount);
    }

    public override void OnApply()
    {
        if (target is Plant plant)
        {
            OnOxygenGranted(plant.ReplenishOxygen(initialOxygen));
            if (grantsFullSunYield) plant.sunYieldMultiplier += 1f;
        }

        if (_bubbleVisualPrefab == null)
            _bubbleVisualPrefab = Resources.Load<GameObject>("AirBubbleVisual");
        if (_bubbleVisualPrefab != null && target != null)
        {
            _bubbleVisual = Object.Instantiate(_bubbleVisualPrefab, target.transform);
            _bubbleVisual.transform.localPosition = Vector3.zero;
        }
    }

    public override void OnExpire()
    {
        if (grantsFullSunYield && target is Plant plant)
            plant.sunYieldMultiplier -= 1f;

        if (_bubbleVisual != null) Object.Destroy(_bubbleVisual);
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#4FC3F7><b>Air Bubble</b></color>";
    public override string GetDescription()
    {
        string s = $"Regenerates <color=green><b>{regenPerSecond:F1}</b></color> Oxygen per second.";
        if (grantsFullSunYield) s += "\nGrants <color=yellow><b>100%</b></color> Sun Yield.";
        return s;
    }
}
