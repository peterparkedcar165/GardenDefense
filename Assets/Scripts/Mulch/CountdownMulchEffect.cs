using UnityEngine;

// counts the waves its plant survives with it equipped (a wave that ends while the plant is alive
// and has it; only waves that start after it's equipped count), then pays out once: free steps on
// every path (Plant.GrantFreePathSteps). merged to a higher rarity after paying out, the plant gets
// the extra levels the new rarity adds
public class CountdownMulchEffect : MulchEffect
{
    private readonly CountdownMulchData typedData;
    private readonly WaveCounter waves = new WaveCounter();
    private bool paidOut;
    private int levelsGiven;

    public CountdownMulchEffect(CountdownMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (paidOut) Pay();
    }

    public override void Deactivate() {}

    public override string GetStatus()
    {
        if (paidOut) return $"Paid out: +{levelsGiven} on every path";
        int left = waves.Left(typedData.wavesNeeded);
        return $"{left} more {(left == 1 ? "wave" : "waves")} to survive";
    }

    public override void OnWaveStarted(int wave) => waves.WaveStarted(wave);

    public override void OnWaveEnded(int wave)
    {
        if (paidOut || !OwnerAlive || !waves.WaveEnded(wave) || waves.Completed < typedData.wavesNeeded) return;
        paidOut = true;
        Pay();
    }

    private void Pay()
    {
        if (!OwnerAlive) return;
        int owed = VI(typedData.levels) - levelsGiven;
        if (owed <= 0) return;
        for (int i = 0; i < owed; i++) Owner.GrantFreePathSteps();
        levelsGiven += owed;
        StatusIndicator.Spawn(Owner.transform.position + new Vector3(0.4f, 0f, 0f), $"+{owed} All Paths", new Color(0.3f, 1f, 0.2f));
    }
}
