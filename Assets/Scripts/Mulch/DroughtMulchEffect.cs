// same two phase shape as dormancy, on MulchManager.SunProductionMultiplier instead, which every
// Plant.GenerateSun call reads (sunflower, bog iris, kelp and glowshroom alike). applied keeps
// what this mulch adds to it so removing it takes back exactly that
public class DroughtMulchEffect : MulchEffect
{
    private readonly DroughtMulchData typedData;
    private readonly MulchWaveCounter waves = new MulchWaveCounter();
    private float applied;
    private bool over;

    public DroughtMulchEffect(DroughtMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        over = typedData.droughtWaves <= 0;
        SetMultiplier(over ? typedData.afterSunMultiplier : typedData.droughtSunMultiplier);
    }

    public override void Deactivate()
    {
        MulchManager.SunProductionMultiplier -= applied;
        applied = 0f;
    }

    public override void OnWaveStarted(int wave) => waves.WaveStarted(wave);

    public override void OnWaveEnded(int wave)
    {
        if (over || !waves.WaveEnded(wave)) return;
        if (waves.Completed < typedData.droughtWaves) return;
        over = true;
        SetMultiplier(typedData.afterSunMultiplier);
    }

    // the multiplier is stored as what it adds on top of 1, so it stacks additively with anything
    // else that ever touches sun production
    private void SetMultiplier(float multiplier)
    {
        float value = multiplier - 1f;
        MulchManager.SunProductionMultiplier += value - applied;
        applied = value;
    }
}
