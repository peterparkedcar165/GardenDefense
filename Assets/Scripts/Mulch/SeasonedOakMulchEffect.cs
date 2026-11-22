using System.Collections;
using UnityEngine;

// its plant climbs on its own clock, kept in Plant.mulchCarry so a death and a revival that brings
// the mulch back carries it over (DeadPlantRecord copies it). only a living plant's clock runs. the
// armor itself is an armorAdder delta, checked every frame. the climb shows in the mulch's tooltip
// (GetStatus; Tommy, 2026-11-22: it was a status effect on the plant before)
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
        if (Owner != null) Owner.armorAdder -= appliedArmor;
        appliedArmor = 0f;
    }

    // the armor bonus now, and the next step while it's still climbing
    public override string GetStatus()
    {
        if (Owner == null) return null;
        float gained = Owner.mulchCarry.TryGetValue(GainedKey, out float savedGain) ? savedGain : 0f;
        float armor = typedData.ArmorWithGain(Rarity, gained);
        float max = typedData.maxArmor.Get(Rarity);
        string now = armor < 0f ? $"{armor:0.#}" : $"+{armor:0.#}";
        if (armor >= max) return $"Armor bonus: {now}, fully seasoned";
        float timer = Owner.mulchCarry.TryGetValue(TimerKey, out float savedTimer) ? savedTimer : 0f;
        float next = Mathf.Max(0f, typedData.stepInterval - timer);
        return $"Armor bonus: {now}\n+{typedData.armorPerStep.Get(Rarity):0.#} in {next:0}s, up to +{max:0.#}";
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
    }

}
