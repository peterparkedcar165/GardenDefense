using System.Collections.Generic;
using UnityEngine;

// pays out once, at the end of its last wave: every living plant takes one free step on each path
// (Plant.GrantFreePathSteps). after that there's nothing left to undo, so it can't be sold or
// swapped anymore
public class CountdownMulchEffect : MulchEffect
{
    private readonly CountdownMulchData typedData;
    private readonly MulchWaveCounter waves = new MulchWaveCounter();
    private bool paidOut;

    public CountdownMulchEffect(CountdownMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() {}
    public override void Deactivate() {}

    public override bool CanBeRemoved => !paidOut;

    public override string GetStatus()
    {
        if (paidOut) return "Paid out";
        int left = waves.Left(typedData.wavesUntilPayout);
        return $"{left} more {(left == 1 ? "wave" : "waves")}";
    }

    public override void OnWaveStarted(int wave) => waves.WaveStarted(wave);

    public override void OnWaveEnded(int wave)
    {
        if (paidOut || !waves.WaveEnded(wave)) return;
        if (waves.Completed < typedData.wavesUntilPayout) return;
        paidOut = true;

        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive) continue;
            plant.GrantFreePathSteps();
            StatusIndicator.Spawn(plant.transform.position + new Vector3(0.4f, 0f, 0f), "+1 All Paths", new Color(0.3f, 1f, 0.2f));
        }
    }
}
