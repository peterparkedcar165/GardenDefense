using System.Collections;
using UnityEngine;

// its plant climbs on its own clock, kept in Plant.mulchCarry so a death and a revival that brings
// the mulch back carries it over (DeadPlantRecord copies it). only a living plant's clock runs. the
// armor itself is an armorAdder delta, checked every frame; the visible status (SeasonedOakEffect) is
// display only and put back if anything strips it
public class SeasonedOakMulchEffect : MulchEffect
{
    private const string GainedKey = "seasonedOak.gained";
    private const string TimerKey = "seasonedOak.timer";

    private readonly SeasonedOakMulchData typedData;
    private float appliedArmor;
    private Coroutine routine;

    public SeasonedOakMulchEffect(SeasonedOakMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        StopOnOwner(ref routine);
        routine = StartOnOwner(Run());
    }

    public override void Deactivate()
    {
        StopOnOwner(ref routine);
        if (Owner != null)
        {
            Owner.armorAdder -= appliedArmor;
            Owner.RemoveEffect<SeasonedOakEffect>();
        }
        appliedArmor = 0f;
    }

    private IEnumerator Run()
    {
        while (true)
        {
            Step(Time.deltaTime);
            yield return null;
        }
    }

    // the armor gained so far is what's kept (Tommy, 2026-11-21): a merge to a higher rarity carries
    // on from it at the new step and up to the new cap, instead of recounting the steps
    private void Step(float deltaTime)
    {
        if (!OwnerAlive) return;
        float maxGain = typedData.MaxGain(Rarity);
        float perStep = typedData.armorPerStep.Get(Rarity);
        float gained = Owner.mulchCarry.TryGetValue(GainedKey, out float savedGain) ? savedGain : 0f;
        if (gained < maxGain && typedData.stepInterval > 0f && perStep > 0f)
        {
            float timer = (Owner.mulchCarry.TryGetValue(TimerKey, out float savedTimer) ? savedTimer : 0f) + deltaTime;
            while (timer >= typedData.stepInterval && gained < maxGain)
            {
                timer -= typedData.stepInterval;
                gained = Mathf.Min(maxGain, gained + perStep);
            }
            Owner.mulchCarry[GainedKey] = gained;
            Owner.mulchCarry[TimerKey] = gained < maxGain ? timer : 0f;
        }

        float wanted = typedData.ArmorWithGain(Rarity, gained);
        if (!Mathf.Approximately(appliedArmor, wanted))
        {
            Owner.armorAdder += wanted - appliedArmor;
            appliedArmor = wanted;
        }
        ShowStatus(wanted);
    }

    private void ShowStatus(float armor)
    {
        SeasonedOakEffect status = Owner.GetEffect<SeasonedOakEffect>();
        if (status == null)
        {
            status = new SeasonedOakEffect(Owner);
            Owner.ApplyEffect(status);
        }
        float timer = Owner.mulchCarry.TryGetValue(TimerKey, out float savedTimer) ? savedTimer : 0f;
        // permanent: ApplyEffect scales a buff's duration by buff duration stats, pinned back here
        status.duration = float.MaxValue;
        status.armor = armor;
        status.armorPerStep = typedData.armorPerStep.Get(Rarity);
        status.maxArmor = typedData.maxArmor.Get(Rarity);
        status.secondsToNextStep = Mathf.Max(0f, typedData.stepInterval - timer);
    }
}
