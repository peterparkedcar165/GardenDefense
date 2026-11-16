// a wave counts as clean when nothing leaked between its start and its end (the moment its
// spawning finishes). a leak at any time, rest periods included, drops the streak to zero, and
// the count starts again from the next wave
public class CleanGardenMulchEffect : MulchEffect
{
    private readonly CleanGardenMulchData typedData;
    private readonly MulchWaveCounter waves = new MulchWaveCounter();
    private int streak;
    private bool leakedThisWave;

    public CleanGardenMulchEffect(CleanGardenMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() {}
    public override void Deactivate() {}

    public override string GetStatus() => $"Clean waves in a row: {streak}/{typedData.cleanWavesNeeded}";

    public override void OnWaveStarted(int wave)
    {
        waves.WaveStarted(wave);
        leakedThisWave = false;
    }

    public override void OnInsectLeaked(Insect insect)
    {
        streak = 0;
        leakedThisWave = true;
    }

    public override void OnWaveEnded(int wave)
    {
        if (!waves.WaveEnded(wave) || leakedThisWave) return;
        streak++;
        if (streak < typedData.cleanWavesNeeded) return;
        streak = 0;
        MulchManager.instance?.GrantMulch(typedData.rewardTier);
    }
}
