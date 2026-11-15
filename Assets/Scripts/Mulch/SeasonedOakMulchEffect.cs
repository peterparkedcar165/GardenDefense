using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// each acorn knight climbs on his own clock, kept in Plant.mulchCarry so a death and revival
// carries it over (DeadPlantRecord copies it). only a living acorn's clock runs. the armor itself
// is an armorAdder delta per acorn instance, checked every frame, so a revived acorn (a brand new
// instance) picks up his saved progress on the frame after he comes back
public class SeasonedOakMulchEffect : MulchEffect
{
    private const string StepsKey = "seasonedOak.steps";
    private const string TimerKey = "seasonedOak.timer";

    private readonly SeasonedOakMulchData typedData;
    private readonly Dictionary<Plant, float> appliedArmor = new Dictionary<Plant, float>();
    private readonly List<Plant> gone = new List<Plant>();
    private Coroutine routine;

    public SeasonedOakMulchEffect(SeasonedOakMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (MulchManager.instance != null)
            routine = MulchManager.instance.StartCoroutine(Run());
    }

    public override void Deactivate()
    {
        if (routine != null && MulchManager.instance != null)
            MulchManager.instance.StopCoroutine(routine);
        routine = null;

        foreach (KeyValuePair<Plant, float> entry in appliedArmor)
            if (entry.Key != null) entry.Key.armorAdder -= entry.Value;
        appliedArmor.Clear();

        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null) continue;
            plant.mulchCarry.Remove(StepsKey);
            plant.mulchCarry.Remove(TimerKey);
            plant.RemoveEffect<SeasonedOakEffect>();
        }
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
        int maxSteps = typedData.MaxSteps;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant is not AcornKnight || plant == null || !plant.IsAlive) continue;

            float steps = plant.mulchCarry.TryGetValue(StepsKey, out float savedSteps) ? savedSteps : 0f;
            if (steps < maxSteps && typedData.stepInterval > 0f)
            {
                float timer = (plant.mulchCarry.TryGetValue(TimerKey, out float savedTimer) ? savedTimer : 0f) + deltaTime;
                while (timer >= typedData.stepInterval && steps < maxSteps)
                {
                    timer -= typedData.stepInterval;
                    steps++;
                }
                plant.mulchCarry[StepsKey] = steps;
                plant.mulchCarry[TimerKey] = steps < maxSteps ? timer : 0f;
            }

            float wanted = typedData.ArmorAtStep(Mathf.RoundToInt(steps));
            float applied = appliedArmor.TryGetValue(plant, out float current) ? current : 0f;
            if (!Mathf.Approximately(applied, wanted) || !appliedArmor.ContainsKey(plant))
            {
                plant.armorAdder += wanted - applied;
                appliedArmor[plant] = wanted;
            }

            ShowStatus(plant, wanted);
        }

        // destroyed acorns drop out of the bookkeeping; their progress already lives on in
        // their DeadPlantRecord if they died
        gone.Clear();
        foreach (Plant plant in appliedArmor.Keys)
            if (plant == null) gone.Add(plant);
        foreach (Plant plant in gone)
            appliedArmor.Remove(plant);
    }

    // the visible status on the acorn (SeasonedOakEffect), put back if anything stripped it, with
    // his current armor and the time left to his next step
    private void ShowStatus(Plant plant, float armor)
    {
        SeasonedOakEffect status = plant.GetEffect<SeasonedOakEffect>();
        if (status == null)
        {
            status = new SeasonedOakEffect(plant);
            plant.ApplyEffect(status);
        }
        float timer = plant.mulchCarry.TryGetValue(TimerKey, out float savedTimer) ? savedTimer : 0f;
        status.armor = armor;
        status.armorPerStep = typedData.armorPerStep;
        status.maxArmor = typedData.maxArmor;
        status.secondsToNextStep = Mathf.Max(0f, typedData.stepInterval - timer);
    }
}
