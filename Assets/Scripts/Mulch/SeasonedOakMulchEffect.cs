using System.Collections;
using UnityEngine;

// its plant climbs on its own clock, kept in Plant.mulchCarry so a death and a revival that brings
// the mulch back carries it over (DeadPlantRecord copies it). only a living plant's clock runs. the
// armor itself is an armorAdder delta, checked every frame; the visible status (SeasonedOakEffect) is
// display only and put back if anything strips it
public class SeasonedOakMulchEffect : MulchEffect
{
    private const string StepsKey = "seasonedOak.steps";
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

    private void Step(float deltaTime)
    {
        if (!OwnerAlive) return;
        int maxSteps = typedData.MaxSteps(Rarity);
        float steps = Owner.mulchCarry.TryGetValue(StepsKey, out float savedSteps) ? savedSteps : 0f;
        if (steps < maxSteps && typedData.stepInterval > 0f)
        {
            float timer = (Owner.mulchCarry.TryGetValue(TimerKey, out float savedTimer) ? savedTimer : 0f) + deltaTime;
            while (timer >= typedData.stepInterval && steps < maxSteps)
            {
                timer -= typedData.stepInterval;
                steps++;
            }
            Owner.mulchCarry[StepsKey] = steps;
            Owner.mulchCarry[TimerKey] = steps < maxSteps ? timer : 0f;
        }

        float wanted = typedData.ArmorAtStep(Rarity, Mathf.RoundToInt(steps));
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
